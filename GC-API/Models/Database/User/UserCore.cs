using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.Database.User
{
    /// <summary>
    /// Key information for authentication and identification
    /// </summary>
    public class UserCore
    {
        /// <summary>
        /// Full name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }

        /// <summary>
        /// Date of birth
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime dob { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        [DataType(DataType.PhoneNumber)]
        public string phone { get; set; }
    }
}
