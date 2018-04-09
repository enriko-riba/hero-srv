namespace ws_hero.DAL
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Newtonsoft.Json;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public class CosmosRepo
    {
        private const string endpointUri = "https://eri-test.documents.azure.com:443/";
        private const string primaryKey = "EBRpYeDhByEU9kvfQdkgqkdE68yFcBxStLb0O3jrQB2jABsCKZCLjvk2ltvjZh7cpEk0TlgxIzNf6a28v89Syw==";

        private const string DB_ID = "heroDB";
        private const string USERS_ID = "Users";

        private DocumentClient client = new DocumentClient(new Uri(endpointUri), primaryKey);


        public async Task Init()
        {
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = DB_ID });
            await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DB_ID), new DocumentCollection { Id = USERS_ID });
        }
        
        public async Task<User> CreateUserIfNotExists(User user)
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

    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Email { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DisplayName { get; set; }
        public string PictureURL { get; set; }

        public PlayerData PlayerData { get; set; }

        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty(PropertyName = "_ts")]
        public DateTime LastLogin { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class PlayerData
    {
    }
}
