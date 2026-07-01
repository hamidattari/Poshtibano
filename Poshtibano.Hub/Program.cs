using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Console;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Poshtibano.Common;
using Poshtibano.Hub;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://127.0.0.1:27017";
var mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "PoshtibanoDesk";

builder.Services.AddSingleton<IMongoClient>(s => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});
builder.Services.AddSingleton<MongoDbHealthCheck>();
builder.Services.AddSingleton<MongoDbService>();

// Configure SignalR with improved settings
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1 * 1024 * 1024; // 1MB for large file transfers
    options.StreamBufferCapacity = 20;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // Increased timeout
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddNewtonsoftJsonProtocol();

// Add CORS for development (optional, adjust for production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

var mongoHealthCheck = app.Services.GetRequiredService<MongoDbHealthCheck>();
var isConnected = await mongoHealthCheck.IsConnectedAsync();

if (isConnected)
{
    // Initialize MongoDB indexes
    var mongoService = app.Services.GetRequiredService<MongoDbService>();
    await mongoService.InitializeIndexesAsync();
    Console.WriteLine($"[{DateTime.Now}] ✅ MongoDB index has been created.");
}
else
{
    Console.WriteLine($"[{DateTime.Now}] ⚠️ MongoDB is not connected. System is working without Database");
}


app.UseCors("AllowAll");
app.MapHub<SessionHub>("/hub");

Console.WriteLine($"[{DateTime.Now}] ✅ Server started on http://0.0.0.0:5000");
Console.WriteLine($"[{DateTime.Now}] SignalR Hub available at:  http://0.0.0.0:5000/hub");

app.Run("http://0.0.0.0:5000");

/// <summary>
/// Handshake context for each session during authentication
/// </summary>
public class SessionHandshakeContext
{
    public bool HasPassword { get; set; }
    public string SubmittedPassword { get; set; }
    public bool PasswordVerified { get; set; }
    public bool AccessRequested { get; set; }
    public bool AccessGranted { get; set; }
    public HandshakeState State { get; set; } = HandshakeState.Idle;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public object Lock { get; } = new object();
}

public enum HandshakeState
{
    Idle,
    WaitingForPasswordInfo,
    WaitingForPassword,
    PasswordVerified,
    WaitingForAccess,
    AccessGranted,
    Failed
}

/// <summary>
/// SignalR Hub for managing remote desktop sessions
/// Handles peer-to-peer signaling between Agent and Controller
/// </summary>
public class SessionHub : Hub
{
    private static readonly ConcurrentDictionary<string, SessionInfo> Sessions = new();
    private static readonly ConcurrentDictionary<string, ClientConnection> Connections = new();
    private static readonly ConcurrentDictionary<string, SessionHandshakeContext> Handshakes = new();

    private readonly ILogger<SessionHub> _logger;
    private readonly MongoDbService _mongoService;

    public SessionHub(ILogger<SessionHub> logger, MongoDbService mongoService)
    {
        Console.OutputEncoding = Encoding.UTF8;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mongoService = mongoService ?? throw new ArgumentNullException(nameof(mongoService));
    }

    private string GetCleanIpAddress()
    {
        var remoteIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;

        if (remoteIp == null)
            return "Unknown";

        if (remoteIp.IsIPv4MappedToIPv6)
        {
            return remoteIp.MapToIPv4().ToString();
        }

        return remoteIp.ToString();
    }

    /// <summary>
    /// Client joins a session with a specific role (Agent or Controller)
    /// </summary>
    public async Task JoinSession(string sessionId, ClientRole role, string callerName, string callerSessionId)
    {
        try
        {
            var clientIp = GetCleanIpAddress();
            var connectionId = Context.ConnectionId;

            // Store connection info
            Context.Items["sessionId"] = sessionId;
            Context.Items["clientIp"] = clientIp;
            Context.Items["clientRole"] = role;
            Context.Items["joinTime"] = DateTime.UtcNow;

            await Groups.AddToGroupAsync(connectionId, sessionId);

            // Create or update session
            var sessionInfo = Sessions.GetOrAdd(sessionId, _ => new SessionInfo(sessionId));

            // Register connection
            var connection = new ClientConnection
            {
                ConnectionId = connectionId,
                SessionId = sessionId,
                Role = role,
                IpAddress = clientIp,
                JoinTime = DateTime.UtcNow
            };

            Connections[connectionId] = connection;

            if (role == ClientRole.Controller)
            {
                _logger.LogInformation($"🔄 Controller joined. Resetting handshake state for session {sessionId}");
                Handshakes.TryRemove(sessionId, out _);
            }

            // Add to session
            lock (sessionInfo.Lock)
            {
                if (role == ClientRole.Agent)
                {
                    if (sessionInfo.AgentConnectionId != null && sessionInfo.AgentConnectionId != connectionId)
                    {
                        _logger.LogWarning($"Agent already exists in session {sessionId}. ChangeRoleRequest to Controller for old Agent.");
                        //Clients.Client(sessionInfo.AgentConnectionId).SendAsync("SessionEnded", "replaced");
                        Clients.Client(sessionInfo.AgentConnectionId).SendAsync("ChangeRoleRequest", ClientRole.Controller);

                        sessionInfo.ControllerConnectionId = sessionInfo.AgentConnectionId;
                        sessionInfo.ControllerIp = sessionInfo.AgentIp;
                        sessionInfo.ControllerName = sessionInfo.AgentName;
                        sessionInfo.ControllerSessionId = sessionInfo.AgentSessionId;
                    }

                    sessionInfo.AgentConnectionId = connectionId;
                    sessionInfo.AgentIp = clientIp;
                    sessionInfo.AgentName = callerName;
                    sessionInfo.AgentSessionId = callerSessionId;
                }
                else if (role == ClientRole.Controller)
                {
                    if (sessionInfo.ControllerConnectionId != null && sessionInfo.ControllerConnectionId != connectionId)
                    {
                        _logger.LogWarning($"Controller already exists in session {sessionId}. ChangeRoleRequest to Agent for old Controller.");
                        //Clients.Client(sessionInfo.ControllerConnectionId).SendAsync("SessionEnded", "replaced");
                        Clients.Client(sessionInfo.ControllerConnectionId).SendAsync("ChangeRoleRequest", ClientRole.Agent);

                        sessionInfo.AgentConnectionId = sessionInfo.ControllerConnectionId;
                        sessionInfo.AgentIp = sessionInfo.ControllerIp;
                        sessionInfo.AgentName = sessionInfo.ControllerName;
                        sessionInfo.AgentSessionId = sessionInfo.ControllerSessionId;
                    }

                    sessionInfo.ControllerConnectionId = connectionId;
                    sessionInfo.ControllerIp = clientIp;
                    sessionInfo.ControllerName = callerName;
                    sessionInfo.ControllerSessionId = callerSessionId;
                }

                sessionInfo.LastActivity = DateTime.UtcNow;
            }

            _logger.LogInformation($"✅ {role} ({clientIp}) joined session {sessionId} [ConnectionId: {connectionId}]");

            // ============================================================
            // 🔑 NEW: Check if both peers are present - START HANDSHAKE
            // ============================================================
            bool isReady;
            string agentIp = null;
            string controllerIp = null;
            string agentConnId = null;
            string controllerConnId = null;

            lock (sessionInfo.Lock)
            {
                isReady = !string.IsNullOrEmpty(sessionInfo.AgentConnectionId) &&
                          !string.IsNullOrEmpty(sessionInfo.ControllerConnectionId);

                if (isReady)
                {
                    agentIp = sessionInfo.AgentIp;
                    controllerIp = sessionInfo.ControllerIp;
                    agentConnId = sessionInfo.AgentConnectionId;
                    controllerConnId = sessionInfo.ControllerConnectionId;
                }
            }

            if (isReady)
            {
                _logger.LogInformation($"🔐 Session {sessionId} has both Agent and Controller.  Starting handshake...");

                // Create handshake context
                var handshake = Handshakes.GetOrAdd(sessionId, _ => new SessionHandshakeContext());

                lock (handshake.Lock)
                {
                    handshake.State = HandshakeState.WaitingForPasswordInfo;
                    handshake.PasswordVerified = false;
                    handshake.AccessGranted = false;
                    handshake.HasPassword = false;
                }

                // Step 1: Request password info from Agent
                _logger.LogInformation($"📋 Requesting password info from Agent");
                await Clients.Client(agentConnId).SendAsync("RequestPasswordInfo");

                _logger.LogInformation($"⏳ Session {sessionId} waiting for Agent password info");
            }
            else
            {
                _logger.LogInformation($"⏳ Session {sessionId} waiting for {(role == ClientRole.Agent ? "Controller" : "Agent")}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in JoinSession for session {sessionId}");
            throw;
        }
    }

    /// <summary>
    /// Agent responds with password info (has password or not)
    /// </summary>
    public async Task SubmitPasswordInfo(string sessionId, bool hasPassword)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation($"🔐 Agent submitted password info:  hasPassword={hasPassword}");

            if (!Handshakes.TryGetValue(sessionId, out var handshake))
            {
                _logger.LogWarning($"No handshake context for session {sessionId}");
                return;
            }

            lock (handshake.Lock)
            {
                if (handshake.State != HandshakeState.WaitingForPasswordInfo)
                {
                    _logger.LogWarning($"Invalid handshake state:  {handshake.State}");
                    return;
                }

                handshake.HasPassword = hasPassword;

                if (hasPassword)
                {
                    handshake.State = HandshakeState.WaitingForPassword;
                }
                else
                {
                    handshake.State = HandshakeState.PasswordVerified;
                }
            }

            // Get Controller connection ID
            if (!Sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                _logger.LogWarning($"Session {sessionId} not found");
                return;
            }

            string controllerConnId;
            lock (sessionInfo.Lock)
            {
                controllerConnId = sessionInfo.ControllerConnectionId;
            }

            if (handshake.HasPassword)
            {
                // Step 2: Request password from Controller
                _logger.LogInformation($"🔐 Requesting password from Controller");
                await Clients.Client(controllerConnId).SendAsync("RequestPassword", sessionInfo.AgentName, sessionInfo.AgentSessionId);
            }
            else
            {
                // Skip password, go to access request
                _logger.LogInformation($"✅ No password required, requesting access permission from Agent");
                await Clients.Client(connectionId).SendAsync("RequestAccessPermission", sessionInfo.ControllerName, sessionInfo.ControllerSessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in SubmitPasswordInfo for session {sessionId}");
        }
    }

    /// <summary>
    /// Controller submits password
    /// </summary>
    public async Task SubmitPassword(string sessionId, string password)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation($"🔐 Controller submitted password (length: {password?.Length ?? 0})");

            if (!Handshakes.TryGetValue(sessionId, out var handshake))
            {
                _logger.LogWarning($"No handshake context for session {sessionId}");
                return;
            }

            lock (handshake.Lock)
            {
                if (handshake.State != HandshakeState.WaitingForPassword)
                {
                    _logger.LogWarning($"Invalid handshake state: {handshake.State}");
                    return;
                }

                handshake.SubmittedPassword = password;
            }

            // Get Agent and Controller connections
            if (!Sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                _logger.LogWarning($"Session {sessionId} not found");
                return;
            }

            string agentConnId;
            lock (sessionInfo.Lock)
            {
                agentConnId = sessionInfo.AgentConnectionId;
            }

            // ✅ TODO: Implement actual password verification
            // For now, we'll assume password is always correct if provided
            // In production, you would: 
            // 1. Store password hash in Agent config/database
            // 2. Compare submitted password with stored hash
            // 3. Or ask Agent to verify the password

            // For this implementation, we'll send password to Agent for verification
            _logger.LogInformation($"🔐 Forwarding password to Agent for verification");
            await Clients.Client(agentConnId).SendAsync("VerifyPassword", password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in SubmitPassword for session {sessionId}");
        }
    }

    /// <summary>
    /// Agent verifies password and responds
    /// </summary>
    public async Task SubmitPasswordVerification(string sessionId, bool isCorrect)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation($"🔐 Agent password verification: isCorrect={isCorrect}");

            if (!Handshakes.TryGetValue(sessionId, out var handshake))
            {
                _logger.LogWarning($"No handshake context for session {sessionId}");
                return;
            }

            if (!Sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                _logger.LogWarning($"Session {sessionId} not found");
                return;
            }

            string controllerConnId;
            string agentConnId;
            lock (sessionInfo.Lock)
            {
                controllerConnId = sessionInfo.ControllerConnectionId;
                agentConnId = sessionInfo.AgentConnectionId;
            }

            lock (handshake.Lock)
            {
                if (!isCorrect)
                {
                    handshake.State = HandshakeState.Failed;
                    handshake.PasswordVerified = false;
                    handshake.AccessGranted = false;
                    handshake.HasPassword = false;
                    _logger.LogWarning($"❌ Password incorrect for session {sessionId}");
                }
                else
                {
                    handshake.PasswordVerified = true;
                    handshake.State = HandshakeState.PasswordVerified;
                    _logger.LogInformation($"✅ Password verified for session {sessionId}");
                }
            }

            if (!isCorrect)
            {
                // Notify Controller - password incorrect
                _logger.LogInformation($"❌ Sending password incorrect notification to Controller");
                await Clients.Client(controllerConnId).SendAsync("PasswordIncorrect");

                // Disconnect Controller
                await Groups.RemoveFromGroupAsync(controllerConnId, sessionId);
                await Clients.Client(controllerConnId).SendAsync("SessionEnded", "passwordIncorrect");

                // Clean up
                Connections.TryRemove(controllerConnId, out _);
                lock (sessionInfo.Lock)
                {
                    sessionInfo.ControllerConnectionId = null;
                }

                return;
            }
            else
            {
                await Clients.Client(controllerConnId).SendAsync("PasswordCorrect");
            }

            // Password correct - request access permission from Agent
            _logger.LogInformation($"🔐 Password verified, requesting access permission from Agent");
            await Clients.Client(connectionId).SendAsync("RequestAccessPermission", sessionInfo.ControllerName, sessionInfo.ControllerSessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in SubmitPasswordVerification for session {sessionId}");
        }
    }

    /// <summary>
    /// Agent grants or denies access
    /// </summary>
    public async Task SubmitAccessResponse(string sessionId, bool allowed)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation($"🔐 Agent access response: allowed={allowed}");

            if (!Handshakes.TryGetValue(sessionId, out var handshake))
            {
                _logger.LogWarning($"No handshake context for session {sessionId}");
                return;
            }

            if (!Sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                _logger.LogWarning($"Session {sessionId} not found");
                return;
            }

            string agentConnId;
            string controllerConnId;
            string agentIp;
            string controllerIp;

            lock (sessionInfo.Lock)
            {
                agentConnId = sessionInfo.AgentConnectionId;
                controllerConnId = sessionInfo.ControllerConnectionId;
                agentIp = sessionInfo.AgentIp;
                controllerIp = sessionInfo.ControllerIp;
            }

            lock (handshake.Lock)
            {
                handshake.AccessGranted = allowed;
                handshake.State = allowed ? HandshakeState.AccessGranted : HandshakeState.Failed;

                if (!allowed)
                {
                    handshake.PasswordVerified = false;
                    handshake.AccessGranted = false;
                    handshake.HasPassword = false;
                }
            }

            if (!allowed)
            {
                _logger.LogInformation($"❌ Access denied for session {sessionId}");

                // Notify Controller - access denied
                await Clients.Client(controllerConnId).SendAsync("AccessDenied");

                // Disconnect Controller
                await Groups.RemoveFromGroupAsync(controllerConnId, sessionId);
                await Clients.Client(controllerConnId).SendAsync("SessionEnded", "accessDenied");

                // Clean up
                Connections.TryRemove(controllerConnId, out _);
                lock (sessionInfo.Lock)
                {
                    sessionInfo.ControllerConnectionId = null;
                }

                return;
            }

            // Access granted - record connection in MongoDB
            _logger.LogInformation($"✅ Access granted for session {sessionId}, recording connection");
            var connectionLog = await _mongoService.RecordConnectionAsync(
                sessionId,
                agentConnId,
                controllerConnId,
                agentIp,
                controllerIp);

            lock (sessionInfo.Lock)
            {
                sessionInfo.CurrentConnectionLogId = connectionLog.Id;
            }

            // Send sessionReady to both peers
            _logger.LogInformation($"🎉 Sending sessionReady to both peers");
            await Clients.Group(sessionId).SendAsync("ReceiveMessage",
                JsonSerializer.Serialize(new { type = "sessionReady" }));

            _logger.LogInformation($"🎉 Session {sessionId} is READY (handshake complete)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in SubmitAccessResponse for session {sessionId}");
        }
    }

    /// <summary>
    /// Relay arbitrary data between peers (legacy support)
    /// </summary>
    public async Task RelayData(string sessionId, byte[] data)
    {
        try
        {
            _logger.LogDebug($"📦 Relaying {data?.Length ?? 0} bytes in session {sessionId}");
            await Clients.OthersInGroup(sessionId).SendAsync("ReceiveRelayData", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error relaying data in session {sessionId}");
        }
    }

    /// <summary>
    /// Send SDP Offer (Agent -> Controller)
    /// </summary>
    public async Task SendSdpOffer(string sessionId, string sdpJson)
    {
        try
        {
            _logger.LogInformation($"📤 SDP Offer sent in session {sessionId}");
            await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("ReceiveSdpOffer", sdpJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending SDP Offer in session {sessionId}");
        }
    }

    /// <summary>
    /// Send SDP Answer (Controller -> Agent)
    /// </summary>
    public async Task SendSdpAnswer(string sessionId, string sdpJson)
    {
        try
        {
            _logger.LogInformation($"📤 SDP Answer sent in session {sessionId}");
            await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("ReceiveSdpAnswer", sdpJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending SDP Answer in session {sessionId}");
        }
    }

    /// <summary>
    /// Send ICE Candidate
    /// </summary>
    public async Task SendIceCandidate(string sessionId, string candidateJson)
    {
        try
        {
            _logger.LogDebug($"🧊 ICE Candidate sent in session {sessionId}");
            await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("ReceiveIceCandidate", candidateJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending ICE Candidate in session {sessionId}");
        }
    }

    /// <summary>
    /// Send generic message (access requests, etc.)
    /// </summary>
    public async Task SendMessage(string sessionId, string messageJson)
    {
        try
        {
            _logger.LogDebug($"💬 Message sent in session {sessionId}");

            // ✅ Parse message to check for rejoin
            try
            {
                var msgObj = JsonSerializer.Deserialize<Dictionary<string, object>>(messageJson);
                if (msgObj != null && msgObj.ContainsKey("type") && msgObj["type"].ToString() == "client_rejoin")
                {
                    _logger.LogInformation($"🔄 Client rejoin detected in session {sessionId}");

                    // Check if both peers are present
                    if (Sessions.TryGetValue(sessionId, out var sessionInfo))
                    {
                        bool isReady;
                        string agentIp = null;
                        string controllerIp = null;
                        string agentConnId = null;
                        string controllerConnId = null;

                        lock (sessionInfo.Lock)
                        {
                            isReady = !string.IsNullOrEmpty(sessionInfo.AgentConnectionId) &&
                                     !string.IsNullOrEmpty(sessionInfo.ControllerConnectionId);

                            if (isReady)
                            {
                                agentIp = sessionInfo.AgentIp;
                                controllerIp = sessionInfo.ControllerIp;
                                agentConnId = sessionInfo.AgentConnectionId;
                                controllerConnId = sessionInfo.ControllerConnectionId;
                            }
                        }

                        if (isReady)
                        {
                            // ✅ Record new connection in MongoDB
                            var connectionLog = await _mongoService.RecordConnectionAsync(
                                sessionId,
                                agentConnId,
                                controllerConnId,
                                agentIp,
                                controllerIp);

                            lock (sessionInfo.Lock)
                            {
                                sessionInfo.CurrentConnectionLogId = connectionLog.Id;
                            }

                            // ✅ Send sessionReady to both peers
                            await Clients.Group(sessionId).SendAsync("ReceiveMessage",
                                JsonSerializer.Serialize(new { type = "sessionReady" }));
                            _logger.LogInformation($"🎉 sessionReady triggered by rejoin in session {sessionId}");
                        }
                    }
                }
            }
            catch
            {
                // Not a structured message, just relay
            }

            await Clients.GroupExcept(sessionId, Context.ConnectionId).SendAsync("ReceiveMessage", messageJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending message in session {sessionId}");
        }
    }

    /// <summary>
    /// Handle client disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var sessionId = Context.Items["sessionId"] as string;
        var clientIp = Context.Items["clientIp"] as string;
        var clientRole = Context.Items["clientRole"] as ClientRole?;

        try
        {
            if (exception != null)
            {
                _logger.LogWarning($"⚠️ Client disconnected with error: {exception.Message}");
            }

            if (sessionId != null && clientRole.HasValue)
            {
                await Clients.Group(sessionId).SendAsync("SessionEnded", "manualDisconnect");

                _logger.LogInformation($"👋 {clientRole.Value} ({clientIp}) disconnected from session {sessionId}");

                // Remove connection
                Connections.TryRemove(connectionId, out _);

                // Update session
                if (Sessions.TryGetValue(sessionId, out var sessionInfo))
                {
                    bool shouldRemoveSession = false;
                    string connectionLogId = null;
                    string disconnectReason = exception?.Message ?? "normal_disconnect";

                    lock (sessionInfo.Lock)
                    {
                        if (Handshakes.TryGetValue(sessionId, out var handshake))
                        {
                            lock (handshake.Lock)
                            {
                                handshake.State = HandshakeState.WaitingForPasswordInfo;
                                handshake.PasswordVerified = false;
                                handshake.AccessGranted = false;
                                handshake.HasPassword = false;
                            }
                        }

                        if (clientRole == ClientRole.Agent && sessionInfo.AgentConnectionId == connectionId)
                        {
                            sessionInfo.AgentConnectionId = null;
                            _logger.LogInformation($"🔴 Agent disconnected from session {sessionId}");
                        }
                        else if (clientRole == ClientRole.Controller && sessionInfo.ControllerConnectionId == connectionId)
                        {
                            sessionInfo.ControllerConnectionId = null;
                            _logger.LogInformation($"🔴 Controller disconnected from session {sessionId}");
                        }

                        // Check if session should be removed (both disconnected)
                        shouldRemoveSession = string.IsNullOrEmpty(sessionInfo.AgentConnectionId) &&
                                             string.IsNullOrEmpty(sessionInfo.ControllerConnectionId);

                        // Get connection log ID only if both disconnected
                        if (shouldRemoveSession)
                        {
                            connectionLogId = sessionInfo.CurrentConnectionLogId;
                        }
                    }

                    // Notify other peer
                    await Clients.Group(sessionId).SendAsync("PeerDisconnected");

                    // If peer connection is incomplete, notify session ended
                    bool isIncomplete;
                    lock (sessionInfo.Lock)
                    {
                        isIncomplete = string.IsNullOrEmpty(sessionInfo.AgentConnectionId) ||
                                      string.IsNullOrEmpty(sessionInfo.ControllerConnectionId);
                    }

                    if (isIncomplete && !shouldRemoveSession)
                    {
                        //await Clients.Group(sessionId).SendAsync("SessionEnded", "peerDisconnected");
                        //_logger.LogInformation($"📢 Session {sessionId} ended (peer disconnected)");
                        shouldRemoveSession = true;
                    }

                    // ✅ Record disconnection in MongoDB ONLY when both disconnected
                    if (shouldRemoveSession)
                    {
                        if (!string.IsNullOrEmpty(connectionLogId))
                        {
                            await _mongoService.RecordDisconnectionAsync(sessionId, connectionLogId, disconnectReason);
                            _logger.LogInformation($"💾 Session {sessionId} connection recorded in MongoDB");
                        }

                        // Clean up handshake context
                        Handshakes.TryRemove(sessionId, out _);

                        Sessions.TryRemove(sessionId, out _);
                        _logger.LogInformation($"🗑️ Session {sessionId} removed (all peers disconnected)");

                        await Clients.Group(sessionId).SendAsync("SessionEnded", "peerDisconnected");
                        _logger.LogInformation($"📢 Session {sessionId} ended (peer disconnected)");
                    }
                }

                await Groups.RemoveFromGroupAsync(connectionId, sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling disconnection for {connectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get session statistics (for monitoring/debugging)
    /// </summary>
    public async Task<SessionStatsExtended> GetSessionStats(string sessionId)
    {
        if (Sessions.TryGetValue(sessionId, out var sessionInfo))
        {
            lock (sessionInfo.Lock)
            {
                var stats = new SessionStatsExtended
                {
                    SessionId = sessionId,
                    HasAgent = !string.IsNullOrEmpty(sessionInfo.AgentConnectionId),
                    HasController = !string.IsNullOrEmpty(sessionInfo.ControllerConnectionId),
                    IsReady = !string.IsNullOrEmpty(sessionInfo.AgentConnectionId) &&
                             !string.IsNullOrEmpty(sessionInfo.ControllerConnectionId),
                    LastActivity = sessionInfo.LastActivity
                };

                return stats;
            }
        }

        // If not in memory, try to get from MongoDB
        var sessionDoc = await _mongoService.GetSessionAsync(sessionId);
        if (sessionDoc != null)
        {
            return new SessionStatsExtended
            {
                SessionId = sessionId,
                HasAgent = false,
                HasController = false,
                IsReady = false,
                LastActivity = sessionDoc.LastDisconnectedAt ?? sessionDoc.LastConnectedAt,
                TotalConnections = sessionDoc.TotalConnections,
                TotalDuration = sessionDoc.TotalDuration,
                Status = sessionDoc.Status
            };
        }

        return null;
    }

    /// <summary>
    /// Get connection history for a session
    /// </summary>
    public async Task<List<ConnectionLogDocument>> GetConnectionHistory(string sessionId)
    {
        return await _mongoService.GetConnectionLogsAsync(sessionId);
    }
}