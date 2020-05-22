using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.Common.User
{
    /// <summary>
    /// Key information for authentication and identification
    /// </summary>
    public class UserCore : IDbModel
    {
        /// <summary>
        /// Full name
        /// </summary>
        public string name { get; set; }

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
