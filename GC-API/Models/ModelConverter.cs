using Models.Common;
using Models.Database.User;
using Models.UI.User;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Common.Extensions;

namespace Models
{
    public class ModelConverter
    {
        // TODO Highly flexible convert method
        //public static T Convert<T>(IDbModel dbModel) where T : IModel, class, new()
        //{
        //    // Basic idea:
        // IDbModel offers Properties list (generate via reflection and flattening)
        // Via reflection, search for T's properties and set them to the values with matching names in dbModel's Properties
        //}

        /// <summary>
        /// Convert a <see cref="GcUser"/> to a <see cref="UserCoreUI"/>
        /// </summary>
        /// <typeparam name="T">Type is used to pick the appropriate overload</typeparam>
        /// <param name="user"></param>
        /// <returns>A new <see cref="UserCoreUI"/></returns>
        public static UserCoreUI Convert<T>(GcUser user) where T : UserCoreUI
        {
            return new UserCoreUI
            {
                name = user.userCore.name,
                email = user.id.FromBase64(),
                dob = user.userCore.dob,
                phone = user.userCore.phone,
            };
        }
    }
}
