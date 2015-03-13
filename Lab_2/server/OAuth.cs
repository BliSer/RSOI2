using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace server
{
    class OAuth<Titem, Tid, Tpwd>
    {
        IList<Titem> items;
        Func<Tid, Tpwd, Func<Titem, bool>> authorize;   // функция, которая по идентификатору и паролю возвращающает функцию,
                                                        // которая будет принимать в качестве параметра запись пользвателя
                                                        // и сравнивать её идентификатор и пароль с этими идентификатором и паролем
                                                        // (name, pwd) => (user) => user.name == name && user.password == pwd)
        Dictionary<Titem, string> authcodes = new Dictionary<Titem,string>();
        Dictionary<Titem, string> access_tokens = new Dictionary<Titem, string>();
        Dictionary<Titem, string> refresh_tokens = new Dictionary<Titem, string>();
        const int expirationTime = 5 * 60;
        Random r = new Random();
        Dictionary<Titem, Timer> timers = new Dictionary<Titem, Timer>();

        public OAuth(IList<Titem> items, Func<Tid, Tpwd, Func<Titem, bool>> authorize)
        {   this.items = items;
            this.authorize = authorize;
        }

        public string Authorize(Tid id, Tpwd pwd)
        {   Func<Titem, bool> auth = authorize(id, pwd);
            foreach(var i in items)
                if (auth(i))
                {   string code;
                    while (authcodes.Values.Contains(code = r.Next().ToString())) ;
                    authcodes[i] = code;
                    return code;
                }
            return null;
        }

        public Titem Login(string access_token)
        {   var token = access_token.Split(new char[] {' '});
            return access_tokens.GetKeyByValue(token[1]);
        }

        public string GetToken(string code)
        {   var user = authcodes.GetKeyByValue(code);
            if (user == null)
                return "{\"error\":\"code is wrong\",\"error_description\":\"error\",\"status\":\"error\"}";
            // {"access_token":"-------","token_type":"Bearer","expires_in":3600,"refresh_token":"-------",\"status\":\"success\"}
            string act, rft;
            while (access_tokens.Values.Contains(act = r.Next().ToString())) ;
            while (refresh_tokens.Values.Contains(rft = r.Next().ToString())) ;
            access_tokens[user] = act;
            refresh_tokens[user] = rft;
            Timer t = new Timer(expirationTime * 1000);
            t.Elapsed += (sender, e) =>
            {   access_tokens.Remove(user);
                refresh_tokens.Remove(user);
            };
            timers[user] = t;
            t.Start();
            string s = "{{\"access_token\":\"{0}\",\"token_type\":\"Bearer\",\"expires_in\":{1},\"refresh_token\":\"{2}\",\"status\":\"success\"}}";
            s = string.Format(s,
                act, expirationTime, rft);
            return s;
        }

        public string RefreshToken(string refreshToken)
        {   var user = refresh_tokens.GetKeyByValue(refreshToken);
            timers[user].Stop();
            Timer t = new Timer(expirationTime * 1000);
            t.Elapsed += (sender, e) =>
            {   access_tokens.Remove(user);
                refresh_tokens.Remove(user);
            };
            timers[user] = t;
            t.Start();
            return string.Format("{{\"access_token\":\"{0}\",\"token_type\":\"Bearer\",\"expires_in\":{1},\"refresh_token\":\"{2}\",\"status\":\"success\"}}",
                access_tokens[user], expirationTime, refresh_tokens[user]);
        }
    }
}
