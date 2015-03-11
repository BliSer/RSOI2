using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;

using SimpleWebServer;

namespace server
{
    partial class Program
    {
        static string client_id = "ot1elmv9hghhz8i";
        static string client_secret = "ngt3y2kqxpc4pfm";
        static string users_db_file = "../../db_users";
        static string photos_db_file = "../../db_photos";
        static List<User> users;
        static List<Photo> photos;
        static OAuth<User, string, string> oauth;

        static Program()
        {   if (File.Exists(users_db_file))
                using (var stream = File.Open(users_db_file, FileMode.Open))
                    users = (List<User>)new XmlSerializer(typeof(List<User>)).Deserialize(stream);
            else
                users = new List<User>();

            if (File.Exists(photos_db_file))
                using (var stream = File.Open(photos_db_file, FileMode.Open))
                    photos = (List<Photo>)new XmlSerializer(typeof(List<Photo>)).Deserialize(stream);
            else
                photos = new List<Photo>();

            oauth = new OAuth<User, string, string>(users, (name, pwd) => (user) => user.name == name && user.password == pwd);
        }

        
        static void Main(string[] args)
        {   WebServer ws = new WebServer(ProcessRequest, "http://localhost/");
            ws.Run();
            Console.WriteLine("Server is running. Press a key to stop & quit.");
            Console.ReadKey();
            ws.Stop();
            
            /*
            Random r = new Random();
            Func<int> newid = ()=>
            {   int res;
                while (photos.Select(p => p.id).ToList().Contains(res = r.Next())) ;
                return res;
            };
            photos.Add(new Photo(newid(), "16.02.2015", users[0].name));
            photos.Add(new Photo(newid(), "28.02.2015", users[1].name));
            photos.Add(new Photo(newid(), "17.02.2015", users[2].name));
            photos.Add(new Photo(newid(), "01.02.2015", users[3].name));
            photos.Add(new Photo(newid(), "17.02.2015", users[2].name));
            photos.Add(new Photo(newid(), "21.02.2015", users[3].name));
            */

            using (var stream = File.Open(users_db_file, FileMode.Create))
                new XmlSerializer(typeof(List<User>)).Serialize(stream, users);
            using (var stream = File.Open(photos_db_file, FileMode.Create))
                new XmlSerializer(typeof(List<Photo>)).Serialize(stream, photos);
        }

        public static string ProcessRequest(HttpListenerRequest request, HttpListenerResponse response)
        {   switch (request.Url.AbsolutePath)
            {   case "/register":
                    return page_Register;
                case "/register-submit":
                    return request_register(request, response);

                case "/authorize":
                    if (request.QueryString["client_id"] == client_id)
                        return page_Auth
                            .Replace("REDIRECT_VALUE_TO_REPLACE", request.QueryString["redirect"])
                            .Replace("STATE_VALUE_TO_REPLACE", request.QueryString["state"]);
                    else
                        return JsonConvert.SerializeObject(new { error = "Client id unrecognized" });
                case "/authorize-submit":
                    return request_authorize(request, response);
                case "/token":
                    return request_token(request, response);

                case "/whoami":
                    return request_whoami(request, response);
                case "/users":
                    return request_users(request, response);
                case "/users/byplace":
                    return request_nearby_people(request, response);
                default:
                    {   if (request.Url.AbsolutePath.StartsWith("/photos/"))
                            return request_list_photos(request, response, request.Url.AbsolutePath.Substring("/photos/".Length));
                        return page_Main;
                    }
            }
        }

        public static string request_register(HttpListenerRequest request, HttpListenerResponse response)
        {   var pars = request.GetPOSTParams();
            if (pars["pwd"] == pars["pwd2"])
            {   users.Add(new User(
                    pars["username"],
                    pars["pwd"],
                    pars["email"],
                    pars["tel"],
                    pars["add"]));
                return JsonConvert.SerializeObject(new { status = "success" });
            }
            else
                return response.SetError(HttpStatusCode.BadRequest, "Passwords should match");
        }

        public static string request_authorize(HttpListenerRequest request, HttpListenerResponse response)
        {   //username, pwd, redirect, state
            var pars = request.GetPOSTParams();
            if (!(pars.Keys.Contains("username")
                && pars.Keys.Contains("pwd")
                && pars.Keys.Contains("redirect")
                && pars.Keys.Contains("state")))
                return response.SetError(HttpStatusCode.BadRequest, "Some of the params are missing. Expected params: POST username, pwd, redirect, state");

            var authcode = oauth.Authorize(pars["username"], pars["pwd"]);
            //"%3A%2F%2F" <=> "://");
            pars["redirect"] = pars["redirect"].Replace("%3A", ":").Replace("%2F", "/");
            if (authcode != null)
                response.Redirect(string.Format("{0}?code={1}&state={2}", pars["redirect"], authcode, pars["state"]));
            else
                response.Redirect(string.Format("{0}?error={1}&error_description={2}", pars["redirect"], "invalid_request", "credentials not recognized"));
            return "";
        }

        public static string request_token(HttpListenerRequest request, HttpListenerResponse response)
        {   //client_id, client_secret, grant_type, code|refresh_token
            var pars = request.GetPOSTParams();
            if (!(pars.Keys.Contains("client_id")
                && pars.Keys.Contains("client_secret")
                && pars.Keys.Contains("grant_type")))
                return response.SetError(HttpStatusCode.BadRequest, "Some of the params are missing. Expected params: POST client_id, client_secret, grant_type, code");

            if ((pars["client_id"] != client_id)
                || (pars["client_secret"] != client_secret))
                return response.SetError(HttpStatusCode.BadRequest, "Client credentials not recognized");
            if (pars["grant_type"] == "authorization_code")
                if (pars.Keys.Contains("code"))
                {   var s = oauth.GetToken(pars["code"]);
                    return JsonConvert.SerializeObject( s );
                }
            if (pars["grant_type"] == "refresh_token")
                if (pars.Keys.Contains("refresh_token"))
                    return oauth.RefreshToken(pars["refresh_token"]);

            return response.SetError(HttpStatusCode.BadRequest, "Wrong params");
        }

        public static User login(HttpListenerRequest request)
        {   return oauth.Login(request.Headers["access_token"]); 
        }

        public static string request_whoami(HttpListenerRequest request, HttpListenerResponse response)
        {   var u = login(request);
            if (u == null)
                return response.SetError(HttpStatusCode.Forbidden, "Wrong credentials");
            else
                return JsonConvert.SerializeObject(new
                {   name = u.name,
                    email = u.email,
                    phone = u.phone,
                    address = u.address
                });
        }

        public static string request_users(HttpListenerRequest request, HttpListenerResponse response)
        {   return JsonConvert.SerializeObject(
                        users.Select(u => u.name));
        }

        private static string request_list_photos(HttpListenerRequest request, HttpListenerResponse response, string username)
        {   var user = login(request);
            if (user == null)
                return response.SetError(HttpStatusCode.Forbidden, "Wrong credentials");
            else
            {   var other = users.First(u => u.name == username);
                if (other == null)
                    return response.SetError(HttpStatusCode.NotFound, "requested user not found");
                else
                    return JsonConvert.SerializeObject(new
                    {   user = other.name,
                        photos = photos.Where(p => p.owner == other.name)
                    });
            }
        }

        private static string request_nearby_people(HttpListenerRequest request, HttpListenerResponse response)
        {   var user = login(request);
            if (user == null)
                return response.SetError(HttpStatusCode.Forbidden, "Wrong credentials");
            else
                if (!request.QueryString.AllKeys.Contains("place"))
                    return response.SetError(HttpStatusCode.BadRequest, "Bad argument for request");
                else
                    return JsonConvert.SerializeObject(
                        users
                          .Where(u => u.address.Contains(request.QueryString["place"]))
                          .Select(u => u.name));
        }
    }
}
