using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace server
{
    partial class Program
    {   static string page_Register = File.ReadAllText("../../pages/register.html");
        static string page_Main = File.ReadAllText("../../pages/main.html");
        static string page_Auth = File.ReadAllText("../../pages/authorize.html");
    }
}
