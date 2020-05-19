using System.Linq;
using System.Runtime.CompilerServices;

namespace FunctionsTests.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Shortcut method which encapsulates using reflection to access a property value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static T GetPropertyValue<T>(this object obj, string property)
        {
            return (T) obj.GetType().GetProperty(property).GetValue(obj, null);
        }

        /// <summary>
        /// Determine if an <see cref="object"/> has an anonymous type.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to test.</param>
        /// <returns><c>True</c> if the object is an anonymous object.</returns>
        /// <remarks>False positives are not impossible, but should only happen if someone intentionally modifies IL.</remarks>
        public static bool IsAnonymous(this object obj)
        {
            var type = obj.GetType();
            return type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Count() > 0 
                && type.FullName.Contains("AnonymousType");
        }
    }
}
