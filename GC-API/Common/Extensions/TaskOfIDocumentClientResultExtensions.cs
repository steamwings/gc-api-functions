using Common.Data.Interfaces;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
    /// <summary>
    /// Extension method class for <see cref="Task{IDocumentClientResult{T}}"/> (<see cref="Task"/> of <see cref="IDocumentClientResult{T}"/> where T implements <see cref="IResourceResponseBase"/>)
    /// </summary>
    public static class TaskOfIDocumentClientResultExtensions
    {
        /// <summary>
        /// Synchronously convert an <see cref="IDocumentClientResult{T}"/> to a returned <see cref="bool"/> and out parameters.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="DocumentClient"/> response</typeparam>
        /// <param name="task">The task which performs a managed <see cref="DocumentClient"/> call</param>
        /// <param name="statusCode">The status code for the client when <c>False</c> is returned</param>
        /// <param name="response">The <typeparamref name="T"/> response, valid when <c>True</c> is returned</param>
        /// <returns><c>True</c> when <see cref="IDocumentClientResult{T}.Success"/> is <c>True</c> </returns>
        /// <remarks>Use in conjunction with <see cref="DocumentClientExtensions.WrapCall{TResponse}(DocumentClient, Func{DocumentClient, Task{TResponse}}, Microsoft.Extensions.Logging.ILogger, string)"/></remarks>
        public static bool GetWrapResult<T>(this Task<IDocumentClientResult<T>> task, out HttpStatusCode statusCode, out T response) where T : IResourceResponseBase
        {
            var result = task.GetAwaiter().GetResult();
            statusCode = result.StatusCode;
            response = result.Response;
            return result.Success;
        }
    }
}
