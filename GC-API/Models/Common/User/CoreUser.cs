using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Common.User
{
    public class CoreUser
    {
        public string name { get; set; }

        public string email { get; set; }

        /// <summary>
        /// If populated, the document ID for the user's full profile
        /// </summary>
        public string fullDetailsId { get; set; }
    }
}
