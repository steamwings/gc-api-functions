using System;
using System.Collections.Generic;
using System.Text;

namespace Models.User
{
    public class GcUser
    { 
        public string hash { get; set; }
        public string salt { get; set; }
        public CoreUser coreUser { get; set; }
    }
}
