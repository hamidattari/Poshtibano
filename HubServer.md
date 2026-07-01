# Poshtibano Desk Hub Server Architecture

> **Focus:** SignalR Hub Server Architecture  
> **Communication:** WebRTC Signaling + Session Management  
> **Database:** MongoDB  

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Hub Server Components](#2-hub-server-components)
3. [Session Handshake](#3-session-handshake)
4. [Connection Management](#4-connection-management)
5. [Database Schema](#5-database-schema)
6. [SignalR Methods](#6-signalr-methods)
7. [Data Flow Diagrams](#7-data-flow-diagrams)
8. [State Machines](#8-state-machines)
9. [Error Handling](#9-error-handling)
10. [Monitoring & Logging](#10-monitoring--logging)

## 1. System Architecture

### 1.1 Hub Server Three-Layer Architecture

```
┌───────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                           │
│                                                           │
│  ┌─────────────────────┐        ┌─────────────────────┐   │
│  │   Agent Client      │        │  Controller Client  │   │
│  │  (Remote Desktop)   │        │  (Controlling)      │   │
│  │                     │        │                     │   │
│  │ • WebRTC P2P        │        │ • WebRTC P2P        │   │
│  │ • SignalR Hub conn  │        │ • SignalR Hub conn  │   │
│  └────────┬────────────┘        └───────┬─────────────┘   │
│           │                             │                 │
│           │  SignalR WebSocket          │                 │
│           │  (over HTTPS/HTTP)          │                 │
│           └───────────────┬─────────────┘                 │
│                           │                               │
└───────────────────────────┼───────────────────────────────┘
                            │
┌───────────────────────────▼───────────────────────────────┐
│               HUB SERVER LAYER                            │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐  │
│  │          SessionHub (SignalR Hub)                   │  │
│  │                                                     │  │
│  │  • JoinSession()                                    │  │
│  │  • SendSdpOffer/Answer()                            │  │
│  │  • SendIceCandidate()                               │  │
│  │  • SendMessage()                                    │  │
│  │  • Handshake methods                                │  │
│  │  • Disconnection handling                           │  │
│  │                                                     │  │
│  │  Static Collections:                                │  │
│  │  • Sessions (ConcurrentDictionary)                  │  │
│  │  • Connections (ConcurrentDictionary)               │  │
│  │  • Handshakes (ConcurrentDictionary)                │  │
│  └─────────────────────────────────────────────────────┘  │
│                           │                               │
└───────────────────────────┼───────────────────────────────┘
                            │
┌───────────────────────────▼───────────────────────────────┐
│              DATABASE LAYER                               │
│                                                           │
│  ┌─────────────────────────────────────────────────────┐  │
│  │  MongoDbService                                     │  │
│  │  • RecordConnectionAsync()                          │  │
│  │  • RecordDisconnectionAsync()                       │  │
│  │  • GetSessionAsync()                                │  │
│  │  • GetConnectionLogsAsync()                         │  │
│  │  • InitializeIndexesAsync()                         │  │
│  │                                                     │  │
│  │  Collections:                                       │  │
│  │  • sessions (SessionDocument)                       │  │
│  │  • connection_logs (ConnectionLogDocument)          │  │
│  │                                                     │  │
│  │  ┌────────────────────────────────────────────────┐ │  │
│  │  │  MongoDbHealthCheck                            │ │  │
│  │  │  • IsConnectedAsync()                          │ │  │
│  │  │  • IsConnected property                        │ │  │
│  │  └────────────────────────────────────────────────┘ │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                           │
│  MongoDB Instance                                         │
│  (Host: 127.0.0.1, Port: 27017)                           │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

### 1.2 Key Responsibilities

| Layer        | Component          | Responsibility                               |
|--------------|--------------------|----------------------------------------------|
| **Client**   | Agent              | Initiates connection, accepts remote control |
| **Client**   | Controller         | Connects to Agent, controls remotely         |
| **Hub**      | SessionHub         | Coordinates signaling, manages sessions      |
| **Hub**      | Session Context    | Tracks Agent & Controller connections        |
| **Hub**      | Handshake Context  | Manages authentication flow                  |
| **Database** | MongoDbService     | Persists connection history                  |
| **Database** | MongoDbHealthCheck | Monitors database connectivity               |

## 2. Hub Server Components

### 2.1 Core Classes & Data Structures

#### SessionInfo (In-Memory Session)

| Property                 | Type     | Purpose                               |
|--------------------------|----------|---------------------------------------|
| `SessionId`              | string   | Unique session identifier             |
| `AgentConnectionId`      | string   | WebSocket connection ID of Agent      |
| `ControllerConnectionId` | string   | WebSocket connection ID of Controller |
| `AgentIp`                | string   | Agent IP address                      |
| `ControllerIp`           | string   | Controller IP address                 |
| `AgentName`              | string   | Agent display name                    |
| `ControllerName`         | string   | Controller display name               |
| `AgentSessionId`         | string   | Agent's client-side session ID        |
| `ControllerSessionId`    | string   | Controller's client-side session ID   |
| `LastActivity`           | DateTime | Last interaction timestamp            |
| `CurrentConnectionLogId` | string   | MongoDB connection log ID             |
| `Lock`                   | object   | Thread-safety lock                    |

#### ClientConnection (Connection Metadata)

| Property       | Type       | Purpose                 |
|----------------|------------|-------------------------|
| `ConnectionId` | string     | WebSocket connection ID |
| `SessionId`    | string     | Associated session      |
| `Role`         | ClientRole | Agent or Controller     |
| `IpAddress`    | string     | Client IP address       |
| `JoinTime`     | DateTime   | When joined             |

#### SessionHandshakeContext (Authentication State)



#### HandshakeState Enum

```
Idle
  ↓
WaitingForPasswordInfo
  ├─→ (Has password)
  │   ↓
  │   WaitingForPassword
  │   ├─→ (Correct)
  │   │   ↓
  │   │   PasswordVerified
  │   │   ↓
  │   └─→ (Incorrect)
  │       ↓
  │       Failed
  │
  └─→ (No password)
      ↓
      PasswordVerified
      ↓
  WaitingForAccess
      ├─→ (Allowed)
      │   ↓
      │   AccessGranted
      │
      └─→ (Denied)
          ↓
          Failed
```

### 2.2 Static Collections in SessionHub

```
┌────────────────────────────────────────────────────────┐
│  IN-MEMORY DATA STRUCTURES                             │
├────────────────────────────────────────────────────────┤
│                                                        │
│  Sessions (ConcurrentDictionary)                       │
│  ├─ Key: SessionId (string)                            │
│  └─ Value: SessionInfo                                 │
│     └─ Tracks: Agent & Controller connections          │
│                                                        │
│  Connections (ConcurrentDictionary)                    │
│  ├─ Key: ConnectionId (WebSocket ID)                   │
│  └─ Value: ClientConnection                            │
│     └─ Maps: WebSocket → Session & Role                │
│                                                        │
│  Handshakes (ConcurrentDictionary)                     │
│  ├─ Key: SessionId (string)                            │
│  └─ Value: SessionHandshakeContext                     │
│     └─ Manages: Auth state & permissions               │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### 2.3 SignalR Configuration

| Setting                       | Value                        | Purpose              |
|-------------------------------|------------------------------|----------------------|
| **EnableDetailedErrors**      | true                         | Debug logging        |
| **MaximumReceiveMessageSize** | 1 MB                         | Large file transfers |
| **StreamBufferCapacity**      | 20                           | Stream buffering     |
| **ClientTimeoutInterval**     | 60s                          | Inactivity timeout   |
| **HandshakeTimeout**          | 30s                          | Connection setup     |
| **KeepAliveInterval**         | 15s                          | Connection alive     |
| **Protocol**                  | NewtonsoftJson + MessagePack | Serialization        |

---

## 3. Session Handshake

### 3.1 Authentication Flow Diagram

#### JoinSession protocol

Each client first connects as ClientRole.Controller, 
then the real controller requests the other controller to change its role via ChangeRoleRequest(ClientRole.Agent).

```
┌──────────────────────────────────────────────────────────────────┐
│              COMPLETE HANDSHAKE FLOW                             │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  STEP 1: Both Peers Connected                                    │
│  ────────────────────────────                                    │
│                                                                  │
│  Controller             Hub              Controller              │
│ (Act as Agent after      │                   │                   │
│  Second Controller       │                   │                   │
│  Join the same sessionId)│                   │                   │
│    │                     │                   │                   │
│    ├─ JoinSession        ─►                  │                   │
│    │                     │  ◄─ JoinSession ──┤                   │
│    │                     │                   │                   │
│    │ ◄─ChangeRoleRequest ┤                   │                   │
│    │                     │                   │                   │
│    │(Change Role to Agent)                   │                   │
│    │                     │                   │                   │
│    │ Both connected → Start handshake        │                   │
│    │                     │                   │                   │
│    │                     │            ┌──────┘                   │
│                                                                  │
│  STEP 2: Request Password Info                                   │
│  ────────────────────────────────                                │
│                                                                  │
│    │              │                   │                          │
│    │◄─ RequestPasswordInfo ────────── │                          │
│    │              │                   │                          │
│    ├─ SubmitPasswordInfo ──►          │                          │
│    │              │  hasPassword=?    │                          │
│    │              │                   │                          │
│                                                                  │
│  STEP 3A: With Password                                          │
│  ─────────────────────────                                       │
│                                                                  │
│    │              │                   │                          │
│    │              ├─ RequestPassword ─►                          │
│    │              │                   │                          │
│    │              │  ◄─ SubmitPassword ───                       │
│    │              │                   │                          │
│    │◄─ VerifyPassword ──────────────  │                          │
│    │              │                   │                          │
│    ├─ SubmitPasswordVerification ──►  │                          │
│    │              │  isCorrect=?      │                          │
│    │              │                   │                          │
│    │              ├─ PasswordCorrect ─►   (if true)              │
│    │              │  or PasswordIncorrect  (if false)            │
│    │              │                   │                          │
│                                                                  │
│  STEP 3B: Without Password                                       │
│  ──────────────────────────                                      │
│  (Skip to Step 4)                                                │
│                                                                  │
│                                                                  │
│  STEP 4: Request Access Permission                               │
│  ──────────────────────────────────                              │
│                                                                  │
│    │              │                   │                          │
│    │◄─ RequestAccessPermission ────── │                          │
│    │              │                   │                          │
│    ├─ SubmitAccessResponse ──►        │                          │
│    │              │  allowed=?        │                          │
│    │              │                   │                          │
│    │              ├─ AccessDenied ───► (if denied)               │
│    │              │                   │ → Disconnect             │
│    │              │                   │                          │
│    │              ├─ sessionReady ───► (if allowed)              │
│    │              │                   │ → Record in MongoDB      │
│    │              │                   │                          │
│    ✅ READY      ✅ READY            ✅ READY                   │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 3.2 Handshake State Transitions

| Current State            | Trigger                              | Next State               | Action                                      |
|--------------------------|--------------------------------------|--------------------------|---------------------------------------------|
| `Idle`                   | Both peers join                      | `WaitingForPasswordInfo` | Send `RequestPasswordInfo()`                |
| `WaitingForPasswordInfo` | Agent responds (no pwd)              | `PasswordVerified`       | Skip to access request                      |
| `WaitingForPasswordInfo` | Agent responds (has pwd)             | `WaitingForPassword`     | Send `RequestPassword()`                    |
| `WaitingForPassword`     | Controller submits pwd               | `PasswordVerified`       | Forward to Agent for verification           |
| `PasswordVerified`       | Verification success                 | `WaitingForAccess`       | Request access from Agent                   |
| `PasswordVerified`       | Verification failure                 | `Failed`                 | Disconnect Controller                       |
| `WaitingForAccess`       | Agent grants access                  | `AccessGranted`          | Record in MongoDB, send `sessionReady`      |
| `WaitingForAccess`       | Agent denies access                  | `Failed`                 | Disconnect Controller                       |

## 4. Connection Management

### 4.1 JoinSession Flow

```
┌────────────────────────────────────────────────────┐
│  JoinSession(sessionId, role, callerName)          │
│                                                    │
│  1. Extract Context                                │
│     ├─ connectionId (WebSocket)                    │
│     ├─ clientIp (Remote address)                   │
│     └─ clientRole (Agent/Controller)               │
│                                                    │
│  2. Register Connection                            │
│     ├─ Add to group: sessionId                     │
│     ├─ Store in Connections dict                   │
│     └─ Context.Items for tracking                  │
│                                                    │
│  3. Get or Create Session                          │
│     ├─ Sessions.GetOrAdd(sessionId, ...)           │
│     └─ Lock for thread-safety                      │
│                                                    │
│  4. Update Session Info                            │
│     ├─ if Agent:                                   │
│     │  ├─ Disconnect old Agent if exists           │
│     │  ├─ Promote old Agent to Controller          │
│     │  └─ Register new Agent                       │
│     │                                              │
│     └─ if Controller:                              │
│        ├─ Disconnect old Controller if exists      │
│        ├─ Promote old Controller to Agent          │
│        └─ Register new Controller                  │
│                                                    │
│  5. Check Session Readiness                        │
│     ├─ Are both Agent & Controller present?        │
│     │                                              │
│     ├─ YES: START HANDSHAKE                        │
│     │  └─ Create handshake context                 │
│     │  └─ State = WaitingForPasswordInfo           │
│     │  └─ Send RequestPasswordInfo() to Agent      │
│     │                                              │
│     └─ NO: WAIT FOR PEER                           │
│        └─ Log: "Waiting for Controller/Agent"      │
│                                                    │
└────────────────────────────────────────────────────┘
```

### 4.2 Disconnection Flow

```
┌──────────────────────────────────────────────────┐
│  OnDisconnectedAsync(exception)                  │
│                                                  │
│  1. Extract Context                              │
│     ├─ connectionId                              │
│     ├─ sessionId                                 │
│     ├─ clientRole                                │
│     └─ clientIp                                  │
│                                                  │
│  2. Remove Connection                            │
│     ├─ Remove from Connections dict              │
│     └─ Remove from group                         │
│                                                  │
│  3. Notify Peer                                  │
│     ├─ Send SessionEnded or PeerDisconnected     │
│     └─ Reset handshake context                   │
│                                                  │
│  4. Check Session Status                         │
│     ├─ Is other peer still connected?            │
│     │                                            │
│     ├─ YES: Keep session alive                   │
│     │  └─ Waiting for reconnection               │
│     │                                            │
│     └─ NO: CLEAN UP                              │
│        ├─ Record disconnection in MongoDB        │
│        ├─ Remove from Sessions dict              │
│        ├─ Remove handshake context               │
│        └─ Send SessionEnded to group             │
│                                                  │
│  5. MongoDB Recording                            │
│     ├─ Get CurrentConnectionLogId                │
│     ├─ RecordDisconnectionAsync()                │
│     │  └─ Duration calculation                   │
│     │  └─ Update session status                  │
│     └─ Log completed                             │
│                                                  │
└──────────────────────────────────────────────────┘
```

## 5. Database Schema

### 5.1 SessionDocument (MongoDB)

```javascript
{
  _id: ObjectId,
  SessionId: string (unique index),
  
  // Agent Info
  AgentIp: string,
  
  // Controller Info
  ControllerIp: string,
  
  // Timestamps
  FirstConnectedAt: DateTime,
  LastConnectedAt: DateTime,
  LastDisconnectedAt: DateTime,
  CreatedAt: DateTime,
  UpdatedAt: DateTime,
  
  // Statistics
  TotalConnections: int,
  TotalDuration: int (seconds),
  Status: string (active | disconnected | failed)
}
```

**Index:**
- `SessionId` (unique)

### 5.2 ConnectionLogDocument (MongoDB)

```javascript
{
  _id: ObjectId,
  SessionId: string (indexed with ConnectedAt),
  
  // Connection IDs
  AgentConnectionId: string,
  ControllerConnectionId: string,
  
  // IP Addresses
  AgentIp: string,
  ControllerIp: string,
  
  // Timestamps
  ConnectedAt: DateTime,
  DisconnectedAt: DateTime (nullable),
  Duration: int (seconds, nullable),
  
  // Metadata
  DisconnectReason: string,
  CreatedAt: DateTime
}
```

**Index:**
- `SessionId` + `ConnectedAt` (descending)

### 5.3 Data Flow to MongoDB

```
┌──────────────────────────────────────────┐
│  SESSION CONNECTION LIFECYCLE            │
├──────────────────────────────────────────┤
│                                          │
│  Both peers ready (handshake complete)   │
│  │                                       │
│  ├─► RecordConnectionAsync()             │
│  │   └─ Insert ConnectionLogDocument     │
│  │      ├─ AgentConnectionId             │
│  │      ├─ ControllerConnectionId        │
│  │      ├─ AgentIp                       │
│  │      ├─ ControllerIp                  │
│  │      └─ ConnectedAt = now()           │
│  │                                       │
│  │   └─ Upsert SessionDocument           │
│  │      ├─ SetOnInsert: SessionId        │
│  │      ├─ SetOnInsert: FirstConnectedAt │
│  │      ├─ Set: LastConnectedAt          │
│  │      ├─ Inc: TotalConnections         │
│  │      └─ Set: Status = "active"        │
│  │                                       │
│  ✅ Connection logged (got log ID)       │
│  │                                       │
│  [Streaming & interaction...]            │
│  │                                       │
│  └─► RecordDisconnectionAsync()          │
│      └─ Update ConnectionLogDocument     │
│         ├─ DisconnectedAt = now()        │
│         ├─ Duration = (now - ConnectedAt)│
│         └─ DisconnectReason              │
│                                          │
│      └─ Update SessionDocument           │
│         ├─ LastDisconnectedAt = now()    │
│         ├─ Inc: TotalDuration            │
│         └─ Set: Status = "disconnected"  │
│                                          │
│  ✅ Disconnection logged                 │
│                                          │
└──────────────────────────────────────────┘
```

## 6. SignalR Methods

### 6.1 Session Management Methods

| Method                  | Parameters                                    | Called By    | Triggers          |
|-------------------------|-----------------------------------------------|--------------|-------------------|
| `JoinSession()`         | sessionId, role, callerName, callerSessionId  | Client       | Session creation  |
| `OnDisconnectedAsync()` | exception                                     | Hub (auto)   | Client disconnect |
| `GetSessionStats()`     | sessionId                                     | Client       | Monitoring        |
| `GetConnectionHistory()`| sessionId                                     | Client       | Audit log         |

### 6.2 Handshake Methods

| Method                        | Parameters               | Called By  | Triggers                              |
|-------------------------------|--------------------------|------------|---------------------------------------|
| `SubmitPasswordInfo()`        | sessionId, hasPassword   | Agent      | Responds to RequestPasswordInfo       |
| `SubmitPassword()`            | sessionId, password      | Controller | Responds to RequestPassword           |
| `SubmitPasswordVerification()`| sessionId, isCorrect     | Agent      | Responds to VerifyPassword            |
| `SubmitAccessResponse()`      | sessionId, allowed       | Agent      | Responds to RequestAccessPermission   |

### 6.3 WebRTC Signaling Methods

| Method              | Parameters               | Direction          | Purpose                            |
|---------------------|--------------------------|--------------------|------------------------------------|
| `SendSdpOffer()`    | sessionId, sdpJson       | Agent → Controller | Initial SDP offer                  |
| `SendSdpAnswer()`   | sessionId, sdpJson       | Controller → Agent | SDP answer                         |
| `SendIceCandidate()`| sessionId, candidateJson | Bidirectional      | ICE candidate exchange             |
| `RelayData()`       | sessionId, data          | Bidirectional      | Generic data relay (legacy)        |

### 6.4 Control Message Methods

| Method          | Parameters             | Direction     | Purpose                       |
|-----------------|------------------------|---------------|-------------------------------|
| `SendMessage()` | sessionId, messageJson | Bidirectional | Generic messages (auth, etc.) |

### 6.5 Client-Bound Callback Methods (Hub.Clients)

| Callback                  | Triggered By              | Parameters                        | Purpose                              |
|---------------------------|---------------------------|-----------------------------------|--------------------------------------|
| `RequestPasswordInfo`     | JoinSession (handshake)   | N/A                               | Ask Agent: has password?             |
| `RequestPassword`         | SubmitPasswordInfo        | agentName, agentSessionId         | Request password from Controller     |
| `VerifyPassword`          | SubmitPassword            | password                          | Ask Agent to verify password         |
| `RequestAccessPermission` | SubmitPasswordVerification| controllerName, controllerSessionId | Request access from Agent          |
| `PasswordIncorrect`       | SubmitPasswordVerification| N/A                               | Password validation failed           |
| `PasswordCorrect`         | SubmitPasswordVerification| N/A                               | Password validation passed           |
| `AccessDenied`            | SubmitAccessResponse      | N/A                               | Access permission denied             |
| `ReceiveMessage`          | SendMessage               | messageJson                       | Relay generic message                |
| `ReceiveSdpOffer`         | SendSdpOffer              | sdpJson                           | Relay SDP offer                      |
| `ReceiveSdpAnswer`        | SendSdpAnswer             | sdpJson                           | Relay SDP answer                     |
| `ReceiveIceCandidate`     | SendIceCandidate          | candidateJson                     | Relay ICE candidate                  |
| `SessionEnded`            | Various                   | reason                            | Session terminated                   |
| `PeerDisconnected`        | OnDisconnectedAsync       | N/A                               | Peer left                            |
| `ChangeRoleRequest`       | JoinSession               | role                              | Role change request                  |

---

## 7. Data Flow Diagrams

### 7.1 Complete Authentication & Connection Flow

#### JoinSession protocol

Each client first connects as ClientRole.Controller, 
then the real controller requests the other controller to change its role via ChangeRoleRequest(ClientRole.Agent).
```
TIMELINE           CONTROLLER           HUB              CONTROLLER
                   (After as Agent)
────────────────────────────────────────────────────────────────────

00:00:00           Connect
                   │
                   ├─ JoinSession("123")
                   │  {role: Controller}┌─ Add to group
                   │                    ├─ Wait for peer
                   │                    │
00:00:01                                                  Connect
                                                          │
                                                          ├─ JoinSession("456")
                                                          │  {role: Controller}
                                        ┌─ Add to group ──┤
                                        ├─ Wait for peer


00:00:02                                           Press Connect to ("123")                                                                                
                                                          │
                                     JoinSession("123")  ─┤ 
                                     {role: Controller}   │
                                                          │
             Controller already exists in session ("123") │                   
                      ChangeRoleRequest(ClientRole.Agent) │
                                to old Controller ("123") │  
        { _currentRole = role; }         ◄────────────────┤              
        { _session.ChangeRole(role); }                    │
               Swap these info in Hub sessionInfo["123"] ─┤ 
                                                          │
                 { sessionInfo.AgentConnectionId = sessionInfo.ControllerConnectionId; }
                 { sessionInfo.AgentIp = sessionInfo.ControllerIp; }
                 { sessionInfo.AgentName = sessionInfo.ControllerName; }
                 { sessionInfo.AgentSessionId = sessionInfo.ControllerSessionId; }
                 
                 { sessionInfo.ControllerConnectionId = connectionId; }
                 { sessionInfo.ControllerIp = clientIp; }
                 { sessionInfo.ControllerName = callerName; }
                 { sessionInfo.ControllerSessionId = callerSessionId;  }
                  
                    Now old Controller ─┤
                      register as Agent │
                          Add to group ─┤
                           Both ready! ─┤
                      Create handshake ─┤
                                       ─┤
                 RequestPasswordInfo() ─┤
                                        │
                   ◄────────────────────┤

00:00:03           Decide: has password?
                   │
                   ├─ SubmitPasswordInfo()
                   │  {hasPassword: true}
                                        ├─ State: WaitingForPassword
                                        ├─ RequestPassword()
                                        │────────────────────────►

00:00:04                                                  Receive password prompt
                                                         │
                                                         ├─ User enters password
                                                         │
                                                         ├─ SubmitPassword()
                                                         │  {password: "xxx"}
                                        ◄────────────────┤

00:00:05           Receive password
                   │
                   ├─ User verifies
                   │
                   ├─ SubmitPasswordVerification()
                   │  {isCorrect: true}
                                        ├─ PasswordCorrect()
                                        │────────────────────────►

00:00:06                                                  PasswordCorrect event
                                                         ├─ RequestAccessPermission()
                   ◄────────────────────┤

00:00:07           Receive access request
                   │
                   ├─ User grants access
                   │
                   ├─ SubmitAccessResponse()
                   │  {allowed: true}
                                        ├─ RecordConnectionAsync()
                                        │   (MongoDB insert)
                                        │
                                        ├─ sessionReady
                                        │────────────────────────►
                   ◄────────────────────┤

00:00:08           ✅ READY              ✅ READY          ✅ READY
                   │ SendSdpOffer()      │ Relay           │
                   ├─────────────────────┼────────────────►│
                   │ (WebRTC setup)      │                 │
                   │◄────────────────────┼─ SendSdpAnswer()│
                   │ (WebRTC setup)      │                 │
                   │ Exchange ICE Candidates
                   │                                       │
                   ├──────────────────────────────────────►│
                   │ WebRTC Direct Connection Established  │
                   │═══════════════════════════════════════

00:00:30           User stops session
                   │
                   └─ Disconnect
                                        ├─ OnDisconnected
                                        ├─ RecordDisconnectionAsync()
                                        │   (MongoDB update)
                                        ├─ SessionEnded
                                        │────────────────────────►

00:00:31           Receive SessionEnded
                   │
                   └─ Cleanup            └─ Cleanup        └─ Cleanup
                                        ├─ Remove session
                                        └─ Clean handshake

✅ SESSION COMPLETE (Duration: ~30 seconds)
```

### 7.2 Rejoin/Reconnection Flow

```
SCENARIO: Network interruption, client reconnects

PREVIOUS STATE:
  ✅ Connected, streaming active
  Session ID: "abc123"
  Agent ConnectionId: "old-agent-123"
  Controller ConnectionId: "old-controller-456"

INTERRUPTION:
  Network failure for 10 seconds

CLIENT RECONNECTS:
  └─ JoinSession(sessionId, role, ...)
     ├─ New ConnectionId: "new-agent-789"
     └─ Same sessionId: "abc123"

HUB DETECTS:
  ├─ SessionInfo exists
  ├─ Old Agent ConnectionId still in memory
  ├─ New Agent connects with same sessionId
  │
  ├─ Replace old connection:
  │  ├─ Disconnect old Agent
  │  └─ Register new Agent
  │
  └─ Check: Both Agent & Controller present?
     ├─ YES: Both ready!
     │  ├─ SendMessage("client_rejoin")
     │  │  └─ Detect rejoin in SendMessage()
     │  ├─ RecordConnectionAsync() [New MongoDB log]
     │  └─ sessionReady
     │
     └─ NO: Wait for other peer

✅ RECONNECTION COMPLETE
   (New connection log created in MongoDB)
```

---

## 8. State Machines

### 8.1 Session Handshake State Machine

```
                      ┌─────────┐
                      │  Idle   │
                      └────┬────┘
                           │
                    Both peers joined
                           │
                      ┌────▼──────────────────┐
                      │WaitingForPasswordInfo │
                      └────┬─────────┬────────┘
                           │         │
                    ┌──────┘         └──────┐
                    │                       │
              No password           Has password
                    │                       │
                    │                  ┌────▼─────────┐
                    │                  │WaitingForPwd │
                    │                  └────┬─────┬───┘
                    │                       │     │
                    │                   Correct  Wrong
                    │                       │     │
                    │                       │     ▼
                    │                       │  ┌──────┐
                    │                       │  │Failed│
                    │                       │  └──────┘
                    │                       │
                    ▼                       ▼
            ┌──────────────────┐  ┌──────────────────┐
            │ PasswordVerified │  │ WaitingForAccess │
            └──────────────────┘  └───┬──────────┬───┘
                                      │          │
                                  Allowed      Denied
                                      │          │
                                      │          ▼
                                      │       ┌──────┐
                                      │       │Failed│
                                      │       └──────┘
                                      │
                                      ▼
                                ┌──────────────┐
                                │AccessGranted │
                                └──────────────┘
                                      │
                                      ▼
                                 ✅ READY
                        (Send sessionReady)
```

### 8.2 Connection Lifecycle State Machine

```
              ┌─────────────────┐
              │  Not Connected  │
              └────────┬────────┘
                       │
                   JoinSession()
                       │
                       ▼
            ┌──────────────────┐
            │  Waiting For Peer│
            └────────┬─────────┘
                     │
             (Other peer joins)
                     │
                     ▼
            ┌──────────────────┐
            │   Handshake      │
            │   In Progress    │
            └────────┬─────────┘
                     │
              ┌──────┴──────┐
              │             │
          (Success)     (Failed)
              │             │
              ▼             ▼
        ┌──────────┐   ┌──────────┐
        │  Ready   │   │Disconnect│
        │ Streaming│   │  Failed  │
        └────┬─────┘   └──────────┘
             │
         (Network/User)
             │
             ▼
        ┌──────────┐
        │Disconnect│
        │ Normal   │
        └──────────┘
             │
             ▼
        ┌──────────────┐
        │Not Connected │
        └──────────────┘
```

---

## 9. Error Handling

### 9.1 Error Scenarios

| Scenario              | Detection                                | Handling                        | Recovery                      |
|-----------------------|------------------------------------------|---------------------------------|-------------------------------|
| **Wrong Password**    | `SubmitPasswordVerification(false)`      | Notify Controller, disconnect   | User must reconnect           |
| **Access Denied**     | `SubmitAccessResponse(false)`            | Notify Controller, disconnect   | User must request again       |
| **Peer Disconnects**  | `OnDisconnectedAsync()`                  | Notify other peer               | Session waits/ends            |
| **Both Disconnect**   | Both call `OnDisconnectedAsync()`        | Record in MongoDB, clean up     | New session creation          |
| **MongoDB Offline**   | `RecordConnectionAsync()` failure        | Log warning, continue in-memory | Session works without history |
| **Handshake Timeout** | Client doesn't respond (no timeout impl) | Server keeps waiting            | Requires manual disconnect    |
### 9.2 Error Handling Architecture

```
┌──────────────────────────────────────┐
│  ERROR DETECTION & HANDLING          │
├──────────────────────────────────────┤
│                                      │
│  Try-Catch Blocks:                   │
│  ├─ JoinSession()                    │
│  ├─ SubmitPasswordInfo()             │
│  ├─ SubmitPassword()                 │
│  ├─ SubmitPasswordVerification()     │
│  ├─ SubmitAccessResponse()           │
│  ├─ SendSdpOffer/Answer()            │
│  ├─ SendIceCandidate()               │
│  ├─ SendMessage()                    │
│  └─ OnDisconnectedAsync()            │
│                                      │
│  Logging:                            │
│  ├─ Info: Normal flow                │
│  ├─ Warning: Unexpected state        │
│  └─ Error: Exceptions                │
│                                      │
│  Notifications:                      │
│  ├─ PasswordIncorrect()              │
│  ├─ AccessDenied()                   │
│  ├─ SessionEnded()                   │
│  └─ PeerDisconnected()               │
│                                      │
└──────────────────────────────────────┘
```

---

## 10. Monitoring & Logging

### 10.1 Log Levels & Emojis

| Level           | Emoji                              | Example                   | Use Case               |
|-----------------|------------------------------------|---------------------------|------------------------|
| **Information** | ✅, 🔄, 📤, 📥, 📋, 🎉, 🔐, 🔌 | "✅ MongoDB connected"    | Normal operations      |
| **Warning**     | ⚠️, 🔴, ❌                       | "⚠️ Agent already exists" | Unexpected but handled |
| **Error**       | ❌, 🛑                            | "❌ Error in JoinSession" | Exceptions, failures   |
| **Debug**       | 📦, 🧊                            | "📦 Relaying data"        | Low-level details      |

### 10.2 Key Logging Points

```
┌──────────────────────────────────────────┐
│  LOGGING CHECKPOINTS                     │
├──────────────────────────────────────────┤
│                                          │
│  Connection:                             │
│  ├─ JoinSession start/end                │
│  ├─ Peer replacement (role change)       │
│  ├─ OnDisconnected events                │
│  └─ ConnectionId tracking                │
│                                          │
│  Handshake:                              │
│  ├─ State transitions                    │
│  ├─ Password info received               │
│  ├─ Password verification result         │
│  ├─ Access response                      │
│  └─ Handshake completion                 │
│                                          │
│  WebRTC Signaling:                       │
│  ├─ SDP Offer sent/received              │
│  ├─ SDP Answer sent/received             │
│  ├─ ICE Candidate exchange               │
│  └─ sessionReady event                   │
│                                          │
│  Database:                               │
│  ├─ MongoDB connection status            │
│  ├─ Connection record creation           │
│  ├─ Disconnection record update          │
│  └─ Index creation                       │
│                                          │
│  Errors:                                 │
│  ├─ Exception details                    │
│  ├─ Invalid state transitions            │
│  ├─ Missing context                      │
│  └─ Database failures                    │
│                                          │
└──────────────────────────────────────────┘
```

### 10.3 MongoDbHealthCheck

| Method                   | Purpose              | Called From              |
|--------------------------|----------------------|--------------------------|
| `IsConnectedAsync()`     | Ping MongoDB         | Program.cs startup       |
| `IsConnected` (property) | Quick status check   | MongoDbService methods   |


## Summary Tables

### Complete Method Reference

| Class                  | Method                         | Parameters                                   | Returns                     | Purpose         |
|------------------------|--------------------------------|----------------------------------------------|-----------------------------|-----------------|
| **SessionHub**         | `JoinSession()`                | sessionId, role, callerName, callerSessionId | Task                        | Peer connection |
| **SessionHub**         | `SubmitPasswordInfo()`         | sessionId, hasPassword                       | Task                        | Auth step 1     |
| **SessionHub**         | `SubmitPassword()`             | sessionId, password                          | Task                        | Auth step 2     |
| **SessionHub**         | `SubmitPasswordVerification()` | sessionId, isCorrect                         | Task                        | Auth step 3     |
| **SessionHub**         | `SubmitAccessResponse()`       | sessionId, allowed                           | Task                        | Auth step 4     |
| **SessionHub**         | `SendSdpOffer()`               | sessionId, sdpJson                           | Task                        | WebRTC setup    |
| **SessionHub**         | `SendSdpAnswer()`              | sessionId, sdpJson                           | Task                        | WebRTC setup    |
| **SessionHub**         | `SendIceCandidate()`           | sessionId, candidateJson                     | Task                        | WebRTC setup    |
| **SessionHub**         | `SendMessage()`                | sessionId, messageJson                       | Task                        | Generic relay   |
| **SessionHub**         | `RelayData()`                  | sessionId, data                              | Task                        | Data relay      |
| **SessionHub**         | `OnDisconnectedAsync()`        | exception                                    | Task                        | Cleanup         |
| **SessionHub**         | `GetSessionStats()`            | sessionId                                    | SessionStatsExtended        | Monitoring      |
| **SessionHub**         | `GetConnectionHistory()`       | sessionId                                    | List<ConnectionLogDocument> | Audit           |
| **MongoDbService**     | `RecordConnectionAsync()`      | sessionId, connIds, ips                      | ConnectionLogDocument       | DB record       |
| **MongoDbService**     | `RecordDisconnectionAsync()`   | sessionId, logId, reason                     | Task                        | DB update       |
| **MongoDbService**     | `GetSessionAsync()`            | sessionId                                    | SessionDocument             | Query           |
| **MongoDbService**     | `GetConnectionLogsAsync()`     | sessionId                                    | List<...>                   | Audit log       |
| **MongoDbHealthCheck** | `IsConnectedAsync()`           | N/A                                          | bool                        | Health check    |

## Architecture Quick Reference

### Configuration Summary

```
SERVER:
├─ Host: 0.0.0.0
├─ Port: 5000
├─ Protocol: HTTP/HTTPS (WebSocket)
└─ SignalR Endpoint: /hub

DATABASE:
├─ Type: MongoDB
├─ Default: mongodb://127.0.0.1:27017
├─ Database: PoshtibanoDesk
└─ Collections: sessions, connection_logs

SIGNALR:
├─ Message Size: 1 MB
├─ Timeout: 60 seconds
├─ Keep-Alive: 15 seconds
├─ Protocols: NewtonsoftJson + MessagePack
└─ CORS: AllowAll (Dev only)
```

### Key Design Patterns

| Pattern          | Implementation       | Benefit                           |
|------------------|----------------------|-----------------------------------|
| **Concurrency**  | ConcurrentDictionary | Thread-safe collections           |
| **Locking**      | `lock()` statement   | Thread-safe state changes         |
| **Event-Driven** | SignalR hub methods  | Asynchronous messaging            |
| **Repository**   | MongoDbService       | Data abstraction                  |
| **Health Check** | MongoDbHealthCheck   | Graceful degradation              |
| **Stateless Hub**| Static collections   | Scalability (with considerations) |

## Appendix: Glossary

| Term             | Definition                                              |
|------------------|---------------------------------------------------------|
| **Hub**          | SignalR server endpoint for bidirectional communication |
| **SessionId**    | Unique identifier for a remote desktop session          |
| **Handshake**    | Authentication and permission negotiation process       |
| **Relay**        | Forward messages between peers through Hub              |
| **Signaling**    | Exchange of connection metadata (SDP, ICE)              |
| **Peer**         | Agent or Controller in a session                        |
| **ConnectionId** | WebSocket connection identifier from SignalR            |
| **ICE**          | Interactive Connectivity Establishment (WebRTC)         |
| **SDP**          | Session Description Protocol (WebRTC offer/answer)      |
