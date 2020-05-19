using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FunctionsTests.Extensions
{
    /// <summary>
    /// Pseudo-extension methods for anonymous objects.
    /// </summary>
    /// <remarks>
    /// These are not *true* extension methods; they will be available on non-anonymous objects.
    /// </remarks>
    public static class AnonymousObjectExtensions
    {
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly string[] BackingFieldFormats = { "<{0}>i__Field", "<{0}>" }; // This is dangerous

        /// <summary>
        /// Set a property of anonymous object to anything, including null. <paramref name="newValue"/> is NOT type-checked.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The anonymous object.</param>
        /// <param name="propName">Name of the property to set.</param>
        /// <param name="newValue">New property value.</param>
        /// <returns>The mutated anonymous object. The operation is also in-place.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="instance"/> is not an anonymous object.</exception>
        /// <exception cref="NotSupportedException">Thrown if the backing fields for the properties are not formatted as expected. 
        /// This may occur if <paramref name="propName"/> is mispelled.</exception>
        /// <remarks> This is modified from: https://stackoverflow.com/a/30242237/4163002 </remarks>
        public static T SetAnonymousObjectProperty<T>(
            this T instance,
            string propName,
            object newValue) where T : class
        {
            if (!instance.IsAnonymous()) throw new ArgumentException($"{nameof(instance)} is not an anonymous object. Use PropertyInfo.SetValue instead.");
            var backingFieldNames = BackingFieldFormats.Select(x => string.Format(x, propName)).ToList();
            var fi = typeof(T)
                .GetFields(FieldFlags)
                .FirstOrDefault(f => backingFieldNames.Contains(f.Name));
            if (fi == null)
                throw new NotSupportedException($"Cannot find backing field for {propName}");
            fi.SetValue(instance, newValue);
            return instance;
        }
    }
}
