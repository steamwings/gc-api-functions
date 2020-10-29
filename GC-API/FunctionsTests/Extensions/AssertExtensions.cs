using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace FunctionsTests.Extensions
{
    public static class AssertExtensions
    {
        /// <summary>
        /// Assert an object has type <typeparamref name="T"/> and cast it.
        /// </summary>
        /// <typeparam name="T">The expected type of <paramref name="testObj"/></typeparam>
        /// <param name="assert">Assert.That</param>
        /// <param name="testObj">object to type check</param>
        /// <param name="result"><paramref name="testObj"/> cast to <typeparamref name="T"/></param>
        /// <returns>The <see cref="Assert"/> object</returns>
        public static Assert IsOfType<T>(this Assert assert, object testObj, out T result)
        {
            if (testObj is T t)
            {
                result = t;
                return assert;
            }
            throw new AssertFailedException($"Type did not match type {typeof(T).Name}");
        }

        /// <summary>
        /// Test that an exception is thrown only when a condition is true.
        /// </summary>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="assert">Assert.That</param>
        /// <param name="condition">When true, <see cref="Assert.ThrowsException{T}(Action)"/> will be called; when false, <paramref name="action"/> will be invoked without an assertion.</param>
        /// <param name="action"><see cref="Action"/> to run which may be checked for a thrown exception</param>
        /// <returns>The <see cref="Assert"/> object</returns>
        public static Assert ThrowsExceptionIf<T>(this Assert assert, bool condition, Action action) where T : Exception
        {
            if (condition)
                Assert.ThrowsException<T>(action);
            else action.Invoke();

            return assert;
        }

        /// <summary>
        /// Verify that one or more streams were not written to
        /// </summary>
        public static Assert StreamNotWritten(this Assert assert, params Stream[] streams)
        {
            foreach (var stream in streams)
            {
                if (stream.Position != 0 || stream.Length > 0)
                    throw new AssertFailedException($"Expected unwritten stream but got position,length=({stream.Position},{stream.Length})");
            }
            return assert;
        }
    }
}
