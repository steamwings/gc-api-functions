using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Common.User
{
    /// <summary>
    /// Data for public profile
    /// </summary>
    public class UserProfile // TODO Add IProfile interface
    {
        public string bio { get; set; }

        // TODO This should be defined from limited options
        public string domains { get; set; }

        // TODO
        // public IEnumerable<IPortfolio> portfolios { get; set; }
    }
}
