using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsTests.Extensions
{
    public static class AssertExtensions
    {
        /// <summary>
        /// Assert an object has type <typeparamref name="T"/> and cast it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assert">Assert.That</param>
        /// <param name="toTest"></param>
        /// <param name="result"><paramref name="toTest"/> cast to <typeparamref name="T"/></param>
        /// <returns></returns>
        public static Assert IsOfType<T>(this Assert assert, object toTest, out T result)
        {
            if (toTest is T t)
            {
                result = t;
                return assert;
            }
            throw new AssertFailedException($"Type did not match type {typeof(T).Name}");
        }

        public static Assert ThrowsExceptionIf<T>(this Assert assert, bool condition, Action action) where T : Exception
        {
            if (condition)
                Assert.ThrowsException<T>(action);
            else action.Invoke();

            return assert;
        }

        public static Assert And(this Assert assert) => assert;
    }
}
