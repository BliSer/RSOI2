using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class User
    {
        public string name;
        public string password;
        public string email;
        public string phone;
        public string address;

        public User()
        {
        }

        public User(string name, string password, string email, string phone, string address)
        {   this.name = name;
            this.password = password;
            this.email = email;
            this.phone = phone;
            this.address = address;
        }
    }
}
