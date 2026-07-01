# Poshtibano Desk Developer Documentation

> **Language:** C# (.NET / WinForms)  
> **Communication Architecture:** WebRTC (SIPSorcery) + SignalR

## Table of Contents

1. [Overall System Architecture](#1-overall-system-architecture)
2. [Project Layering](#2-project-layering)
3. [Connection Flow and Handshake](#3-connection-flow-and-handshake)
4. [Key Classes](#4-key-classes)
   - 4.1 [HubConnectionService](#41-hubconnectionservice)
   - 4.2 [PeerConnectionManager](#42-peerconnectionmanager)
   - 4.3 [PeerConnectionManager.AudioVideo](#43-peerconnectionmanageraudiovideo)
   - 4.4 [SessionCoordinator](#44-sessioncoordinator)
   - 4.5 [SessionCoordinator.AudioVideo](#45-sessioncoordinatoraudiovideo)
   - 4.6 [SessionCoordinator.Clipboard](#46-sessioncoordinatorclipboard)
   - 4.7 [PacketHandler](#47-packethandler)
   - 4.8 [ChatManager](#48-chatmanager)
   - 4.9 [FileTransferManager](#49-filetransfermanager)
   - 4.10 [ClipboardSharingManager](#410-clipboardsharingmanager)
   - 4.11 [ClipboardMonitor](#411-clipboardmonitor)
   - 4.12 [ProcessNameManager](#412-processnamemanager)
   - 4.13 [MainForm and Subcomponents](#413-mainform-and-subcomponents)
5. [Data Flow Diagrams](#5-data-flow-diagrams)
6. [Relationships Between Classes](#6-relationships-between-classes)
7. [Development Best Practices](#7-development-best-practices)

## 1. Overall System Architecture

Poshtibano Desk is a remote desktop tool that uses **WebRTC** for Peer-to-Peer communication and **SignalR** for initial signaling. The system has two main roles:

| Role          | Description                                                   |
|---------------|---------------------------------------------------------------|
| **Agent**     | The machine that grants remote control permission (host side) |
| **Controller**| The machine that remotely controls (controller side)          |

```
┌─────────────────────────────────────────────────────────────────┐
│                      Poshtibano Architecture                    │
│                                                                 │
│  ┌──────────┐    SignalR Hub    ┌──────────┐                    │
│  │  Agent   │◄─────────────────►│Controller│                    │
│  │  (Host)  │   (Signaling)     │ (Remote) │                    │
│  └────┬─────┘                   └────┬─────┘                    │
│       │                              │                          │
│       │     WebRTC P2P Connection    │                          │
│       │◄────────────────────────────►│                          │
│       │   ┌─ frames (id=1)           │                          │
│       │   ├─ control (id=2)          │                          │
│       │   ├─ bulk (id=3)             │                          │
│       │   ├─ audio (id=4)            │                          │
│       │   └─ webcam (id=5)           │                          │
│       │                              │                          │
└─────────────────────────────────────────────────────────────────┘
```

## 2. Project Layering

```
Poshtibano/
├── Poshtibano.Common/                        ← shared models (Packet, Enums)
├── Poshtibano.Hub/                           ← SignalR server (SessionHub)
├── Poshtibano.Desk.Shared/                   ← shared Services layer
│   ├── PeerConnectionManager.cs              ← P2P core (WebRTC)
│   ├── PeerConnectionManager.AudioVideo.cs   ← audio/video channels
│   └── Services/                             
│       ├── Connection/                        plen
│       │   └── HubConnectionService.cs       ← SignalR signaling
│       ├── Networking/                       
│       │   └── PacketHandler.cs              ← packet routing
│       ├── ChatManager.cs                    ← chat logic
│       ├── FileTransferManager.cs            ← file transfer
│       └── ClipboardSharingManager.cs        ← clipboard sharing
└── Poshtibano.Desk/              ← UI layer (WinForms)
    ├── MainForm.cs                           ← main form
    ├── MainForm.SessionEvents.cs             ← session events
    ├── MainForm.Controller.cs                ← controller logic
    ├── MainForm.Chat.cs                      ← chat UI
    ├── MainForm.Clipboard.cs                 ← clipboard UI
    ├── MainForm.AudioVideo.cs                ← audio/webcam UI
    ├── MainForm.UIManager.cs                 ← appearance management
    ├── MainForm.Monitor.cs                   ← monitor management
    └── Services/
        ├── SessionCoordinator.cs             ← overall coordinator
        ├── SessionCoordinator.AudioVideo.cs  ← audio/webcam coordination
        ├── SessionCoordinator.Clipboard.cs   ← clipboard coordination
        ├── ClipboardMonitor.cs               ← clipboard monitoring
        └── ProcessNameManager.cs             ← process under-mouse detection
```

| Layer / Component                      | Description / Responsibility                                            |
|----------------------------------------|-------------------------------------------------------------------------|
| **UI (MainForm)**                      | Display user interface, receive input, show remote frame/viewport       |
| **Coordination (SessionCoordinator)**  | Coordinate services, manage session lifecycle                           |
| **Services**                           | Independent logic modules (chat, file transfer, clipboard, audio/video) |
| **Connection**                         | Manage SignalR connection and WebRTC                                    |
| **Common**                             | Shared models and data structures                                       |

## 3. Connection Flow and Handshake

### 3.1 Full connection sequence

```
Agent                       Hub (SignalR)                  Controller
  │                           │                              │
  │──── JoinSession ─────────►│                              │
  │                           │◄──── JoinSession ────────────│
  │                           │                              │
  │                    ┌──────┴──────┐                       │
  │                    │ Both joined │                       │
  │                    │ Start HSK   │                       │
  │                    └──────┬──────┘                       │
  │                           │                              │
  │◄── RequestPasswordInfo ───│                              │
  │─── SubmitPasswordInfo ───►│                              │
  │                           │                              │
  │   [if hasPassword=true]   │                              │
  │                           │── RequestPassword ──────────►│
  │                           │◄── SubmitPassword ───────────│
  │◄── VerifyPassword ────────│                              │
  │─── PasswordVerification ─►│                              │
  │                           │── PasswordCorrect ──────────►│
  │                           │                              │
  │   [if hasPassword=false OR verified]                     │
  │                           │                              │
  │◄── RequestAccessPerm ─────│                              │
  │─── AccessResponse ───────►│                              │
  │                           │                              │
  │   [if access=granted]     │                              │
  │                           │                              │
  │─── SendSdpOffer ─────────►│── ReceiveSdpOffer ──────────►│
  │                           │                              │
  │◄── ReceiveSdpAnswer ──────│◄── SendSdpAnswerAsync────────│
  │                           │                              │
  │                           │                              │
  │◄──► ICE Candidates ◄─────►│◄──► ICE Candidates ◄────────►│
  │                           │                              │
  │═══════════ P2P Connected (5 Data Channels) ══════════════│
```

### 3.2 WebRTC Handshake flow

```
Controller (Offerer)                          Agent (Answerer)
       │                                            │
       │─── CreatePeerConnectionInternal() ────────►│
       │    ├── RTCPeerConnection(config)           │
       │    ├── CreateDataChannels() [5 channels]   │
       │    └── AttachPeerConnectionHandlers()      │
       │                                            │
       │─── CreateAndSendOfferAsync() ─────────────►│
       │    ├── createOffer()                       │
       │    ├── setLocalDescription(offer)          │
       │    └── SendSdpOfferAsync(offer)            │
       │                                            │
       │                  HandleSdpOffer(offer) ────│
       │                  ├── CreatePeerConnection  │
       │                  ├── setRemoteDescription  │
       │                  ├── createAnswer()        │
       │                  ├── setLocalDescription   │
       │                  ├── FlushPendingCandidates│
       │                  └── SendSdpAnswerAsync    │
       │                                            │
       │◄── HandleSdpAnswer(answer) ────────────────│
       │    ├── setRemoteDescription                │
       │    └── FlushPendingCandidates              │
       │                                            │
       │◄═══════ ICE Candidates Exchange ══════════►│
       │                                            │
       │════════ Data Channels OPEN ════════════════│
```

## 4. Key Classes

### 4.1 HubConnectionService

> **File:** `Poshtibano.Desk.Shared/Services/Connection/HubConnectionService.cs`  
> **Role:** Manages the SignalR Hub connection with the server, initial signaling, and authentication handshake

#### Class fields


| Field                  | Type                        | Description                              |
|------------------------|-----------------------------|------------------------------------------|
| `_hubConnection`       | `HubConnection`             | SignalR connection                       |
| `_signalingUrl`        | `string`                    | Hub server address                       |
| `_stateManager`        | `ConnectionStateManager`    | Connection state management              |
| `_role`                | `ClientRole`                | Client role (Agent / Controller)         |
| `_reconnectCts`        | `CancellationTokenSource`   | Reconnect cancellation token source      |
| `_reconnectAttempts`   | `int`                       | Reconnect attempt counter                |
| `MaxReconnectAttempts` | `const int = 10`            | Maximum reconnect attempts               |

#### Events


| Event                        | Signature              | Description                              |
|------------------------------|------------------------|------------------------------------------|
| `OnRequestPasswordInfo`      | `Action`               | Server requests password information     |
| `OnRequestPassword`          | `Action<string>`       | Server requests password                 |
| `OnPasswordIncorrect`        | `Action`               | Password is incorrect                    |
| `OnPasswordCorrect`          | `Action`               | Password is correct                      |
| `OnRequestAccessPermission`  | `Action<string>`       | Request access permission                |
| `OnAccessDenied`             | `Action`               | Access denied                            |
| `OnSessionEnded`             | `Action<string>`       | Session ended                            |
| `OnSdpOfferReceived`         | `Action<string>`       | SDP Offer received                       |
| `OnSdpAnswerReceived`        | `Action<string>`       | SDP Answer received                      |
| `OnIceCandidateReceived`     | `Action<string>`       | ICE Candidate received                   |
| `OnPeerDisconnected`         | `Action`               | Peer disconnected                        |
| `OnVerifyPassword`           | `Action<string>`       | Verify password                          |
| `OnChangeRoleRequest`        | `Action<ClientRole>`   | Request role change                      |

#### Important methods

```csharp
// Connect to the Hub and join the session
public async Task ConnectAsync(string sessionId, string caller)
// 1. Create HubConnection with MessagePack + Newtonsoft
// 2. RegisterHandlers() to register handlers
// 3. StartAsync() → JoinSession(sessionId, role, caller)
// 4. On error and role == Agent → RetryConnectionAsync

// Send password info
public async Task SendPasswordInfoAsync(string sessionId, bool hasPassword)

// Send password
public async Task SendPasswordAsync(string sessionId, string password)

// Send password verification result
public async Task SendPasswordVerificationAsync(string sessionId, bool isCorrect)

// Send access response
public async Task SendAccessResponseAsync(string sessionId, bool allowed)

// Send SDP Offer/Answer and ICE Candidate
public async Task SendSdpOfferAsync(string sessionId, string offer)
public async Task SendSdpAnswerAsync(string sessionId, string answer)
public async Task SendIceCandidateAsync(string sessionId, string candidate)

// Disconnect
public async Task DisconnectAsync()

// Dispose: cancel reconnect, remove handlers, free resources
public void Dispose()
```

#### Technical characteristics
- Uses **MessagePack** and **Newtonsoft JSON** simultaneously for serialization
- Custom **RetryPolicy** for `WithAutomaticReconnect`
- In **Agent** role, automatically reconnects on disconnection
- HTTP proxy is disabled (`UseProxy = false`)


### 4.2 PeerConnectionManager

> **File:** `Poshtibano.Desk.Shared/PeerConnectionManager.cs`  
> **Role:** Core P2P communication — manages RTCPeerConnection, creates data channels and sends/receives data

#### Class fields

| Field               | Type                        | Description                              |
|---------------------|-----------------------------|------------------------------------------|
| `_role`             | `ClientRole`                | Current role (can be changed)            |
| `_sessionId`        | `string`                    | Session identifier                       |
| `_stateManager`     | `ConnectionStateManager`    | Connection/state management              |
| `_hubService`       | `HubConnectionService`      | Signaling service (SignalR)              |
| `_peerConnection`   | `RTCPeerConnection`         | WebRTC peer connection                   |
| `_frameChannel`     | `RTCDataChannel`            | Frame/video channel (id=1)               |
| `_controlChannel`   | `RTCDataChannel`            | Control / chat channel (id=2)            |
| `_bulkChannel`      | `RTCDataChannel`            | File transfer channel (id=3)             |
| `_audioChannel`     | `RTCDataChannel`            | Audio channel (id=4)                     |
| `_webcamChannel`    | `RTCDataChannel`            | Webcam / video channel (id=5)            |
| `_pcLock`           | `object`                    | Thread-safety lock for peer connection   |
| `_bulkSendLock`     | `SemaphoreSlim(1,1)`        | Limiter for concurrent file sending      |

#### 5 data channel architecture (Negotiated)

| ID | Name       | Type       | Settings                                   | Usage / Application         |
|----|------------|------------|--------------------------------------------|-----------------------------|
| 1  | `frames`   | `unordered`| `maxPacketLifeTime=500`                    | Display/screen frames       |
| 2  | `control`  | `ordered`  | `maxPacketLifeTime=5000, maxRetransmits=5` | Input events + chat         |
| 3  | `bulk`     | `ordered`  | `maxRetransmits=15`                        | Bulk file transfer          |
| 4  | `audio`    | `unordered`| `maxPacketLifeTime=500, maxRetransmits=0`  | Microphone audio            |
| 5  | `webcam`   | `unordered`| `maxPacketLifeTime=500, maxRetransmits=0`  | Webcam image/video          |

> **Important:** All channels are created with `negotiated=true` to guarantee consistent IDs on both sides.

#### Events

| Event                                       | Signature         | Description                   |
|---------------------------------------------|-------------------|-------------------------------|
| `OnFrameDataReceived`                       | `Action<byte[]>`  | Received image frame data     |
| `OnEventDataReceived`                       | `Action<byte[]>`  | Received input event data     |
| `OnChatDataReceived`                        | `Action<byte[]>`  | Received chat message data    |
| `OnFileDataReceived`                        | `Action<byte[]>`  | Received file chunk data      |
| `OnSessionReady`                            | `Action`          | Session is ready              |
| `OnRequestPasswordInfo`                     | `Action`          | Request password information  |
| `OnRequestPassword`                         | `Action<string>`  | Request password              |
| `OnPasswordIncorrect` / `OnPasswordCorrect` | `Action`          | Password verification result  |
| `OnRequestAccessPermission`                 | `Action<string>`  | Request access permission     |
| `OnAccessDenied`                            | `Action`          | Access permission denied      |
| `OnSessionEnded`                            | `Action<string>`  | Session ended                 |

#### Important methods

```csharp
// Constructor: create stateManager and hubService
public PeerConnectionManager(ClientRole role, string sessionId, string signalingUrl)

// Connect to Hub (signaling)
public async Task ConnectAsync(string caller)

// Create PeerConnection (ICE servers, data channels, handlers)
public async Task CreatePeerConnectionAsync()
private void CreatePeerConnectionInternal()

// Create and send SDP Offer
public async Task CreateAndSendOfferAsync()

// Handle SDP Offer/Answer/ICE Candidate
private async void HandleSdpOffer(string offerJson)
private void HandleSdpAnswer(string answerJson)
private void HandleIceCandidate(string candidateJson)
private void FlushPendingCandidates()

// Send data on various channels
public void SendFrame(byte[] data)     // frames channel
public void SendEvent(byte[] data)     // control channel (type=0x01)
public void SendChat(byte[] data)      // control channel (type=0x02)
public void SendFile(byte[] data)      // bulk channel
public void SendFileCancel(string transferId)

// Disconnect and recreate connection
public async Task DisconnectAsync()
public async Task DisconnectPeerOnlyAsync()
public async Task RecreatePeerConnectionAsync()
private async Task ResetPeerConnectionAsync()
private void ClosePeerConnection()

// Change role
public void ChangeRole(ClientRole role)

// Dispose: disconnect, free all resources
public void Dispose()
```

#### Control channel protocol

The `control` channel (id=2) uses the first byte as message type:

```
┌─────────────────────────────────┬─────────┐
| Payload (remaining bytes)       | byte[0] |
├─────────────────────────────────┼─────────┤
| Event (input event)             | 0x01    |
| Chat (chat message / clipboard) | 0x02    |
└─────────────────────────────────┴─────────┘
```

#### ICE Servers

The `GetIceServers()` method returns the list of STUN/TURN servers. Settings:
- `iceTransportPolicy = all`
- `bundlePolicy = max_bundle`
- `rtcpMuxPolicy = require`

### 4.3 PeerConnectionManager.AudioVideo

> **File:** `Poshtibano.Desk.Shared/PeerConnectionManager.AudioVideo.cs`  
> **Role:** Manages audio and webcam channels (partial class)

#### Important methods

```csharp
// Setup audio channel (id=4)
private void SetupAudioChannel()

// Setup webcam channel (id=5)
private void SetupWebcamChannel()

// Send audio data
public void SendAudioData(ClientRole role, byte[] audioData,
    int sampleRate, int channels, int bitsPerSample,
    long timestampTicks, uint sequenceNumber)

// Send webcam data
public void SendWebcamData(ClientRole role, byte[] videoData, byte[] audioData,
    int width, int height, int audioSampleRate, int audioChannels,
    long timestampTicks, uint sequenceNumber)

// Check channel enabled/disabled
// methods related to status and enable/disable
```

#### Channel settings

| ID | Name      | Type       | Settings                                   | Reason                                          |
|----|-----------|------------|--------------------------------------------|-------------------------------------------------|
| 4  | `audio`   | `unordered`| `maxPacketLifeTime=2000, maxRetransmits=0` | Real-time; latency is more important than loss  |
| 5  | `webcam`  | `ordered`  | `maxPacketLifeTime=2000, maxRetransmits=5` | Real-time; old frames have no value             |

### 4.4 SessionCoordinator

> **File:** `Poshtibano.Desk/Services/SessionCoordinator.cs`  
> **Role:** Central coordinator of all services — mediator between UI and network layer

#### Class fields

| Field                     | Type                        | Description                                           |
|---------------------------|-----------------------------|-------------------------------------------------------|
| `_role`                   | `ClientRole`                | Role (can be changed)                                 |
| `_sessionId`              | `string`                    | Session identifier                                    |
| `_signalingUrl`           | `string`                    | Hub server address                                    |
| `_uiContext`              | `SynchronizationContext`    | UI context for thread-safety                          |
| `_peerManager`            | `PeerConnectionManager`     | Core P2P / WebRTC management                          |
| `_packetHandler`          | `PacketHandler`             | Packet routing / dispatching                          |
| `_chatManager`            | `ChatManager`               | Chat management                                       |
| `_fileTransferManager`    | `FileTransferManager`       | File transfer management                              |
| `_captureService`         | `ScreenCaptureService`      | Screen capture (Agent only)                           |
| `_localInputHandler`      | `LocalInputHandler`         | Local input handling (Agent only)                     |
| `_renderService`          | `FrameRenderService`        | Frame rendering (Controller only)                     |
| `_remoteInputService`     | `RemoteInputService`        | Remote input generation / injection (Controller only) |

#### Events

| Event                                       | Signature                        | Description                              |
|---------------------------------------------|----------------------------------|------------------------------------------|
| `OnHubStateChanged`                         | `Action<ConnectionStatus>`       | Hub connection status changed            |
| `OnPeerStateChanged`                        | `Action<ConnectionStatus>`       | P2P / WebRTC connection status changed   |
| `OnSessionReady`                            | `Action`                         | Session is ready                         |
| `OnChatMessage`                             | `Action<ChatMessage>`            | New chat message received                |
| `OnChatEvent`                               | `Action<ChatEvent>`              | Chat event (like, edit, delete)          |
| `OnFileProgress`                            | `Action<FileTransferProgress>`   | File transfer progress update            |
| `OnFileReceived`                            | `Action<string, string>`         | File successfully received               |
| `OnTransferCancelled`                       | `Action<string, string>`         | Transfer cancelled                       |
| `OnError`                                   | `Action<string>`                 | Error occurred                           |
| `OnRequestPasswordInfo`                     | `Action`                         | Request password information             |
| `OnRequestPassword`                         | `Action<string>`                 | Request password                         |
| `OnPasswordIncorrect` / `OnPasswordCorrect` | `Action`                         | Password verification result             |
| `OnRequestAccessPermission`                 | `Action<string>`                 | Request access permission                |
| `OnAccessDenied`                            | `Action`                         | Access permission denied                 |
| `OnSessionEnded`                            | `Action<string>`                 | Session ended                            |

#### Important methods

```csharp
// Constructor
public SessionCoordinator(ClientRole role, string sessionId, string signalingUrl)

// Initialize — create all services and attach events
public async Task InitializeAsync(string caller)
// ← PeerConnectionManager is created
// ← PacketHandler, ChatManager, FileTransferManager are created
// ← Audio/Video, Clipboard services are initialized
// ← ConnectAsync is called

// Initialize services for Agent and Controller
public void Initialize(MonitorInfo monitor, Control renderTarget, Form parentForm)
private void InitializeAgentServices(MonitorInfo monitor)
private void InitializeControllerServices(Control renderTarget, Form parentForm)

// Send a Packet (supports fragmentation)
public void SendPacket(Packet packet)
private void SendPacketDirect(Packet packet) // route to appropriate channel

// Send chat message
public async Task SendChatMessageAsync(string text)

// Send chat event (like, edit, delete)
public void SendChatEvent(Guid messageId, ChatEventType eventType, object data)

// Send files
// Uses FileTransferManager.SendPathsAsync

// Cleanup
private async Task CleanupPreviousSession()
private void DetachAllEvents()
public async Task DisconnectAsync()
public void Dispose()

// Change role
public void ChangeRole(ClientRole role)
```

#### Packet routing flow

```
PeerConnectionManager
     │
     ├── OnFrameDataReceived ──► PacketHandler.HandleRawData(Frame)
     │                               └── OnFramePacket → RenderService
     │
     ├── OnEventDataReceived ──► PacketHandler.HandleRawData(Event)
     │                               └── OnEventPacket → LocalInputHandler
     │
     ├── OnChatDataReceived ───► PacketHandler.HandleRawData(Chat)
     │                               ├── OnChatPacket → ChatManager
     │                               └── OnChatEventPacket → ChatManager
     │
     └── OnFileDataReceived ───► FileTransferManager (direct)
```

### 4.5 SessionCoordinator.AudioVideo

> **File:** `Poshtibano.Desk/Services/SessionCoordinator.AudioVideo.cs`  
> **Role:** Manages audio and webcam services (partial class)

#### Key fields

| Field                     | Type                        | Description                                 |
|---------------------------|-----------------------------|---------------------------------------------|
| `_audioCaptureService`    | `AudioCaptureService`       | Microphone recording / capture              |
| `_audioPlaybackService`   | `AudioPlaybackService`      | Audio playback / speaker output             |
| `_videoCaptureService`    | `VideoCaptureService`       | Webcam video capture                        |
| `_avStateManager`         | `AudioVideoStateManager`    | Audio/Video state management                |
| `_webcamViewerForm`       | `WebcamViewerForm`          | Webcam preview/display form                 |
| `HasMicrophone`           | `bool`                      | Indicates whether a microphone is available |
| `HasWebcam`               | `bool`                      | Indicates whether a webcam is available     |

#### Audio/Webcam permission request flow

```
Requester                                         Responder
     │                                               │
     │── RequestToSendMyAudioAsync() ───────────────►│
     │   (State: Requesting → WaitingPermission)     │
     │                                               │
     │                  HandleTheyWantToSendAudio() ─│
     │                  └── OnTheyWantToSendAudio    │
     │                      (show permission dialog) │
     │                                               │
     │◄── RespondToTheirAudioSendRequestAsync() ─────│
     │                                               │
     │── HandleMyAudioSendResponse(allowed) ─────────│
     │   ├── if allowed: StartAudioCapture()         │
     │   └── if denied:  State = Stopped             │
     │                                               │
```

#### Important methods

```csharp
// Initialize: detect hardware (microphone/webcam)
private void InitializeAudioVideoServices()

// Requests
public async Task RequestToSendMyAudioAsync(string name)
public async Task RequestToSendMyWebcamAsync(string name)
public async Task RequestTheirAudioAsync(string name)
public async Task RequestTheirWebcamAsync(string name)

// Responses
public async Task RespondToTheirAudioSendRequestAsync(bool allowed)
public async Task RespondToTheirWebcamSendRequestAsync(bool allowed, string name)
public async Task RespondToTheirReceiveMyAudioRequestAsync(bool allowed)
public async Task RespondToTheirReceiveMyWebcamRequestAsync(string name, bool allowed)

// Start/stop
private void StartAudioCapture()       // 16000Hz, 1ch, 16bit
private void StartAudioPlayback()      // JitterBuffer 100ms
private void StartWebcamCapture()
public async Task StopSendingMyAudioAsync()
public async Task StopSendingMyWebcamAsync()
public async Task StopReceivingTheirAudioAsync()
public async Task StopReceivingTheirWebcamAsync()

// Incoming data handlers
private void HandleAudioDataReceived(...)
private void HandleWebcamDataReceived(...)

// Cleanup
private async Task CleanupAudioVideoServicesAsync()
```

### 4.6 SessionCoordinator.Clipboard

> **File:** `Poshtibano.Desk/Services/SessionCoordinator.Clipboard.cs`  
> **Role:** Integration of clipboard sharing (partial class)

#### Architecture

```
MainForm ←→ SessionCoordinator ←→ ClipboardSharingManager ←→ PeerConnectionManager
                   ↕
           ClipboardMonitor
```

#### Fields and events

```csharp
private ClipboardSharingManager _clipboardSharingManager;
private ClipboardMonitor _clipboardMonitor;

// Events (to MainForm)
public event Action<string> OnClipboardRemoteTextReceived;
public event Action<List<string>> OnClipboardRemoteFilesReceived;
public event Action<string> OnClipboardStatusChanged;
public event Action<string> OnClipboardError;
public event Action<string, List<string>, long> OnClipboardFileOfferReceived;

// Enabled/disabled
public bool IsClipboardSharingEnabled { get; set; }
```

#### Important methods

```csharp
// Initialize: create ClipboardSharingManager + ClipboardMonitor
private void InitializeClipboardServices()

// Attach and detach events
private void AttachClipboardEvents()
private void DetachClipboardEvents()
private void CleanupClipboardServices()

// Accept/reject file offer (called from MainForm)
public void AcceptClipboardFileOffer(string transferId)
public void RejectClipboardFileOffer(string transferId)

// Local clipboard change handler
private void OnLocalClipboardChangedHandler()
```

### 4.7 PacketHandler

> **File:** `Poshtibano.Desk.Shared/Services/Networking/PacketHandler.cs`  
> **Role:** Deserialize, reassemble fragments and route packets to appropriate service

#### Events


| Event                  | Packet Type              | Destination                  |
|------------------------|--------------------------|------------------------------|
| `OnFramePacket`        | `Frame`                  | `FrameRenderService`         |
| `OnEventPacket`        | `Event`                  | `LocalInputHandler`          |
| `OnChatPacket`         | `Chat`                   | `ChatManager`                |
| `OnChatEventPacket`    | `ChatEvent`              | `ChatManager`                |
| `OnFilePacket`         | `FileStart/Chunk/End`    | `FileTransferManager`        |
| `OnFileCancelPacket`   | `FileCancel`             | `FileTransferManager`        |
| `OnSettingsPacket`     | `Settings`               | Settings                     |
| `OnAudioPacket`        | `AudioData`              | `AudioPlaybackService`       |
| `OnWebcamPacket`       | `WebcamData`             | `WebcamViewer`               |

#### Important methods

```csharp
// Constructor: store UI Context + cleanup timer every 30 seconds
public PacketHandler()

// Entry point: deserialize and decide (complete or fragment)
public void HandleRawData(byte[] data, PacketType expectedType)

// Fragment management: buffer → reassemble → process
private void HandleFragmentedPacket(Packet packet)
private Packet ReassembleFragments(List<Packet> fragments, Packet template)

// Route to corresponding event
private void ProcessCompletePacket(Packet packet)

// Cleanup old fragments
private void CleanupOldFragments(object state)
```

#### Routing logic

```csharp
// ProcessCompletePacket: switch on packet.Type
PacketType.Frame      → OnFramePacket      (direct, without UI context)
PacketType.Event      → OnEventPacket      (with UI context)
PacketType.Chat       → OnChatPacket       (with UI context)
PacketType.ChatEvent  → OnChatEventPacket  (with UI context)
PacketType.FileStart  → OnFilePacket       (with UI context)
PacketType.FileChunk  → OnFilePacket       (with UI context)
PacketType.FileEnd    → OnFilePacket       (with UI context)
PacketType.FileCancel → OnFileCancelPacket (with UI context)
PacketType.Settings   → OnSettingsPacket   (with UI context)
PacketType.AudioData  → OnAudioPacket      (direct, low-latency)
PacketType.WebcamData → OnWebcamPacket     (direct, low-latency)
```

> **Note:** `Frame`, `AudioData` and `WebcamData` packets are processed directly (without `_uiContext.Post`) due to latency sensitivity.

### 4.8 ChatManager

> **File:** `Poshtibano.Desk.Shared/Services/ChatManager.cs`  
> **Role:** Manages sending and receiving chat messages and related events

#### Data models

```csharp
public enum ChatMessageMode { Local, Remote }

public class ChatMessage
{
    public Guid Id { get; set; }
    public string Text { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public ChatMessageMode Mode { get; set; }
    public ClientRole SenderRole { get; set; }
    public bool IsLiked { get; set; }
    public bool IsEdited { get; set; }
}
```

#### Events

| Event                  | Signature              | Description                                |
|------------------------|------------------------|--------------------------------------------|
| `OnNewMessage`         | `Action<ChatMessage>`  | New message (local or remote)              |
| `OnChatEventReceived`  | `Action<ChatEvent>`    | Chat event received (like / edit / delete) |

#### Important methods

```csharp
// Constructor
public ChatManager(PeerConnectionManager peerConnectionManager, ClientRole localRole)

// Process incoming packet (from PacketHandler)
public void HandlePacket(Packet packet)
// ← if ChatEvent: HandleChatEvent
// ← if Chat: build ChatMessage and invoke event

// Send message
public Task SendMessageAsync(string text)
// ← build Packet + serialize + peer.SendChat
// ← create local ChatMessage + invoke OnNewMessage

// Send chat event
public void SendChatEvent(Guid messageId, ChatEventType eventType, object data)
// ← supports: MessageLiked, MessageEdited, MessageDeleted
```

#### Send/receive flow

```
Sender Side:                         Receiver Side:
  SendMessageAsync("Hello")           HandlePacket(packet)
    │                                     │
    ├── Packet(Chat, UTF8)                ├── Deserialize
    ├── peer.SendChat(serialized)         ├── ChatMessage{Remote}
    ├── ChatMessage{Local}                └── OnNewMessage.Invoke
    └── OnNewMessage.Invoke
```

### 4.9 FileTransferManager

> **File:** `Poshtibano.Desk.Shared/Services/FileTransferManager.cs`  
> **Role:** Manages file transfer by chunking, progress tracking and support for cancelation

#### Settings

| Setting                                   | Default Value     | Description                              |
|-------------------------------------------|-------------------|------------------------------------------|
| `MaxFileChunkSize`                        | `8 * 1024` (8KB)  | Maximum size of each file chunk          |
| `DelayBetweenEachChunkForFileTransfer`    | `25ms`            | Delay between sending each chunk         |
| `DelayBetweenEachFileTransfer`            | `10ms`            | Delay between consecutive file transfers |
| `UsePrefixForFileName`                    | `false`           | Add TransferId prefix to the filename    |

#### Data models

```csharp
public enum FileTransferProgressState { None, Start, InTransfer, Complete }

public class FileTransferProgress
{
    public string TransferId { get; set; }
    public string FileName { get; set; }
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public bool IsReceiving { get; set; }
    public FileTransferProgressState State { get; set; }
    public double Percent { get; set; } // TotalBytes == 0 ? 0 : Transferred * 100.0 / Total
}
```

#### Events

| Event                   | Signature                           | Description                              |
|-------------------------|-------------------------------------|------------------------------------------|
| `OnProgressUpdated`     | `Action<FileTransferProgress>`      | Progress update                          |
| `OnFileReceived`        | `Action<string, string>`            | File received (transferId, path)         |
| `OnTransferStarted`     | `Action<string, string, long>`      | Transfer started                         |
| `OnTransferCompleted`   | `Action<string>`                    | Transfer completed                       |
| `OnTransferCancelled`   | `Action<string, string>`            | Transfer cancelled                       |

#### Important methods

```csharp
// Constructor: subscribe to OnFileDataReceived from PeerManager
public FileTransferManager(PeerConnectionManager peerConnectionManager, ClientRole localRole)

// Incoming data handler
private void Peer_OnFileDataReceived(byte[] data)
// ← switch based on PacketType: FileStart/FileChunk/FileEnd/FileCancel

// Start receiving: create a temporary FileStream
private void HandleFileStart(Packet packet)

// Handle chunk: write to FileStream (seek-based)
private void HandleFileChunk(Packet packet)

// Finish receiving: close file, rename, invoke event
private void HandleFileEnd(Packet packet)

// Cancel: cleanup temporary file
public void HandleFileCancel(Packet packet)

// Send files
public async Task SendPathsAsync(IEnumerable<string> paths,
    IProgress<FileTransferProgress> progress = null,
    CancellationToken cancellation = default)

// Cancel transfer
public string CancelTransfer(string transferId)
public void CancelAllTransfers()

// Cleanup temp files
public void CleanupTemp()
```

#### File transfer flow

```
Sender                                            Receiver
  │                                                  │
  │── FileStart (name, size, totalChunks) ──────────►│
  │                                HandleFileStart() │
  │                                ← create FileStream│
  │                                                  │
  │── FileChunk[0] (data) ──────────────────────────►│
  │── FileChunk[1] (data) ──────────────────────────►│
  │── ...                                            │
  │── FileChunk[N] (data) ──────────────────────────►│
  │                                HandleFileChunk() │
  │                                ← Write + Progress│
  │                                                  │
  │── FileEnd (totalSize) ──────────────────────────►│
  │                                HandleFileEnd()   │
  │                                ← Close + Rename  │
  │                                ← OnFileReceived  │
```

#### Note: chunk ordering

Chunks are written using `fs.Seek(packet.ChunkIndex * chunkSize, SeekOrigin.Begin)`, so even if out-of-order, they are written in the correct position.

### 4.10 ClipboardSharingManager

> **File:** `Poshtibano.Desk.Shared/Services/ClipboardSharingManager.cs`  
> **Role:** Clipboard sharing (text and files) between Agent and Controller with echo prevention

#### Data models

```csharp
public class ClipboardData
{
    public string Type { get; set; }          // "text", "files", "file_request"
    public string Text { get; set; }
    public List<string> FileNames { get; set; }
    public List<long> FileSizes { get; set; }
    public string TransferId { get; set; }
    public long TotalSize { get; set; }
    public long Timestamp { get; set; }
}

public class PendingClipboardFileOffer
{
    public string TransferId { get; set; }
    public List<string> FilePaths { get; set; }
    public List<string> FileNames { get; set; }
    public long TotalSize { get; set; }
}
```

#### Echo prevention mechanism

```
┌──────────────────────────────────────────────────────────────┐
│ Echo Prevention Strategy                                     │
│ ────────────────────────                                     │
│                                                              │
│ 1. _suppressCount                                            │
│    Counter for clipboard changes to ignore                   │
│    (SetFileDropList typically triggers 1–2 extra events)     │
│                                                              │
│ 2. _lastRemoteTextHash                                       │
│    Hash of the last remote text content                      │
│    → If current clipboard text matches remote hash → skip    │
│                                                              │
│ 3. _lastRemoteFilesHash                                      │
│    Hash of the last remote file list                         │
│    → If current file drop list matches remote hash → skip    │
│                                                              │
│ 4. _lastClipboardChangeTime                                  │
│    Debounce mechanism (300ms)                                │
│    → Clipboard changes within 300ms are ignored              │
│                                                              │
│ 5. SenderRole check                                          │
│   Ignore packets that originated from ourselves              │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

#### Events

| Event                       | Signature                            | Description                                                   |
|-----------------------------|--------------------------------------|---------------------------------------------------------------|
| `OnRemoteTextReceived`      | `Action<string>`                     | Remote text received                                          |
| `OnRemoteFilesReceived`     | `Action<List<string>>`               | Remote files received                                         |
| `OnRemoteFileOfferReceived` | `Action<string, List<string>, long>` | Remote file offer received (transferId, fileNames, totalSize) |
| `OnClipboardError`          | `Action<string>`                     | Clipboard error occurred                                      |
| `OnClipboardStatusChanged`  | `Action<string>`                     | Clipboard status changed                                      |

#### Important methods

```csharp
// Constructor: subscribe to OnChatDataReceived and OnFileReceived
public ClipboardSharingManager(
    PeerConnectionManager peerConnectionManager,
    FileTransferManager fileTransferManager,
    ClientRole localRole)

// === Sending (Copy Side) ===

// Handle local clipboard change
public void HandleLocalClipboardChange()
// ← check suppress, debounce, echo
// ← if text: SendClipboardText
// ← if files: SendClipboardFileOffer

// Send clipboard text
private void SendClipboardText(string text)

// Send file offer (metadata only, not content)
private void SendClipboardFileOffer(List<string> filePaths)

// Handle remote's file request (after Accept)
private async void HandleFileRequestFromRemote(string requestedTransferId)

// === Receiving ===

// Process incoming clipboard packets
private void OnChatDataReceivedForClipboard(byte[] data)
// ← ClipboardText → HandleRemoteClipboardText
// ← ClipboardFiles → HandleRemoteClipboardFileOffer
// ← ClipboardFileRequest → HandleRemoteClipboardFileRequest

// Accept/reject offered files
public void AcceptFileOffer(string transferId)
public void RejectFileOffer(string transferId)

// === Utilities ===
public void SuppressNextChange()           // Suppress next change
public List<string> GetReceivedFiles()     // List of received files
public string GetReceivedText()            // Received text
```

#### Clipboard file sharing flow

```
Sender (Copy)                                Receiver (Paste)
     │                                               │
     │ User copies file → HandleLocalClipboardChange │
     │                                               │
     │── ClipboardFiles (metadata only) ────────────►│
     │   {fileNames, sizes, transferId}              │
     │                                               │
     │         OnRemoteFileOfferReceived(offer)──────│
     │         └── show Accept/Reject dialog         │
     │                                               │
     │   [User accepts]                              │
     │                                               │
     │◄── ClipboardFileRequest(transferId) ──────────│
     │                                               │
     │── HandleFileRequestFromRemote ────────────────│
     │   └── FileTransferManager.SendPathsAsync      │
     │                                               │
     │── FileStart/Chunks/End ──────────────────────►│
     │                                               │
     │              OnClipboardFileReceived() ───────│
     │              └── SetReceivedFilesInClipboard  │
```

### 4.11 ClipboardMonitor

> **File:** `Poshtibano.Desk/Services/ClipboardMonitor.cs`  
> **Role:** Monitors Windows clipboard changes using Win32 API

#### Implementation

```csharp
public class ClipboardMonitor : NativeWindow, IDisposable
```

Inherits from `NativeWindow` to receive Windows messages.

#### Win32 API

| Function                              | Usage / Application                               |
|---------------------------------------|---------------------------------------------------|
| `AddClipboardFormatListener(hwnd)`    | Register to receive `WM_CLIPBOARDUPDATE` messages |
| `RemoveClipboardFormatListener(hwnd)` | Unregister / stop receiving clipboard updates     |

#### Important methods

```csharp
// Constructor: create NativeWindow handle
public ClipboardMonitor()  // ← CreateHandle(new CreateParams())

// Start monitoring
public void Start()  // ← AddClipboardFormatListener(Handle)

// Stop monitoring
public void Stop()   // ← RemoveClipboardFormatListener(Handle)

// Process Windows messages
protected override void WndProc(ref Message m)
// ← if WM_CLIPBOARDUPDATE → OnClipboardChanged?.Invoke()

// Dispose
public void Dispose()  // ← Stop + DestroyHandle
```

#### Event

```csharp
public event Action OnClipboardChanged;
```

> **Note:** This class **must** be created on the UI Thread because `NativeWindow` requires a message pump.

### 4.12 ProcessNameManager

> **File:** `Poshtibano.Desk/Services/ProcessNameManager.cs`  
> **Role:** Detects the process under the mouse cursor + checks forbidden processes

#### Win32 API

| Function                                  | Usage / Application                                                                                                       |
|-------------------------------------------|---------------------------------------------------------------------------------------------------------------------------|
| `GetForegroundWindow()`                   | Get the handle of the currently active/foreground window                                                                  |
| `WindowFromPoint(Point)`                  | Get the window handle at a specific screen point / coordinates                                                            |
| `GetWindowThreadProcessId(hwnd, out pid)` | Retrieve the process ID (PID) and thread ID associated with a window                                                      |
| `ChildWindowFromPoint(hwnd, Point)`       | Get the child window handle located at the specified point (relative to parent)                                           |
| `GetClassName(hwnd, sb, len)`             | Retrieve the class name of the specified window                                                                           |
| `SendMessage(hwnd, WM_NCHITTEST, ...)`    | Perform hit-testing to determine which part of the window (caption, border, client area, etc.) is under the mouse pointer |

#### Important methods

```csharp
// Get process name from mouse position
public string GetProcessNameByMousePosition(Point point)
// ← WindowFromPoint → GetProcessNameByHandle

// Get process name of foreground window
public string GetProcessNameByForegroundWindow()
// ← GetForegroundWindow → GetProcessNameByHandle

// Get child window from point
public string GetChildWindowFromPoint(Point point)

// Get hit test type
public string GetHitNameByMousePosition(Point point)
// ← WM_NCHITTEST → HitTestValues enum

// Check if process is forbidden
public bool IsForbidden(string processName)
```

#### Forbidden processes list

The system prevents interacting with the following security/system processes:

| Category             | Processes                                          |
|----------------------|----------------------------------------------------|
| **Windows Defender** | `msmpeng`, `smartscreen`, `nisssrv`, `secHealthui` |
| **ESET**             | `egui`, `ekrn`                                     |
| **Kaspersky**        | `avp`, `avpui`                                     |
| **Avast/AVG**        | `avastsvc`, `avastui`, `avgsvc`, `avgui`           |
| **Bitdefender**      | `vsserv`, `bdagent`, `bdredline`                   |
| **McAfee**           | `mcshield`, `mfevtps`, `mcupdmgr`                  |
| **Norton**           | `navapsvc`, `symcorpui`                            |
| **System**           | `taskmgr`, `resmon`, `mmc`                         |

#### HitTestValues Enum

```csharp
public enum HitTestValues
{
    HTNOWHERE = 0,  HTCLIENT = 1,     HTCAPTION = 2,
    HTSYSMENU = 3,  HTMENU = 5,       HTMINBUTTON = 8,
    HTMAXBUTTON = 9, HTCLOSE = 20,    // and others ...
}
```

### 4.13 MainForm and Subcomponents

> **Files:** `MainForm.cs`, `MainForm.SessionEvents.cs`, `MainForm.Controller.cs`, `MainForm.Chat.cs`, `MainForm.Clipboard.cs`, `MainForm.AudioVideo.cs`, `MainForm.UIManager.cs`, `MainForm.Monitor.cs`  
> **Role:** UI layer — render UI, handle user input, connect to SessionCoordinator

#### MainForm.cs (main file)

```csharp
public partial class MainForm : Form
{
    private SessionCoordinator _session;     // session coordinator
    private ClientRole _currentRole;         // current role
    private string _signalingUrl;            // Hub address
    private bool _isConnecting;
    private bool _passwordVerified;
    private bool _accessGranted;
}
```

Key methods:

```csharp
// Constructor: initialize UI, Settings, ApplicationGuid
public MainForm()

// Start: create SessionCoordinator + InitializeAsync
protected async Task Start()

// Form shown
protected override async void OnShown(EventArgs e)

// Form closing: cleanup session
protected override void OnFormClosing(FormClosingEventArgs e)
```

#### MainForm.SessionEvents.cs

Manages all SessionCoordinator events:

```csharp
// Attach events
private void AttachSessionEvents()
// ← _session.OnHubStateChanged += ...
// ← _session.OnPeerStateChanged += ...
// ← _session.OnSessionReady += ...
// ← _session.OnChatMessage += ...
// ← _session.OnClipboard* += ...
// ← _session.OnAudioVideo* += ...

private void DetachSessionEvents()

// Event handlers
private void Session_OnSessionReady()
// ← Controller: SetupControllerUI
// ← Agent: InitializeMonitors + StartCapture

private void Session_OnPasswordIncorrect()
private void Session_OnPasswordCorrect()
private void Session_OnRequestAccessPermission(string caller)
private void Session_OnAccessDenied()
private async void Session_OnSessionEnded(string reason)
private void Session_OnHubStateChanged(ConnectionStatus state)
private void Session_OnPeerStateChanged(ConnectionStatus state)
private void Session_OnError(string error)
```

#### MainForm.Controller.cs

Manages Controller UI (capture input, send to Agent):

```csharp
private void SetupControllerUI()
// ← attach mouse/keyboard handlers to pictureBox

private void DetachControllerUIHandlers()

// Input handlers
private void PictureBox_MouseMove(object sender, MouseEventArgs e)
private void PictureBox_MouseDown(object sender, MouseEventArgs e)
private void PictureBox_MouseUp(object sender, MouseEventArgs e)
private void PictureBox_MouseWheel(object sender, MouseEventArgs e)

// Focus management (Keyboard Suppression)
private void TextBoxChatInput_GotFocus(...)   // ← SetKeyboardSuppression(false)
private void TextBoxChatInput_LostFocus(...)  // ← SetKeyboardSuppression(true)
```

#### MainForm.Chat.cs

```csharp
private void buttonChatSend_Click(...)    // ← _session.SendChatMessageAsync
private void textBoxChatInput_KeyDown(...) // ← Enter = Send

private void Session_OnChatMessage(ChatMessage msg)  // ← AddChatBubble
private void Session_OnChatEvent(ChatEvent evt)      // ← Update bubble (like/edit/delete)
private void AddChatBubble(ChatMessage msg)           // ← create ChatBubbleControl
```

#### MainForm.Clipboard.cs

```csharp
private void Session_OnClipboardRemoteTextReceived(string text)
private void Session_OnClipboardRemoteFilesReceived(List<string> files)
private void Session_OnClipboardFileOfferReceived(transferId, fileNames, totalSize)
// ← show Accept/Reject dialog

private void Session_OnClipboardStatusChanged(string status)
private void Session_OnClipboardError(string error)
private void ShowClipboardNotification(string message)
```

#### MainForm.AudioVideo.cs

```csharp
private void AttachAudioVideoSessionEvents()
private void DetachAudioVideoSessionEvents()

// Permission request handlers
private void HandleTheyWantToSendAudio(MediaPermissionRequest request)
private void HandleTheyWantToSendWebcam(MediaPermissionRequest request)
private void HandleTheyWantToReceiveMyAudio(MediaPermissionRequest request)
private void HandleTheyWantToReceiveMyWebcam(MediaPermissionRequest request)
```

#### MainForm.UIManager.cs

```csharp
private void ApplyModernStyles()
private void UpdateConnectionStatusUI(ConnectionStatus state)

// WndProc: handle WM_CLIPBOARDUPDATE, WM_DROPFILES, WM_NCHITTEST
protected override void WndProc(ref Message m)

// Borderless window management
private void Header_MouseDown(...)   // ← Drag
private void buttonClose_Click(...)
private void buttonMinimize_Click(...)
```

## 5. Data Flow Diagrams

### 5.1 Frame flow (Agent → Controller)

```
Agent Side:                                Controller Side:
                                           
ScreenCaptureService                       PeerConnectionManager
  │ OnFrameCaptured                          │ OnFrameDataReceived
  ▼                                          ▼
SessionCoordinator                         PacketHandler
  │ HandleFrameCaptured()                    │ HandleRawData(Frame)
  │ ├── Packet(Frame, data)                  │ └── ProcessCompletePacket
  │ ├── Serialize + Fragment                 ▼
  │ └── SendFrame()                        SessionCoordinator
  ▼                                          │ OnFramePacketHandler
PeerConnectionManager                       ▼
  │ _frameChannel.send()                   FrameRenderService
  ▼                                          │ ProcessFrame()
 ═══ WebRTC Data Channel (id=1) ═══         ▼
                                           PictureBox
```

### 5.2 Input flow (Controller → Agent)

```
Controller Side:                           Agent Side:

PictureBox MouseEvent                      PeerConnectionManager
  │                                          │ OnEventDataReceived
  ▼                                          ▼
RemoteInputService                         PacketHandler
  │ OnInputEventGenerated                    │ HandleRawData(Event)
  ▼                                          ▼
SessionCoordinator                         SessionCoordinator
  │ SendInputEvent()                         │ OnEventPacketHandler
  │ └── SendEvent()                          ▼
  ▼                                        LocalInputHandler
PeerConnectionManager                        │ ProcessEventData()
  │ _controlChannel (0x01)                   │ ├── HandleMouseMove
  ▼                                          │ ├── HandleMouseButton
 ═══ WebRTC Data Channel (id=2) ═══          │ └── HandleKeyboard
                                            ▼
                                           Win32 API (mouse/keyboard injection)
```

### 5.3 File transfer flow

```
Sender Side:                               Receiver Side:

User drops file                            PeerConnectionManager
  │                                          │ OnFileDataReceived
  ▼                                          ▼
FileTransferManager                        FileTransferManager
  │ SendPathsAsync()                         │ Peer_OnFileDataReceived
  │ ├── FileStart packet                     │ ├── HandleFileStart → FileStream
  │ ├── FileChunk[0..N]                      │ ├── HandleFileChunk → Write
  │ └── FileEnd packet                       │ └── HandleFileEnd → Close/Rename
  ▼                                          ▼
PeerConnectionManager                      Events:
  │ SendFile() → _bulkChannel                  ├── OnProgressUpdated
  ▼                                            └── OnFileReceived
 ═══ WebRTC Data Channel (id=3) ═══
```

## 6. Relationships Between Classes

```
┌──────────────────────────────────────────────────────────────────┐
│                          MainForm                                │
│  ┌──────────────┐ ┌──────────────┐ ┌────────────┐ ┌───────────┐  │
│  │SessionEvents │ │ Controller   │ │    Chat    │ │ Clipboard │  │
│  │  .cs         │ │    .cs       │ │    .cs     │ │   .cs     │  │
│  └──────┬───────┘ └──────┬───────┘ └─────┬──────┘ └─────┬─────┘  │
│         │                │               │              │        │
│  ┌──────┴────────┐ ┌─────┴──────┐ ┌──────┴────┐ ┌───────┴─────┐  │
│  │  UIManager    │ │  Monitor   │ │AudioVideo │ │             │  │
│  │    .cs        │ │    .cs     │ │   .cs     │ │             │  │
│  └───────────────┘ └────────────┘ └───────────┘ └─────────────┘  │
└────────────────────────────┬─────────────────────────────────────┘
                             │ Events / Method calls
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                      SessionCoordinator                          │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐  │
│  │  .AudioVideo.cs  │ │  .Clipboard.cs   │ │    (main.cs)     │  │
│  └────────┬─────────┘ └────────┬─────────┘ └─────────┬────────┘  │
│           │                    │                     │           │
│  ┌────────▼────────────────────▼─────────────────────▼──────────┐│
│  │                    Service Composition                       ││
│  │                                                              ││
│  │  ┌─────────────────┐  ┌──────────────┐  ┌─────────────────┐  ││
│  │  │  ChatManager    │  │PacketHandler │  │FileTransferMgr  │  ││
│  │  └────────┬────────┘  └──────┬───────┘  └─────────┬───────┘  ││
│  │           │                  │                    │          ││
│  │  ┌────────▼──────────────────▼────────────────────▼─────────┐││
│  │  │          ClipboardSharingManager                         │││
│  │  └──────────────────────┬───────────────────────────────────┘││
│  │                         │                                    ││
│  │  ┌──────────────────────▼───────────────────────────────────┐││
│  │  │              PeerConnectionManager                       │││
│  │  │  ┌─────────────────────────────────────────┐             │││
│  │  │  │  PeerConnectionManager.AudioVideo.cs    │             │││
│  │  │  └─────────────────────────────────────────┘             │││
│  │  │                     │                                    │││
│  │  │  ┌──────────────────▼─────────────────────────┐          │││
│  │  │  │         HubConnectionService               │          │││
│  │  │  └────────────────────────────────────────────┘          │││
│  │  └──────────────────────────────────────────────────────────┘││
│  └──────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────┘
                             │
                      SignalR + WebRTC
                             │
┌──────────────────────────────────────────────────────────────────┐
│                    Poshtibano.Hub (Server)                       │
│                      SessionHub : Hub                            │
└──────────────────────────────────────────────────────────────────┘
```

### Summary of relationships

| From                      | To                        | Connection Type                                                |
|---------------------------|---------------------------|----------------------------------------------------------------|
| `MainForm`                | `SessionCoordinator`      | Method calls + Event subscriptions                             |
| `SessionCoordinator`      | `PeerConnectionManager`   | Ownership + Method calls + Event subscriptions                 |
| `SessionCoordinator`      | `ChatManager`             | Ownership + Method calls + Event subscriptions                 |
| `SessionCoordinator`      | `FileTransferManager`     | Ownership + Method calls + Event subscriptions                 |
| `SessionCoordinator`      | `PacketHandler`           | Ownership + Event subscriptions                                |
| `SessionCoordinator`      | `ClipboardSharingManager` | Ownership + Event subscriptions                                |
| `SessionCoordinator`      | `ClipboardMonitor`        | Ownership + Event subscriptions                                |
| `PeerConnectionManager`   | `HubConnectionService`    | Ownership + Method calls + Event subscriptions                 |
| `ChatManager`             | `PeerConnectionManager`   | Reference + Call `SendChat`                                    |
| `FileTransferManager`     | `PeerConnectionManager`   | Reference + Subscribe `OnFileDataReceived` + Call `SendFile`   |
| `ClipboardSharingManager` | `PeerConnectionManager`   | Reference + Subscribe `OnChatDataReceived` + Call `SendChat`   |
| `ClipboardSharingManager` | `FileTransferManager`     | Reference + Subscribe `OnFileReceived` + Call `SendPathsAsync` |
| `MainForm`                | `ProcessNameManager`      | Creation + Method calls                                        |


## 7. Development Best Practices

### 7.1 Thread Safety

```csharp
// ✅ Pattern using SynchronizationContext
if (_uiContext != null)
    _uiContext.Post(_ => OnNewMessage?.Invoke(msg), null);
else
    OnNewMessage?.Invoke(msg);

// ✅ locks for shared resources
private readonly object _pcLock = new object();
private readonly object _lock = new object();
private readonly object _hashLock = new object();
private readonly object _offerLock = new object();

// ✅ SemaphoreSlim for limiting concurrent sends
private readonly SemaphoreSlim _bulkSendLock = new SemaphoreSlim(1, 1);

// ✅ InvokeRequired in MainForm
if (InvokeRequired)
{
    Invoke(() => MethodName(args));
    return;
}
```

### 7.2 Dispose Pattern

All service classes implement `IDisposable`:

```csharp
public void Dispose()
{
    if (_isDisposed) return;
    _isDisposed = true;

    // 1. Cancel running operations
    _cancellationTokenSource?.Cancel();

    // 2. Detach events
    _peer.OnDataReceived -= HandleData;

    // 3. Free resources
    _service?.Dispose();
    _service = null;

    // 4. Cleanup events
    OnEvent = null;
}
```

### 7.3 Event Handling

```csharp
// ✅ Correct pattern: -= before += to avoid duplicate subscription
_hubService.OnSdpOfferReceived -= HandleSdpOffer;
_hubService.OnSdpOfferReceived += HandleSdpOffer;

// ✅ Named handlers instead of lambdas (for easy detach)
_packetHandler.OnFramePacket += OnFramePacketHandler;
// later:
_packetHandler.OnFramePacket -= OnFramePacketHandler;

// ✅ check null and dispose
if (_isDisposed || !_isConnected)
{
    Console.WriteLine("Ignoring event - disposed or disconnected");
    return;
}
```

### 7.4 Reconnect logic

```
Agent Reconnect Strategy:
─────────────────────────
Hub disconnected
  │
  ├── RetryPolicy (automatic via SignalR)
  │   └── WithAutomaticReconnect
  │
  ├── OnHubConnectionClosed
  │   └── if (Agent && !disposed) → UpdateState(Reconnecting)
  │
  └── Manual retry: RetryConnectionAsync
      ├── delay = min(2^attempt * 1000, 30000)  ms
      └── max 10 attempts

Peer Disconnected Strategy:
───────────────────────────
Agent: waits for reconnect
Controller: is notified → OnSessionEnded
```

### 7.5 Logging convention

```
✅  → success
❌  → error
⚠️  → warning
📡  → network status
📤  → send data
📥  → receive data
📋  → clipboard operations
🎤  → audio
📷  → webcam
🗑️  → Dispose
🧹  → Cleanup
🔌  → connect/disconnect
🚀  → start
📢  → notification
🔐  → authentication
```

### 7.6 Important notes

1. **SynchronizationContext**: Always store `_uiContext` in the constructor. In WinForms, `SynchronizationContext.Current` only has a value on the UI Thread.

2. **Negotiated Channels**: All 5 data channels are created with `negotiated=true`. Any change to IDs or settings must be applied on **both sides**.

3. **Packet Fragmentation**: Packets larger than `Packet.MaxFragmentSize` are automatically fragmented. `PacketHandler` reassembles them.

4. **Shared Control Channel**: The `control` channel (id=2) is shared for Events and Chat. The first byte defines the message type (`0x01`=Event, `0x02`=Chat).

5. **Clipboard on STA Thread**: `Clipboard.GetText()` and `Clipboard.SetText()` must be executed on an STA Thread. `new Thread` with `SetApartmentState(ApartmentState.STA)` is used.

6. **ClipboardMonitor on UI Thread**: `ClipboardMonitor` inherits `NativeWindow` and must be created on the UI Thread.

7. **Forbidden processes**: `ProcessNameManager` prevents clicking/interacting with security processes (antivirus, Task Manager).

8. **Low-Latency Packets**: `Frame`, `AudioData` and `WebcamData` packets are processed without `_uiContext.Post` to minimize latency.