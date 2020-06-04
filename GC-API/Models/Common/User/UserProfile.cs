using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Common.User
{
    /// <summary>
    /// Data for public profile
    /// </summary>
    public class UserProfile
    {
        public string bio { get; set; }

        // TODO This should defined from limited options
        public string artisticDomains { get; set; }

        // TODO
        // public IEnumerable<IPortfolio> portfolios { get; set; }
    }
}
