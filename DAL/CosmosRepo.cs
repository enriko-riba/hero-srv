﻿namespace ws_hero.DAL
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    public class CosmosRepo
    {
        private const string endpointUri = "https://eri-test.documents.azure.com:443/";
        private const string primaryKey = "EBRpYeDhByEU9kvfQdkgqkdE68yFcBxStLb0O3jrQB2jABsCKZCLjvk2ltvjZh7cpEk0TlgxIzNf6a28v89Syw==";

        private const string DB_ID = "heroDB";
        private const string USERS_ID = "Users";

        private DocumentClient client = new DocumentClient(new Uri(endpointUri), primaryKey);

        private Uri userCollectionLink = UriFactory.CreateDocumentCollectionUri(DB_ID, USERS_ID);

        public async Task InitAsync()
        {
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = DB_ID });
            await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DB_ID), new DocumentCollection { Id = USERS_ID });
        }
        
        public IEnumerable<User> GetActiveUsers()
        {
            var query = client.CreateDocumentQuery<User>(userCollectionLink)
                        .Where(so => so.IsActive == true)
                        .ToArray();
            return query;                        
        }

        public async Task<User> GetUserAsync(string Id)
        {
            try
            {
                var result = await this.client.ReadDocumentAsync<User>(UriFactory.CreateDocumentUri(DB_ID, USERS_ID, Id));
                return result.Document;
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode != HttpStatusCode.NotFound) throw;
            }
            return null;
        }

        public async Task<User> SaveUserAsync(User user)
        {
            try
            {
                var response = await this.client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DB_ID, USERS_ID), user);
                user = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(response.Resource.ToString());
                return user;
            }
            catch (Exception ex)
            {
                throw;
            }
            return null;
        }
        public async Task<User> CreateUserIfNotExistsAsync(User user)
        {
            try
            {
                var result = await this.client.ReadDocumentAsync<User>(UriFactory.CreateDocumentUri(DB_ID, USERS_ID, user.Email));
                return result;
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DB_ID, USERS_ID), user);
                    return user;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
