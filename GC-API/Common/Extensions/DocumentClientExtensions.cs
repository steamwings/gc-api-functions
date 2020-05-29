using Common.Data.Interfaces;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Common.Extensions
{
    /// <summary>
    /// Extensions for <see cref="DocumentClient"/>, including things like built-in logging and exception handling.
    /// </summary>
    /// <remarks>
    /// Let's try to prevent this class becoming unmanageable. If it hinders more than it helps, then it should be removed and the functions themselves can be responsible for 
    /// logging and exception handling.
    /// 
    /// These methods are asynchronous in the spirit of <see cref="DocumentClient"/> (and because some operations may benefit from parallelism). 
    /// For synchronous cases, the syntax may be cleaner using <see cref="TaskOfIDocumentClientResultExtensions.GetWrapResult{T}(Task{IDocumentClientResult{T}}, out HttpStatusCode, out T)"/>.
    /// </remarks>
    public static class DocumentClientExtensions
    {
        /// <summary>
        /// Wrap a <see cref="DocumentClient"/> call with built-in logging and exception handling
        /// </summary>
        /// <typeparam name="T">The type of the response expected</typeparam>
        /// <param name="client"></param>
        /// <param name="log"></param>
        /// <param name="clientCall">Call to make on the <paramref name="client"/></param>
        /// <param name="callerName">Auto-populated.</param>
        /// <returns>A <see cref="IDocumentClientResult{T}"/></returns>
        public static Task<IDocumentClientResult<T>> WrapCall<T>(this DocumentClient client, ILogger log, Func<DocumentClient, Task<T>> clientCall, [CallerMemberName] string callerName = "") where T : IResourceResponseBase
        {
            return ClientDocumentOperation(log, clientCall.Invoke(client), functionName: callerName);
        }

        /// <summary>
        /// Standard wrapper for <see cref="DocumentClient"/> calls. "Translate" status codes to what the client should see.
        /// </summary>
        /// <typeparam name="T">The type of the response from the <see cref="DocumentClient"/> call</typeparam>
        /// <param name="log"></param>
        /// <returns>An <see cref="IDocumentClientResult{T}"/> </returns>
        private static async Task<IDocumentClientResult<T>> ClientDocumentOperation<T>(ILogger log, Task<T> clientTask, string functionName = "?", [CallerMemberName] string callerName = "") where T : IResourceResponseBase
        {
            T response = default;
            HttpStatusCode statusCode = HttpStatusCode.Unused; // It's reasonably unlikely to ever get this from Cosmos, right? 
            try
            {
                response = await clientTask;
                statusCode = response.StatusCode;
                if (response.IsSuccessStatusCode())
                    return new DocumentClientResult<T>(response);
            // TODO handle other exceptions like AggregateException?
            } catch(DocumentClientException dce)
            {
                log.LogError(dce, $"DocumentClientException for document operation {callerName}");
                statusCode = dce.StatusCode ?? HttpStatusCode.InternalServerError;
            } finally
            {
                if (statusCode == HttpStatusCode.Unused) 
                    log.LogError($"{functionName}: status code was NOT set in {callerName}!");
                else 
                    log.LogDebug($"{functionName}: status {(int)statusCode} in {callerName}"); // TODO Consider adding back {JsonSerializer.ToString(user)}");
            }

            switch (statusCode) // Turn most non-2XXs into 500s
            {
                case HttpStatusCode.TooManyRequests:
                    log.LogCritical("Request denied due to lack of RUs (429)!");
                    break;
                default:
                    log.LogInformation($"{callerName} call failed with {statusCode}");
                    break;
            }
            return new DocumentClientResult<T>(statusCode);
        }

        /// <summary>
        /// Encapsulate the result of a <see cref="DocumentClient"/> call; default is just an HTTP 500
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class DocumentClientResult<T> : IDocumentClientResult<T> where T : IResourceResponseBase
        {
            public DocumentClientResult() {}

            public DocumentClientResult(T response)
            {
                Response = response;
                Success = true;
                statusCode = HttpStatusCode.OK;
            }

            public DocumentClientResult(HttpStatusCode statusCode)
            {
                this.statusCode = statusCode;
            }

            private readonly HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            public bool Success { get; private set; } = false;

            public HttpStatusCode StatusCode => Response?.StatusCode ?? statusCode;

            public T Response { get; private set; }
        }
    }
}
