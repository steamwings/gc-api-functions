using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common.Data.Interfaces
{
    /// <summary>
    /// Interface to abstract results of <see cref="DocumentClient"/> calls
    /// </summary>
    /// <typeparam name="T">The type of response from the <see cref="DocumentClient"/> call</typeparam>
    public interface IDocumentClientResult<T> where T : IResourceResponseBase
    {
        /// <summary>
        /// <c>True</c> indicates <see cref="Response"/> is valid (non-null)
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Cannot be null, so it can always be used.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// When valid (indicated by <see cref="Success"/>), the <see cref="IResourceResponseBase.StatusCode"/> property should match <see cref="StatusCode"/>.
        /// </summary>
        public T Response { get; }

    }
}
