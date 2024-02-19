using LabraryApi.Classes;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LabraryApi.DataSchemas {
    public class MongoDbContext {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings) {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<UserDTO> UserDTO => _database.GetCollection<UserDTO>("UserDTO");
        public IMongoCollection<BookDTO> BookDTO => _database.GetCollection<BookDTO>("BookDTO");
    }
}
