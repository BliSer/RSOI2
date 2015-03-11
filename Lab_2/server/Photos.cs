using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class Photo
    {
        public int id;
        public string uploaded;
        public string owner;

        public Photo()
        {
        }

        public Photo(int id, string uploaded, string owner)
        {   this.id = id;
            this.uploaded = uploaded;
            this.owner = owner;
        }
    }
}
