using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Poshtibano.Common;

/// <summary>
/// MongoDB Document for Sessions
/// </summary>
public class SessionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("sessionId")]
    public string SessionId { get; set; }

    [BsonElement("agentIp")]
    public string AgentIp { get; set; }

    [BsonElement("controllerIp")]
    public string ControllerIp { get; set; }

    [BsonElement("firstConnectedAt")]
    public DateTime FirstConnectedAt { get; set; }

    [BsonElement("lastConnectedAt")]
    public DateTime LastConnectedAt { get; set; }

    [BsonElement("lastDisconnectedAt")]
    public DateTime? LastDisconnectedAt { get; set; }

    [BsonElement("totalConnections")]
    public int TotalConnections { get; set; }

    [BsonElement("totalDuration")]
    public int TotalDuration { get; set; } // in seconds

    [BsonElement("status")]
    public string Status { get; set; } // "active" or "disconnected"

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// MongoDB Document for Connection Logs
/// </summary>
public class ConnectionLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("sessionId")]
    public string SessionId { get; set; }

    [BsonElement("agentConnectionId")]
    public string AgentConnectionId { get; set; }

    [BsonElement("controllerConnectionId")]
    public string ControllerConnectionId { get; set; }

    [BsonElement("agentIp")]
    public string AgentIp { get; set; }

    [BsonElement("controllerIp")]
    public string ControllerIp { get; set; }

    [BsonElement("connectedAt")]
    public DateTime ConnectedAt { get; set; }

    [BsonElement("disconnectedAt")]
    public DateTime? DisconnectedAt { get; set; }

    [BsonElement("duration")]
    public int Duration { get; set; } // in seconds

    [BsonElement("disconnectReason")]
    public string DisconnectReason { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

#region Support Classes

/// <summary>
/// Information about a session
/// </summary>
public class SessionInfo
{
    public string SessionId { get; }
    public string AgentConnectionId { get; set; }
    public string ControllerConnectionId { get; set; }
    public string AgentIp { get; set; }
    public string ControllerIp { get; set; }
    public string CurrentConnectionLogId { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime LastActivity { get; set; }
    public object Lock { get; } = new object();
    public string ControllerName { get; internal set; }
    public string ControllerSessionId { get; internal set; }
    public string AgentName { get; internal set; }
    public string AgentSessionId { get; internal set; }

    public SessionInfo(string sessionId)
    {
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }
}

/// <summary>
/// Information about a client connection
/// </summary>
public class ClientConnection
{
    public string ConnectionId { get; set; }
    public string SessionId { get; set; }
    public ClientRole Role { get; set; }
    public string IpAddress { get; set; }
    public DateTime JoinTime { get; set; }
}

/// <summary>
/// Session statistics for monitoring
/// </summary>
public class SessionStats
{
    public string SessionId { get; set; }
    public bool HasAgent { get; set; }
    public bool HasController { get; set; }
    public bool IsReady { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Extended session statistics with MongoDB data
/// </summary>
public class SessionStatsExtended : SessionStats
{
    public int TotalConnections { get; set; }
    public int TotalDuration { get; set; }
    public string Status { get; set; }
}

#endregion