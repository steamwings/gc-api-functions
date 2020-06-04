using Models.Common.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.UI.User
{
    /// <summary>
    /// User core data model for UI
    /// </summary>
    public class UiUser
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Full name
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Date of birth
        /// </summary>
        [DataType(DataType.Date)] // TODO add a JsonConverter attribute
        public DateTime dob { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        [DataType(DataType.PhoneNumber)] // TODO add a JsonConverter attribute?
        public string phone { get; set; }

        /// <summary>
        /// Base64-encoded email used as a unique id
        /// </summary>
        public string email { get; set; }

        /// <summary>
        /// JWT authorization
        /// </summary>
        public string token { get; set; }

        public UserProfile profile { get; set; }
    }
}
