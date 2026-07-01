using MongoDB.Driver;
using Poshtibano.Hub;


public class MongoDbService
{
    private readonly IMongoCollection<SessionDocument> _sessionsCollection;
    private readonly IMongoCollection<ConnectionLogDocument> _connectionLogsCollection;
    private readonly ILogger<MongoDbService> _logger;
    private readonly MongoDbHealthCheck _healthCheck;

    public MongoDbService(
        IMongoDatabase database,
        ILogger<MongoDbService> logger,
        MongoDbHealthCheck healthCheck)
    {
        _sessionsCollection = database.GetCollection<SessionDocument>("sessions");
        _connectionLogsCollection = database.GetCollection<ConnectionLogDocument>("connection_logs");
        _logger = logger;
        _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
    }

    public async Task InitializeIndexesAsync()
    {
        if (!await _healthCheck.IsConnectedAsync())
        {
            _logger.LogWarning("⚠️ MongoDB is not conected");
            return;
        }

        try
        {
            var sessionIndexKeys = Builders<SessionDocument>.IndexKeys
                .Ascending(s => s.SessionId);
            await _sessionsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<SessionDocument>(sessionIndexKeys,
                    new CreateIndexOptions { Unique = true }));

            var connectionLogIndexKeys = Builders<ConnectionLogDocument>.IndexKeys
                .Ascending(c => c.SessionId)
                .Descending(c => c.ConnectedAt);
            await _connectionLogsCollection.Indexes.CreateOneAsync(
                new CreateIndexModel<ConnectionLogDocument>(connectionLogIndexKeys));

            _logger.LogInformation("✅ index has been created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ error in MongoDB");
        }
    }

    public async Task<ConnectionLogDocument> RecordConnectionAsync(
        string sessionId,
        string agentConnectionId,
        string controllerConnectionId,
        string agentIp,
        string controllerIp)
    {
        if (!_healthCheck.IsConnected)
        {
            _logger.LogWarning($"⚠️ MongoDB is not connected. session {sessionId} log has not been recorded into database");
            return null;
        }

        try
        {
            var now = DateTime.UtcNow;

            var connectionLog = new ConnectionLogDocument
            {
                SessionId = sessionId,
                AgentConnectionId = agentConnectionId,
                ControllerConnectionId = controllerConnectionId,
                AgentIp = agentIp,
                ControllerIp = controllerIp,
                ConnectedAt = now,
                CreatedAt = now
            };

            await _connectionLogsCollection.InsertOneAsync(connectionLog);

            var filter = Builders<SessionDocument>.Filter.Eq(s => s.SessionId, sessionId);
            var update = Builders<SessionDocument>.Update
                .SetOnInsert(s => s.SessionId, sessionId)
                .SetOnInsert(s => s.FirstConnectedAt, now)
                .SetOnInsert(s => s.CreatedAt, now)
                .Set(s => s.AgentIp, agentIp)
                .Set(s => s.ControllerIp, controllerIp)
                .Set(s => s.LastConnectedAt, now)
                .Set(s => s.Status, "active")
                .Set(s => s.UpdatedAt, now)
                .Inc(s => s.TotalConnections, 1);

            await _sessionsCollection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true });

            _logger.LogInformation($"✅ Session log has been for session: {sessionId}");
            return connectionLog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error in record log for session: {sessionId}");
            return null;
        }
    }

    public async Task RecordDisconnectionAsync(
        string sessionId,
        string connectionLogId,
        string disconnectReason = null)
    {
        if (!_healthCheck.IsConnected)
        {
            _logger.LogWarning($"⚠️ MongoDB is not coonected. {sessionId} is not logged");
            return;
        }

        try
        {
            var now = DateTime.UtcNow;

            var logFilter = Builders<ConnectionLogDocument>.Filter.Eq(c => c.Id, connectionLogId);
            var connectionLog = await _connectionLogsCollection.Find(logFilter).FirstOrDefaultAsync();

            if (connectionLog != null)
            {
                var duration = (int)(now - connectionLog.ConnectedAt).TotalSeconds;

                var logUpdate = Builders<ConnectionLogDocument>.Update
                    .Set(c => c.DisconnectedAt, now)
                    .Set(c => c.Duration, duration)
                    .Set(c => c.DisconnectReason, disconnectReason);

                await _connectionLogsCollection.UpdateOneAsync(logFilter, logUpdate);

                var sessionFilter = Builders<SessionDocument>.Filter.Eq(s => s.SessionId, sessionId);
                var sessionUpdate = Builders<SessionDocument>.Update
                    .Set(s => s.LastDisconnectedAt, now)
                    .Set(s => s.Status, "disconnected")
                    .Set(s => s.UpdatedAt, now)
                    .Inc(s => s.TotalDuration, duration);

                await _sessionsCollection.UpdateOneAsync(sessionFilter, sessionUpdate);

                _logger.LogInformation($"✅ Session {sessionId} log has been recorded (duration: {duration}s)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error in record log for session: {sessionId}");
        }
    }

    public async Task<SessionDocument> GetSessionAsync(string sessionId)
    {
        if (!_healthCheck.IsConnected)
        {
            _logger.LogWarning($"⚠️ MongoDB is not connected");
            return null;
        }

        return await _sessionsCollection
            .Find(s => s.SessionId == sessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ConnectionLogDocument>> GetConnectionLogsAsync(string sessionId)
    {
        if (!_healthCheck.IsConnected)
        {
            _logger.LogWarning($"⚠️ MongoDB us not connected");
            return new List<ConnectionLogDocument>();
        }

        return await _connectionLogsCollection
            .Find(c => c.SessionId == sessionId)
            .SortByDescending(c => c.ConnectedAt)
            .ToListAsync();
    }
}