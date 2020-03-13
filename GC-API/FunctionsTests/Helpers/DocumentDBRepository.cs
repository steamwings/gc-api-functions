using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FunctionsTests.Helpers
{
    public static class DocumentDBRepository<T> where T : class
    {
        private const string DEFAULT_DB = "userdb";
        private const string DEFAULT_COL = "usercoll";
        private static string DatabaseId = DEFAULT_DB;
        private static string CollectionId = DEFAULT_COL;

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

        public static void Initialize(string endpoint, string authKey, string partitionKey = null, string uniqueKey = null, string databaseId = DEFAULT_DB, string collectionId = DEFAULT_COL)
        {
            DatabaseId = databaseId;
            CollectionId = collectionId;
            Client = new DocumentClient(new Uri(endpoint), authKey);
            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync(partitionKey, uniqueKey).Wait();
        }

        public static void Teardown()
        {
            DeleteDatabaseAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await Client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await Client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync(string partitionkey = null, string uniqueKey = null, int throughputRus = 400)
        {
            try
            {
                if (partitionkey is null)
                {
                    await Client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId));
                }
                else
                {
                    await Client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId), new RequestOptions { PartitionKey = new PartitionKey(partitionkey) });
                }
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var docColl = new DocumentCollection
                    {
                        Id = CollectionId,
                    };
                    if (partitionkey != null)
                    {
                        docColl.PartitionKey = new PartitionKeyDefinition
                        {
                            Paths = new System.Collections.ObjectModel.Collection<string>(new List<string>() { partitionkey })
                        };
                    }
                    if (uniqueKey != null)
                    {
                        docColl.UniqueKeyPolicy = new UniqueKeyPolicy
                        {
                            UniqueKeys = new System.Collections.ObjectModel.Collection<UniqueKey>(new List<UniqueKey>
                            {
                                new UniqueKey { Paths = new System.Collections.ObjectModel.Collection<string>(new List<string> { 
                                    uniqueKey, 
                                }) },
                            })
                        };
                    }
                    await Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId), docColl,
                        new RequestOptions { OfferThroughput = throughputRus });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task DeleteDatabaseAsync()
        {
            await Client.DeleteDatabaseAsync((UriFactory.CreateDatabaseUri(DatabaseId)));
        }
    }
}
