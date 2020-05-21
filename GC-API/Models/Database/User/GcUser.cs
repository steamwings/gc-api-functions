using Models.Common;
using Models.Common.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Database.User
{
    public class GcUser : IDbModel
    { 
        /// <summary>
        /// Document id used by Cosmos
        /// </summary>
        public string id { get; set; }
        public string hash { get; set; }
        public string salt { get; set; }
        public UserCore userCore { get; set; }
        public UserProfile profile { get; set; }
    }
}
