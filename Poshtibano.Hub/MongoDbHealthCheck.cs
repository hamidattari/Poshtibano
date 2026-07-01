using MongoDB.Driver;

namespace Poshtibano.Hub
{
    public class MongoDbHealthCheck
    {
        private readonly IMongoClient _mongoClient;
        private readonly ILogger<MongoDbHealthCheck> _logger;
        private bool _isConnected;

        public MongoDbHealthCheck(IMongoClient mongoClient, ILogger<MongoDbHealthCheck> logger)
        {
            _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isConnected = false;
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                await _mongoClient.GetDatabase("admin").RunCommandAsync<MongoDB.Bson.BsonDocument>(
                    MongoDB.Bson.BsonDocument.Parse("{ ping: 1 }"));

                _isConnected = true;
                _logger.LogInformation("✅ MongoDB is connected");
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger.LogWarning($"⚠️ MongoDB is not connected: {ex.Message}");
                return false;
            }
        }

        public bool IsConnected => _isConnected;
    }
}
