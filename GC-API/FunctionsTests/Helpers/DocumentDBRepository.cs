using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FunctionsTests.Helpers
{
    /// <summary>
    /// Convenient wrapper for <see cref="DocumentClient"/> with basic configuration methods for a single type, single collection, single database repository
    /// </summary>
    /// <typeparam name="T">The type stored in this repository</typeparam>
    /// <remarks>
    /// Since it is static, only one instance can exist per type at a time. (This format is convenient for unit testing.)
    /// </remarks>
    public static class DocumentDBRepository<T> where T : class
    {
        private const string DEFAULT_DB = "userdb";
        private const string DEFAULT_COL = "usercoll";
        private static string DatabaseId = DEFAULT_DB;
        private static string CollectionId = DEFAULT_COL;

        private static Database database;

        public static DocumentClient Client { get; private set; }

        public static async Task<T> GetItemAsync(string id, string category = null)
        {
            try
            {
               Document document = category is null ?
                    await Client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id)) :
                    await Client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), new RequestOptions { PartitionKey = new PartitionKey(category) });
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = Client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task<Document> CreateItemAsync(T item)
        {
            return await Client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), item);
        }

        public static async Task<Document> UpdateItemAsync(string id, T item)
        {
            return await Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), item);
        }

        public static async Task DeleteItemAsync(string id, string category = null)
        {
            await Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id), 
                category is null ? null : new RequestOptions { PartitionKey = new PartitionKey(category) });
        }

        /// <summary>
        /// Initialize the DocumentDB repo with one DB and one collection
        /// </summary>
        /// <param name="endpoint">Where is the Cosmos emulator</param>
        /// <param name="authKey">Cosmos emulator key</param>
        /// <param name="partitionKey">Required now apparently... The default of /profile/domains is not a good one and should be updated when the real thing is</param>
        /// <param name="uniqueKey">Add to prevent duplicates</param>
        /// <param name="databaseId">DB name</param>
        /// <param name="collectionId">Collection name</param>
        public static void Initialize(string endpoint, string authKey, string partitionKey = "/profile/domains", string uniqueKey = "/userCore/email", string databaseId = DEFAULT_DB, string collectionId = DEFAULT_COL)
        {
            DatabaseId = databaseId;
            CollectionId = collectionId;
            Client = new DocumentClient(new Uri(endpoint), authKey);
            database = Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId }).GetAwaiter().GetResult();
            CreateCollectionIfNotExistsAsync(partitionKey, uniqueKey).Wait();
        }

        public static void Teardown()
        {
            DeleteDatabaseAsync().Wait();
        }

        private static async Task CreateCollectionIfNotExistsAsync(string partitionkey, string uniqueKey = null, int throughputRus = 400)
        {
            try
            {
                var collection = new DocumentCollection
                {
                    Id = CollectionId,
                    PartitionKey = new PartitionKeyDefinition { Paths = new Collection<string> { partitionkey } }
                };
                if (uniqueKey != null)
                {
                    collection.UniqueKeyPolicy = new UniqueKeyPolicy
                    {
                        UniqueKeys = new Collection<UniqueKey>(new List<UniqueKey>
                            {
                                new UniqueKey { Paths = new Collection<string>(new List<string> {
                                    uniqueKey,
                                }) },
                            })
                    };
                }


                await Client.CreateDocumentCollectionIfNotExistsAsync(database?.SelfLink, collection);
            }
            catch (DocumentClientException e)
            {
                string a = e.Message;
                throw;
            }
            
        }

        private static async Task DeleteDatabaseAsync()
        {
            await Client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
        }
    }
}
