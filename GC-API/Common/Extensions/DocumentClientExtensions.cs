using Common.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
        /// <param name="callerName">Auto-populated. Will generally be "Run" for Azure functions.</param>
        /// <returns>A <see cref="IDocumentClientResult{T}"/></returns>
        public static Task<IDocumentClientResult<T>> WrapCall<T>(this DocumentClient client, ILogger log, Func<DocumentClient, Task<T>> clientCall, [CallerMemberName] string callerName = "", [CallerLineNumber] int callerLine = 0) where T : IResourceResponseBase
        {
            return ClientDocumentOperation(log, clientCall.Invoke(client), callerName: callerName, callerLine: callerLine);
        }

        /// <summary>
        /// Method to find a unique item in the database.
        /// </summary>
        /// <typeparam name="T">Type of the desired unique item</typeparam>
        /// <param name="client"></param>
        /// <param name="log"></param>
        /// <param name="func">A function using a <see cref="DocumentClient"/> and returning an <see cref="IQueryable{T}"/></param>
        /// <param name="item">Should only be used when <c>True</c> is returned.</param>
        /// <param name="errorResponse">Should only be used when <c>False</c> is returned.</param>
        /// <param name="callerName">Auto-populated. Will generally be "Run" for Azure functions.</param>
        /// <returns><c>True</c> when <paramref name="item"/> was found.</returns>
        /// <remarks> If this method does not prove reusable, then remove it and just paste this code back. 
        /// That at least provides the advantage of less generic log messages.</remarks>
        public static bool FindUniqueItem<T>(this DocumentClient client, ILogger log, Func<DocumentClient, IQueryable<T>> func,  out T item, out IStatusCodeActionResult errorResponse, [CallerMemberName] string callerName = "")
        {
            var items = func.Invoke(client);
            item = default;
            errorResponse = new StatusCodeResult(500);
            switch (items.Count())
            {
                case 1: break; // Found!
                case 0:
                    log.LogTrace($"No {typeof(T).GetType().Name} found for {callerName}");
                    errorResponse = new NotFoundResult();
                    return false;
                default:
                    log.LogCritical($"More than one {typeof(T).GetType().Name} for {callerName}");
                    // Writing code to recover from this (e.g. change one user's id) should not be necessary
                    // but might end up being useful...
                    return false;
            }
            item = items.AsEnumerable().Single();
            return true;
        }

        /// <summary>
        /// Standard wrapper for <see cref="DocumentClient"/> calls. "Translate" status codes to what the client should see.
        /// </summary>
        /// <typeparam name="T">The type of the response from the <see cref="DocumentClient"/> call</typeparam>
        /// <param name="log"></param>
        /// <returns>An <see cref="IDocumentClientResult{T}"/> </returns>
        private static async Task<IDocumentClientResult<T>> ClientDocumentOperation<T>(ILogger log, Task<T> clientTask, [CallerMemberName] string callerName = "", [CallerLineNumber] int callerLine = 0) where T : IResourceResponseBase
        {
            T response = default;
            HttpStatusCode statusCode = HttpStatusCode.Unused; // It's reasonably unlikely to ever get this from Cosmos, right? 
            try
            {
                response = await clientTask;
                statusCode = response.StatusCode;
                log.LogTrace("Response from DocumentClient task has status {0}, activityId {1}", response.StatusCode, response.ActivityId);
                if (response.IsSuccessStatusCode())
                    return new DocumentClientResult<T>(response);
            // TODO handle other exceptions like AggregateException?
            } catch(DocumentClientException dce)
            {
                log.LogError(dce, $"{callerName}({callerLine}): DocumentClientException");
                statusCode = dce.StatusCode ?? HttpStatusCode.InternalServerError;
            } finally
            {
                if (statusCode == HttpStatusCode.Unused) 
                    log.LogError($"{callerName}({callerLine}): status code was NOT set for document operation!");
                else 
                    log.LogDebug($"{callerName}({callerLine}): status {(int)statusCode} for document operation"); // TODO Consider adding back {JsonSerializer.ToString(user)}");
            }

            switch (statusCode) // Turn most non-2XXs into 500s
            {
                case HttpStatusCode.TooManyRequests:
                    log.LogCritical($"{callerName}({callerLine}): Request denied due to lack of RUs (429)!");
                    break;
                default:
                    log.LogInformation($"{callerName}({callerLine}): document operation failed with {statusCode}");
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
