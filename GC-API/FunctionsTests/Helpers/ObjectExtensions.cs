using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsTests.Helpers
{
    public static class ObjectExtensions
    {
        public static T GetPropertyValue<T>(this object obj, string property)
        {
            return (T) obj.GetType().GetProperty(property).GetValue(obj, null);
        }
    }
}
