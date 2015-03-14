using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace server
{
    static class Extensions
    {
        public static Dictionary<string, string> GetPOSTParams(this HttpListenerRequest request)
        {   var body = new System.IO.StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();
            return body.Split('&').Select(kvp =>
            {   var _t = kvp.Split('=');
                return new KeyValuePair<string, string>(_t[0], _t[1]);
            }).ToDictionary(x => x.Key, x => x.Value);
        }

        public static T1 GetKeyByValue<T1>(this Dictionary<T1, string> dict, string value)
        {   foreach (var key in dict.Keys)
                if (dict[key] == value)
                    return key;
            return default(T1);
        }

        public static string SetError(this HttpListenerResponse response, HttpStatusCode code, string message)
        {   response.ContentType = "text/plain";
            byte[] buf = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buf.Length;
            response.OutputStream.Write(buf, 0, buf.Length);
            response.StatusCode = Convert.ToInt32(code);
            return "";
        }
    }


}
