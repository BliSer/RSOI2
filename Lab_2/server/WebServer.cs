using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;

namespace SimpleWebServer
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, HttpListenerResponse, string> _responder_method;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, HttpListenerResponse, string> method)
        {   if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes list is empty");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("no responder method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);
            _responder_method = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, HttpListenerResponse, string> method, params string[] prefixes)
            : this(prefixes, method)
        {
        }

        public void Run()
        {   ThreadPool.QueueUserWorkItem((o) =>
            {   Console.WriteLine("Webserver running...");
                try
                {   while (_listener.IsListening)
                    {   ThreadPool.QueueUserWorkItem((c) =>
                        {   var ctx = c as HttpListenerContext;
                            try
                            {   ctx.Response.ContentType = "text/html";
                                string rstr = _responder_method(ctx.Request, ctx.Response);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch (Exception e) { } // suppress any exceptions
                            finally
                            {   // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {   _listener.Stop();
            _listener.Close();
        }
    }
}