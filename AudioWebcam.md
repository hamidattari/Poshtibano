# Poshtibano Desk Audio & Webcam Transfer Architecture

> **Focus:** Audio/Video Streaming Architecture  
> **Communication Layer:** WebRTC Data Channels (id=4, id=5) + SignalR Hub  

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Audio Streaming](#2-audio-streaming-architecture)
3. [Webcam Streaming](#3-webcam-streaming-architecture)
4. [Permission Management](#4-permission-management)
5. [State Management](#5-state-management)
6. [Data Packet Structures](#6-data-packet-structures)
7. [Complete Data Flows](#7-complete-data-flows)
8. [State Machines](#8-state-machines)
9. [Error Handling](#9-error-handling)
10. [End-to-End Scenarios](#10-end-to-end-scenarios)


## 1. System Architecture

### 1.1 Three-Layer Audio/Video Architecture

```
┌────────────────────────────────────────────────────────────────┐
│                      UI LAYER                                  │
│                                                                │
│  ┌──────────────────────┐  ┌──────────────────────┐            │
│  │ MainForm.            │  │ Permission Dialogs   │            │
│  │ AudioVideo.cs        │  │ • Request forms      │            │
│  │                      │  │ • Accept/Reject      │            │
│  │ • 4 Media buttons    │  │ • State display      │            │
│  │ • Status updates     │  │                      │            │
│  │ • State visualize    │  │ WebcamViewerForm     │            │
│  └──────────────────────┘  │ • Video preview      │            │
│                            │ • Audio indicator    │            │
│                            └──────────────────────┘            │
│                                                                │
└────────────────────────────┬───────────────────────────────────┘
                             │
                 Event Callbacks & State Updates
                             │
┌────────────────────────────▼─────────────────────────────────┐
│                    SERVICE LAYER                             │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │    SessionCoordinator.AudioVideo.cs                     │ │
│  │                                                         │ │
│  │  • Audio Capture Service                                │ │
│  │  • Audio Playback Service                               │ │
│  │  • Video Capture Service                                │ │
│  │  • AudioVideoStateManager                               │ │
│  │  • Permission Request Handling                          │ │
│  │  • Webcam Viewer Management                             │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
└──────────────────────────────┬───────────────────────────────┘
                             │
            Capture/Playback Data & SignalR Messages
                             │
┌────────────────────────────▼─────────────────────────────────┐
│                  NETWORK LAYER                               │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │    PeerConnectionManager.AudioVideo.cs                  │ │
│  │                                                         │ │
│  │  WebRTC Channels:                                       │ │
│  │  • Channel id=4: Audio Data (unordered, max500ms)       │ │
│  │  • Channel id=5: Webcam Data (unordered, max500ms)      │ │
│  │                                                         │ │
│  │  SignalR Hub:                                           │ │
│  │  • Availability messages                                │ │
│  │  • Permission requests/responses                        │ │
│  │  • State change signals                                 │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
└────────────────────────────┬─────────────────────────────────┘
                             │
                    ═════════════════════
                    Remote Peer (Agent/Controller)
                    ═════════════════════
```

### 1.2 Communication Channels

| Channel ID | Type                | Purpose          | Latency           | Reliability | Capacity      |
|------------|---------------------|------------------|-------------------|-------------|---------------|
| **4**      | WebRTC Data Channel | Audio streaming  | Ultra-low (<50ms) | Unordered   | ~64 Kbps      |
| **5**      | WebRTC Data Channel | Webcam streaming | Low (<100ms)      | Unordered   | ~256 Kbps     |
| **Hub**    | SignalR             | Control messages | Medium (~500ms)   | Ordered     | Metadata only |

---

## 2. Audio Streaming Architecture

### 2.1 Audio Overview

```
┌─────────────────────────────────────────────────────┐
│        AUDIO STREAMING LIFECYCLE                    │
│                                                     │
│  INITIALIZATION:                                    │
│  • User clicks "Send My Audio" button               │
│  • RequestToSendMyAudioAsync() triggered            │
│  • Permission request sent via SignalR Hub          │
│                                                     │
│  PERMISSION PHASE:                                  │
│  • Remote user receives dialog                      │
│  • User accepts/rejects permission                  │
│  • RespondToTheirReceiveMyAudioRequestAsync()       │
│                                                     │
│  CAPTURE PHASE:                                     │
│  • StartAudioCapture() called                       │
│  • AudioCaptureService created & started            │
│  • OnLocalAudioCaptured() fired continuously        │
│                                                     │
│  TRANSMISSION PHASE:                                │
│  • SendAudioData() packs audio with metadata        │
│  • Sends via Channel 4 (WebRTC)                     │
│  • ~20-40ms audio frames                            │
│                                                     │
│  PLAYBACK PHASE:                                    │
│  • OnAudioDataReceived event fires                  │
│  • AudioPlaybackService.AddSamples()                │
│  • Audio played through speakers                    │
│                                                     │
│  STOP PHASE:                                        │
│  • StopSendingMyAudioAsync() called                 │
│  • AudioCaptureService.StopAsync()                  │
│  • SendStopMyAudioAsync() notification              │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### 2.2 Audio Architecture Components

| Layer       | Class/Method                  | Responsibility         | Inputs             | Outputs                         |
|-------------|-------------------------------|------------------------|--------------------|---------------------------------|
| **UI**      | `ButtonMyMic_Click()`         | User initiates audio   | Button event       | State change                    |
| **UI**      | `UpdateButtonState()`         | Visual feedback        | `MediaStreamState` | Button color/text               |
| **Service** | `RequestToSendMyAudioAsync()` | Permission request     | `name`, `role`     | Hub message                     |
| **Service** | `StartAudioCapture()`         | Initialize capture     | N/A                | `AudioCaptureService` instance  |
| **Service** | `OnLocalAudioCaptured()`      | Process captured audio | `byte[]`, metadata | `SendAudioData()` call          |
| **Service** | `StartAudioPlayback()`        | Initialize playback    | N/A                | `AudioPlaybackService` instance |
| **Network** | `SendAudioData()`             | Pack & send audio      | `byte[]`, settings | Packed binary packet            |
| **Network** | `SetupAudioChannel()`         | Setup WebRTC channel   | RTCDataChannelInit | Active channel                  |

### 2.3 Audio Capture Settings

| Setting                  | Value | Unit    | Purpose                    |
|--------------------------|-------|---------|----------------------------|
| **Sample Rate**          | 16000 | Hz      | Standard telephony quality |
| **Channels**             | 1     | Channel | Mono (not stereo)          |
| **Bits Per Sample**      | 16    | bits    | Audio resolution (PCM)     |
| **Device Index**         | 0     | -       | Default microphone         |
| **Frame Duration**       | ~20   | ms      | Typical frame size         |
| **Bitrate (calculated)** | ~256  | Kbps    | 16000 × 1 × 16 / 1000      |

### 2.4 Audio Packet Structure

```
┌────────────────────────────────────────────┐
│     AUDIO DATA PACKET STRUCTURE            │
├────────────────────────────────────────────┤
│ Field              │ Type  │ Size │ Offset │
├────────────────────┼───────┼──────┼────────┤
│ SampleRate         │ int32 │  4B  │   0    │
│ Channels           │ int32 │  4B  │   4    │
│ BitsPerSample      │ int32 │  4B  │   8    │
│ TimestampTicks     │ int64 │  8B  │  12    │
│ SequenceNumber     │ uint32│  4B  │  20    │
│ AudioData (PCM)    │ byte[]│ Var  │  24+   │
├────────────────────┴───────┴──────┴────────┤
│ TOTAL HEADER: 24 bytes + variable data     │
│ Channel: WebRTC id=4 (unordered)           │
│ Max Lifetime: 500ms                        │
└────────────────────────────────────────────┘
```

### 2.5 Audio Data Flow

```
SENDER SIDE                          RECEIVER SIDE
─────────────────────────────────────────────────

AudioCaptureService                  OnAudioDataReceived
  │ OnAudioCaptured event              │ event fires
  ▼                                    ▼
OnLocalAudioCaptured()               AudioPlaybackService
  │ Validates data                     │ AddSamples()
  ├─ Check: _peerManager != null       │
  ├─ Check: _isConnected               ├─ Queue samples
  │                                    ├─ Buffer management
  ▼                                    │
SendAudioData()                        ▼
  │ Pack format:                      Audio Output
  │ [sampleRate:4]                    │ Speaker/Headphones
  │ [channels:4]                      │
  │ [bitsPerSample:4]                 │ ▼
  │ [timestampTicks:8]                [Sound to user]
  │ [sequenceNumber:4]
  │ [audioData:rest]
  │
  ▼
_audioChannel.send(packet)
  │ WebRTC Channel id=4
  │
  ├─► Network (P2P)
  │
  └─► Remote receives
```

## 3. Webcam Streaming Architecture

### 3.1 Webcam Overview

```
┌──────────────────────────────────────────────────┐
│      WEBCAM STREAMING LIFECYCLE                  │
│                                                  │
│  INITIALIZATION:                                 │
│  • User clicks "Send My Webcam" button           │
│  • RequestToSendMyWebcamAsync() triggered        │
│  • Permission request sent via SignalR Hub       │
│                                                  │
│  PERMISSION PHASE:                               │
│  • Remote user receives dialog                   │
│  • User accepts/rejects permission               │
│  • RespondToTheirReceiveMyWebcamAsync()          │
│                                                  │
│  CAPTURE PHASE:                                  │
│  • StartWebcamCapture() called                   │
│  • VideoCaptureService created & started         │
│  • OnLocalWebcamFrameCaptured() fired            │
│    (typically 15 FPS)                            │
│                                                  │
│  TRANSMISSION PHASE:                             │
│  • SendWebcamData() packs video + optional audio │
│  • Sends via Channel 5 (WebRTC)                  │
│  • ~66-67ms frames (15 FPS)                      │
│                                                  │
│  VIEWER PHASE:                                   │
│  • OnWebcamDataReceived event fires              │
│  • ShowWebcamViewer() opens window               │
│  • _webcamViewerForm.UpdateFrame()               │
│  • Video displayed in real-time                  │
│                                                  │
│  STOP PHASE:                                     │
│  • StopSendingMyWebcamAsync() called             │
│  • VideoCaptureService.StopAsync()               │
│  • Also stops audio if streaming with webcam     │
│  • SendStopMyWebcamAsync() notification          │
│                                                  │
└──────────────────────────────────────────────────┘
```

### 3.2 Webcam Architecture Components

| Layer       | Class/Method                   | Responsibility       | Inputs             | Outputs               |
|-------------|--------------------------------|----------------------|--------------------|-----------------------|
| **UI**      | `ButtonMyCam_Click()`          | User initiates video | Button event       | State change          |
| **UI**      | `WebcamViewerForm`             | Display remote video | `videoData`, dims  | Rendered frame        |
| **Service** | `RequestToSendMyWebcamAsync()` | Permission request   | `name`, `role`     | Hub message           |
| **Service** | `StartWebcamCapture()`         | Initialize capture   | N/A                | `VideoCaptureService` |
| **Service** | `OnLocalWebcamFrameCaptured()` | Process video frame  | `byte[]`, dims     | `SendWebcamData()`    |
| **Service** | `ShowWebcamViewer()`           | Create viewer window | `senderName`       | `WebcamViewerForm`    |
| **Service** | `ShowWebcamViewerInternal()`   | Thread-safe viewer   | `senderName`       | Window instance       |
| **Network** | `SendWebcamData()`             | Pack & send video    | `byte[]`, settings | Packed packet         |
| **Network** | `SetupWebcamChannel()`         | Setup WebRTC channel | RTCDataChannelInit | Active channel        |

### 3.3 Webcam Capture Settings

| Setting              | Value    | Unit   | Purpose                     |
|----------------------|----------|--------|-----------------------------|
| **Target FPS**       | 15       | fps    | Balance quality & bandwidth |
| **Quality**          | 50       | %      | JPEG/H.264 compression      |
| **Device Index**     | -1       | -      | Auto-select best device     |
| **Resolution**       | Variable | pixels | Depends on device           |
| **Bitrate (approx)** | ~256-512 | Kbps   | Varies by quality           |
| **Frame Interval**   | ~67      | ms     | 1000 / 15 fps               |

### 3.4 Webcam Packet Structure

```
┌────────────────────────────────────────────────┐
│    WEBCAM DATA PACKET STRUCTURE                │
├────────────────────────────────────────────────┤
│ Field              │ Type  │ Size │ Offset     │
├────────────────────┼───────┼──────┼────────────┤
│ Width              │ int32 │  4B  │   0        │
│ Height             │ int32 │  4B  │   4        │
│ AudioSampleRate    │ int32 │  4B  │   8        │
│ AudioChannels      │ int32 │  4B  │  12        │
│ TimestampTicks     │ int64 │  8B  │  16        │
│ SequenceNumber     │ uint32│  4B  │  24        │
│ VideoDataLen       │ int32 │  4B  │  28        │
│ VideoData (JPEG)   │ byte[]│ Var  │  32        │
│ AudioData (PCM)    │ byte[]│ Opt  │ 32+VidLen  │
├────────────────────┴───────┴──────┴────────────┤
│ TOTAL HEADER: 32 bytes + video + optional audio│
│ Channel: WebRTC id=5 (unordered)               │
│ Max Lifetime: 500ms                            │
│ Note: Audio optional, included if webcam also  │
│       captures audio                           │
└────────────────────────────────────────────────┘
```

### 3.5 Webcam Data Flow

```
SENDER SIDE                          RECEIVER SIDE
─────────────────────────────────────────────────

VideoCaptureService                  OnWebcamDataReceived
  │ OnFrameCaptured event              │ event fires
  ▼                                    ▼
OnLocalWebcamFrameCaptured()         HandleWebcamDataReceived()
  │ Validates video                    │ Parses packet
  ├─ Check: _peerManager != null       ├─ Extract dimensions
  ├─ Check: _isConnected               ├─ Extract audio (if any)
  │                                    │
  ▼                                    ▼
SendWebcamData()                     ShowWebcamViewer()
  │ Pack format:                       │ Creates window (if needed)
  │ [width:4]                          │
  │ [height:4]                         ▼
  │ [audioSampleRate:4]               WebcamViewerForm
  │ [audioChannels:4]                  │ UpdateFrame()
  │ [timestampTicks:8]                 │
  │ [sequenceNumber:4]                 ├─ Decode JPEG/H.264
  │ [videoDataLen:4]                   ├─ Render to control
  │ [videoData:var]                    │
  │ [audioData:opt]                    ▼
  │                                    [Video displayed]
  ▼
_webcamChannel.send(packet)          Optional Audio
  │ WebRTC Channel id=5                │ Feed to AudioPlaybackService
  │                                    │ AddSamples()
  ├─► Network (P2P)                    ▼
  │                                    [Audio played]
  └─► Remote receives
```

## 4. Permission Management

### 4.1 Permission Request/Response Flow

```
┌──────────────────────────────────────────────────────────┐
│         AUDIO PERMISSION REQUEST FLOW                    │
├──────────────────────────────────────────────────────────┤
│                                                          │
│ REQUESTER SIDE:         RESPONDER SIDE:                  │
│ ────────────────        ──────────────                   │
│                                                          │
│ RequestToSend...Async() ────┐                            │
│  └─ Set state: Requesting   │                            │
│     └─ Send via Hub         │                            │
│        (SignalR)            │                            │
│                             ▼                            │
│                    OnTheyWantToSend... event             │
│                    └─ MainForm handler                   │
│                      └─ Show dialog                      │
│                         [Accept] [Reject]                │
│                                                          │
│                         User clicks                      │
│                             │                            │
│            ┌────────────────┴────────────────┐           │
│            │                                 │           │
│      [Accept]                          [Reject]          │
│            │                                 │           │
│            ▼                                 ▼           │
│  RespondTo...Async(true)    RespondTo...Async(false)     │
│    └─ Send response via Hub                 │            │
│                                             │            │
│                             ┌───────────────┘            │
│                             │                            │
│                             ▼                            │
│           OnMyAudioSendResponseReceived                  │
│           ├─ If allowed:                                 │
│           │   └─ StartAudioCapture()                     │
│           │   └─ State: Streaming                        │
│           │                                              │
│           └─ If denied:                                  │
│               └─ State: Denied                           │
│               └─ Show error dialog                       │
│                                                          │
│ ✅ Stream established or ❌ Permission denied           │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

### 4.2 Permission Types & Methods

| Permission Type       | Request Method                  | Response Method                      | Event                         | Use Case             |
|-----------------------|---------------------------------|--------------------------------------|-------------------------------|----------------------|
| **My Audio Send**     | `SendMyAudioRequestAsync()`     | `SendMyAudioReceiveResponseAsync()`  | `OnTheyWantToReceiveMyAudio`  | I want to send audio |
| **My Webcam Send**    | `SendMyWebcamRequestAsync()`    | `SendMyWebcamReceiveResponseAsync()` | `OnTheyWantToReceiveMyWebcam` | I want to send video |
| **Their Audio Recv**  | `SendTheirAudioRequestAsync()`  | `SendTheirAudioSendResponseAsync()`  | `OnTheyWantToSendAudio`       | I want to hear them  |
| **Their Webcam Recv** | `SendTheirWebcamRequestAsync()` | `SendTheirWebcamSendResponseAsync()` | `OnTheyWantToSendWebcam`      | I want to see them   |

### 4.3 Availability Messaging

```
┌───────────────────────────────────────────┐
│  MEDIA AVAILABILITY EXCHANGE              │
├───────────────────────────────────────────┤
│                                           │
│  ON SESSION READY:                        │
│  ────────────────                         │
│                                           │
│  Local side detects hardware:             │
│  • MediaDeviceDetector.GetDeviceSummary() │
│    └─ Returns: (hasMic, hasCam, ...)      │
│                                           │
│  SendWebcamAndMicrophoneAvailbility()     │
│    └─ Send via SignalR Hub:               │
│       {                                   │
│         type: "media_availability",       │
│         hasMicrophone: bool,              │
│         hasWebcam: bool                   │
│       }                                   │
│                                           │
│  REMOTE RECEIVES:                         │
│  ────────────────                         │
│                                           │
│  OnTheirMicophoneAndWebcamAvailability    │
│  Received event                           │
│    └─ MainForm.Initialize...UI()          │
│       └─ Only show buttons for            │
│           available hardware              │
│                                           │
└───────────────────────────────────────────┘
```

### 4.4 Active State Management

```
┌────────────────────────────────────────┐
│   DEVICE ACTIVE/INACTIVE STATE         │
├────────────────────────────────────────┤
│                                        │
│  User right-clicks microphone button   │
│    └─ Context menu appears:            │
│       ✅ Enable / ❌ Disable          │
│                                        │
│  SetMyMicrophoneActiveAsync(bool)      │
│    ├─ If deactivating:                 │
│    │  ├─ StopSendingMyAudioAsync()     │
│    │  ├─ State: Idle                   │
│    │  └─ Send deactivated signal       │
│    │                                   │
│    └─ Send via Hub:                    │
│       {                                │
│         type: "my_microphone_active",  │
│         active: bool                   │
│       }                                │
│                                        │
│  REMOTE RECEIVES:                      │
│  ────────────────                      │
│                                        │
│  OnTheirMicrophoneActiveReceived       │
│    └─ Disable "request audio" button   │
│    └─ Show disabled visual state       │
│                                        │
└────────────────────────────────────────┘
```

## 5. State Management

### 5.1 Media Stream States

| State                 | Description              | Can Transmit | Can Receive | UI Color   |
|-----------------------|--------------------------|--------------|-------------|------------|
| **Idle**              | No activity              | ❌ No       | ❌ No       | Gray       |
| **Requesting**        | Permission requested     | ⏳ Waiting  | ❌ No       | Gray       |
| **WaitingPermission** | Awaiting remote approval | ⏳ Waiting  | ❌ No       | Gray       |
| **Streaming**         | Active data transfer     | ✅ Yes      | ✅ Yes      | Orange     |
| **Stopped**           | Gracefully stopped       | ❌ No       | ❌ No       | Gray       |
| **Disabled**          | Hardware unavailable     | ❌ No       | ❌ No       | Light Gray |
| **Denied**            | Permission rejected      | ❌ No       | ❌ No       | Red        |

### 5.2 Audio State Transitions

```
                    Idle
                    │
    ┌───────────────┼───────────────┐
    │               │               │
    ▼               ▼               ▼
Requesting    Disabled       Waiting
    │               │        Permission
    │               │               │
    └───────────────┼───────────────┘
                    │
           (permission granted)
                    │
                    ▼
              Streaming
                    │
    ┌───────────────┼───────────────┐
    │               │               │
    ▼               ▼               ▼
 Stopped        Denied           Idle
(user stops) (rejected) (permission expired)
```

### 5.3 AudioVideoStateManager

| Method                         | Parameters         | Returns            | Purpose                         |
|--------------------------------|--------------------|--------------------|---------------------------------|
| `SetLocalAudioState()`         | `MediaStreamState` | void               | Update local audio state        |
| `SetLocalWebcamState()`        | `MediaStreamState` | void               | Update local webcam state       |
| `SetRemoteAudioState()`        | `MediaStreamState` | void               | Update remote audio state       |
| `SetRemoteWebcamState()`       | `MediaStreamState` | void               | Update remote webcam state      |
| `LocalAudioState` (property)   | N/A                | `MediaStreamState` | Get current local audio state   |
| `LocalWebcamState` (property)  | N/A                | `MediaStreamState` | Get current local webcam state  |
| `RemoteAudioState` (property)  | N/A                | `MediaStreamState` | Get current remote audio state  |
| `RemoteWebcamState` (property) | N/A                | `MediaStreamState` | Get current remote webcam state |


## 6. Data Packet Structures

### 6.1 Channel Configuration Comparison

| Property                | Audio Ch. (4) | Webcam Ch. (5)   |
|-------------------------|---------------|------------------|
| **Ordered**             | false         | false            |
| **Max Packet Lifetime** | 500ms         | 500ms            |
| **Max Retransmits**     | 0 (commented) | 0 (commented)    |
| **Negotiated**          | true          | true             |
| **Channel ID**          | 4             | 5                |
| **Data Type**           | PCM Audio     | JPEG/H.264 Video |
| **Typical Frame Size**  | ~1-4 KB       | ~20-100 KB       |
| **Frame Rate**          | Continuous    | 15 FPS           |

### 6.2 Audio Packet Binary Format

```
Byte Layout (Big-Endian):

Offset  Size  Type    Field
──────  ────  ──────  ──────────────────
0       4     int32   SampleRate (16000)
4       4     int32   Channels (1)
8       4     int32   BitsPerSample (16)
12      8     int64   TimestampTicks
20      4     uint32  SequenceNumber
24      N     byte[]  PCM Audio Data

Example: 20ms @ 16kHz mono
  Samples: 16000 * 0.02 = 320 samples
  Bytes: 320 * 2 bytes = 640 bytes
  Total packet: 24 + 640 = 664 bytes
```

### 6.3 Webcam Packet Binary Format

```
Byte Layout (Big-Endian):

Offset  Size  Type    Field
──────  ────  ──────  ───────────────────
0       4     int32   Width (e.g., 640)
4       4     int32   Height (e.g., 480)
8       4     int32   AudioSampleRate (0 or 16000)
12      4     int32   AudioChannels (0 or 1)
16      8     int64   TimestampTicks
24      4     uint32  SequenceNumber
28      4     int32   VideoDataLen
32      N     byte[]  JPEG/H.264 Video Data
32+N    M     byte[]  Optional PCM Audio Data

Example: 640x480 JPEG frame + audio
  JPEG size: ~30 KB (approx)
  Audio (20ms): ~640 bytes
  Total packet: 32 + 30720 + 640 = 31392 bytes
```

---

## 7. Complete Data Flows

### 7.1 Audio Streaming Flow

```
SENDER (Wants to send audio)          RECEIVER (Wants to receive)
─────────────────────────────         ───────────────────────────

User clicks "Send My Audio"
  │
  ▼
ButtonMyMic_Click()
  │ Check: state != Streaming
  │
  ├─► RequestToSendMyAudioAsync()
  │   ├─ Set state: Requesting
  │   ├─ Send via Hub:
  │   │  {
  │   │    type: "request_send_my_audio",
  │   │    requesterName: "Ali",
  │   │    requesterRole: "Agent"
  │   │  }
  │   └─ Set state: WaitingPermission
  │
  │                              Hub Message Received
  │                              │
  │                              ▼
  │                      OnTheyWantToReceiveMyAudio
  │                      └─ MainForm handler
  │                         └─ Show dialog:
  │                            "Ali wants to send audio"
  │                            [Accept] [Reject]
  │
  │                      User clicks Accept
  │                      │
  │                      ▼
  │              RespondToTheirReceiveMyAudio
  │              RequestAsync(true)
  │              └─ Send via Hub:
  │                 {
  │                   type: "response_my_audio_receive",
  │                   allowed: true
  │                 }
  │
  ├◄──────────────────────────────────────────
  │ Hub Message Received
  │
  ▼
OnMyAudioSendResponseReceived(true)
  ├─ Set state: Streaming
  └─► StartAudioCapture()
      ├─ Create AudioCaptureService
      │  (16kHz, 1 ch, 16-bit)
      ├─ Subscribe to OnAudioCaptured
      └─ Start recording

[Continuous capture loop]
  │
  ├─► OnLocalAudioCaptured()
  │   ├─ Validate: _peerManager != null
  │   ├─ Validate: _isConnected == true
  │   │
  │   └─► SendAudioData()
  │       ├─ Pack: [sr:4][ch:4][bps:4][ts:8][seq:4][data]
  │       │
  │       ▼
  │     _audioChannel.send(packet)
  │     (WebRTC Channel 4)
  │
  │                              WebRTC Packet Arrives
  │                              │
  │                              ▼
  │                      OnAudioDataReceived event
  │                      └─ SessionCoordinator handler
  │                         └─ HandleAudioDataReceived()
  │                            ├─ Validate audio data
  │                            ├─ Create playback service
  │                            │
  │                            └─► StartAudioPlayback()
  │                                ├─ Create AudioPlaybackService
  │                                │  (16kHz, 1 ch, 16-bit)
  │                                └─ Start player
  │
  │                            [Continuous playback loop]
  │                            │
  │                            ├─► AddSamples(audioData)
  │                            │   └─ Queue to output buffer
  │                            │   └─ Play through speaker
  │

[User clicks Stop]
  │
  ▼
ButtonMyMic_Click()
  │ Check: state == Streaming
  │
  ├─► StopSendingMyAudioAsync()
  │   ├─ audioCaptureService.StopAsync()
  │   ├─ Dispose service
  │   ├─ Set state: Stopped
  │   │
  │   └─► SendStopMyAudioAsync()
  │       └─ Send via Hub:
  │          {
  │            type: "stop_my_audio"
  │          }
  │
  │                              Hub Message Received
  │                              │
  │                              ▼
  │                      OnStopAudioReceived event
  │                      └─ Handle stop:
  │                         ├─ Stop playback service
  │                         ├─ Dispose service
  │                         └─ Set state: Stopped

✅ Audio Stream Complete
```

### 7.2 Webcam Streaming Flow (Summary)

```
Sender: User clicks "Send My Webcam"
  │
  ├─► RequestToSendMyWebcamAsync()
  │   └─ Send request via Hub
  │
  ├◄─ RespondToTheirReceiveMyWebcam(true)
  │   └─ Allowed by receiver
  │
  ├─► StartWebcamCapture()
  │   ├─ VideoCaptureService (15 FPS, quality 50%)
  │   └─ Subscribe to OnFrameCaptured
  │
  ├─► OnLocalWebcamFrameCaptured() [Continuous]
  │   │
  │   ├─► SendWebcamData()
  │   │   ├─ Pack: [w:4][h:4][asr:4][ach:4][ts:8][seq:4][vidlen:4][vid][aud]
  │   │   │
  │   │   └─► _webcamChannel.send(packet)
  │   │       (WebRTC Channel 5)
  │   │
  │   └─ Wait ~67ms (15 FPS)

Receiver: Packets arrive
  │
  ├─► OnWebcamDataReceived event
  │   │
  │   ├─► HandleWebcamDataReceived()
  │   │   ├─ Parse packet
  │   │   ├─ Check viewer exists
  │   │   │
  │   │   ├─► ShowWebcamViewer() [if needed]
  │   │   │   └─ Create WebcamViewerForm window
  │   │   │
  │   │   └─► _webcamViewerForm.UpdateFrame()
  │   │       └─ Render JPEG to control
  │   │
  │   └─ If audio present:
  │       └─ AddSamples() to playback

[User closes viewer or clicks Stop]
  │
  ├─► StopSendingMyWebcamAsync()
  ��   ├─ videoCaptureService.StopAsync()
  │   ├─ Also: audioCaptureService.StopAsync()
  │   │
  │   └─► SendStopMyWebcamAsync()
  │       └─ Send via Hub

✅ Webcam Stream Complete
```

## 8. State Machines

### 8.1 Local Audio State Machine

```
                    ┌──────────┐
                    │   Idle   │
                    └────┬─────┘
                         │
         User clicks button or permission request received
                         │
                    ┌────▼──────────┐
                    │  Requesting/  │
                    │ Waiting Perm  │
                    └────┬──────────┘
                         │
              ┌──────────┼──────────┐
              │                     │
          [Allowed]           [Denied]
              │                     │
              ▼                     ▼
        ┌──────────┐         ┌────────┐
        │Streaming │         │ Denied │
        └────┬─────┘         └────────┘
             │ (StartAudioCapture)
             │ OnLocalAudioCaptured fires continuously
             │
             ├─ User stops
             │  │
             └──┬────────────────────┐
                ▼                    ▼
            ┌───────┐            ┌───────────┐
            │Stopped│            │ Disabled  │
            │(user) │            │(HW unavail)
            └───────┘            └───────────┘
```

### 8.2 Remote Webcam State Machine

```
                    ┌──────────┐
                    │   Idle   │◄──────────────┐
                    └────┬─────┘               │
                         │                     │
         Request to see their webcam           │
                         │                     │
                    ┌────▼──────────┐          │
                    │  Requesting/  │          │
                    │ Waiting Perm  │          │
                    └────┬──────────┘          │
                         │                     │
              ┌──────────┼──────────┐          │
              │                     │          │
          [Allowed]           [Denied]         │
              │                     │          │
              ▼                     ▼          │
        ┌──────────┐         ┌────────┐        │
        │Streaming │         │ Denied │        │
        │ShowViewer│         └────────┘        │
        └────┬─────┘                           │
             │ OnWebcamDataReceived fires      │
             │ UpdateFrame() continuously      │
             │                                 │
      ┌──────┼──────────────┬──────────┐       │
      │      │              │          │       │
   [Stop] [Close] [Remote  [Stream   ──┼───────┘
   user   viewer   stops]  expires]    │
      │      │       │         │       │
      └──────┴───────┼─────────┘       │
                     │                 │
                     └─────────────────┘
```

---

## 9. Error Handling

### 9.1 Audio/Webcam Error Scenarios

| Error Scenario                | Trigger                  | Handling               | Recovery               |
|-------------------------------|--------------------------|------------------------|------------------------|
| **No Microphone**             | `StartAudioCapture()`    | Catch exception        | Show disabled button   |
| **No Webcam**                 | `StartWebcamCapture()`   | Catch exception        | Show disabled button   |
| **Capture Service Null**      | `OnLocalAudioCaptured()` | Check & return         | Skip frame             |
| **Peer Manager Disconnected** | `SendAudioData()`        | Check `_isConnected`   | Buffer and retry       |
| **Channel Not Open**          | `send()`                 | Check `readyState`     | Skip send              |
| **Playback Service Null**     | `OnAudioDataReceived()`  | Create on demand       | `StartAudioPlayback()` |
| **Viewer Already Exists**     | `ShowWebcamViewer()`     | Check & reuse          | Bring to front         |
| **Viewer Closed by User**     | User closes window       | `OnViewerClosed` event | Clean up references    |

### 9.2 Error Recovery Strategy

```
┌─────────────────────────────────────────────┐
│    ERROR RECOVERY FLOW                      │
├─────────────────────────────────────────────┤
│                                             │
│  TRY:                                       │
│    StartAudioCapture()                      │
│                                             │
│  CATCH (Exception ex):                      │
│    ├─ Log: ❌ Error: {ex.Message}          │
│    ├─ Set state: Disabled                   │
│    ├─ Update button: Gray disabled color    │
│    │                                        │
│    └─ Do NOT retry automatically            │
│        (requires user interaction)          │
│                                             │
│  Result:                                    │
│    • Service cleanup complete               │
│    • UI reflects error state                │
│    • No cascading failures                  │
│    • User aware through button state        │
│                                             │
└─────────────────────────────────────────────┘
```

## 10. End-to-End Scenarios

### 10.1 Audio Call Scenario

```
TIMELINE:        AGENT                        CONTROLLER
─────────────────────────────────────────────────────

14:00:00
  User copies:   "Hi Ali, can you hear me?"
                 │
                 ├─ Clicks "Send My Audio"
                 │ [Button changes to orange]
                 │
                 └─► RequestToSendMyAudioAsync()
                     └─ Send via Hub

14:00:01                                    OnTheyWantToReceiveMyAudio
                                            │
                                            └─ Dialog appears:
                                               "Agent wants to send audio"
                                               [Accept] [Reject]

14:00:02                                    User clicks [Accept]
                                            │
                                            └─► RespondToTheirReceive
                                                MyAudioAsync(true)

14:00:03
  OnMyAudioSendResponseReceived
  (allowed: true)
  │
  ├─ Set state: Streaming
  └─► StartAudioCapture()
      ├─ Create service
      ├─ Start recording

14:00:04 - 14:00:30
  [Continuous capture loop]
  │
  ├─► OnLocalAudioCaptured()
  │   └─ SendAudioData()
  │       └─ _audioChannel.send() every ~20ms
  │
  └─ Total: ~1300 audio packets

                                        14:00:04 - 14:00:30
                                        [Continuous receive loop]
                                        │
                                        ├─► OnAudioDataReceived()
                                        │   ├─ StartAudioPlayback() [once]
                                        │   └─ AddSamples()
                                        │
                                        └─ Audio played through speakers

14:00:31
  User clicks stop
  │
  ├─ ButtonMyMic state: Streaming
  │
  └─► StopSendingMyAudioAsync()
      ├─ audioCaptureService.StopAsync()
      └─► SendStopMyAudioAsync()
          └─ Send via Hub

14:00:32                                OnStopAudioReceived
                                        │
                                        ├─ Stop playback
                                        ├─ Dispose service
                                        └─ State: Stopped

✅ 28 seconds of audio transmitted successfully
```

### 10.2 Webcam Call Scenario

```
TIMELINE:        AGENT                        CONTROLLER
─────────────────────────────────────────────────────

15:00:00
  User clicks:   "Send My Webcam"
                 │
                 └─► RequestToSendMyWebcamAsync()
                     └─ Send via Hub

15:00:01                                    OnTheyWantToReceiveMyWebcam
                                            │
                                            └─ Dialog appears:
                                               "Agent wants to send webcam"
                                               [Accept] [Reject]

15:00:02                                    User clicks [Accept]
                                            │
                                            └─► RespondToTheirReceive
                                                MyWebcamAsync(true)

15:00:03
  OnMyWebcamSendResponseReceived
  (allowed: true)
  │
  ├─ Set state: Streaming
  └─► StartWebcamCapture()
      ├─ VideoCaptureService (15 FPS)
      └─ Subscribe to OnFrameCaptured

15:00:04 - 15:00:30
  [Continuous capture @ 15 FPS]
  │
  ├─ OnLocalWebcamFrameCaptured()
  │  ├─ ~67ms interval
  │  └─ SendWebcamData()
  │      └─ _webcamChannel.send() every ~67ms
  │
  └─ Total: ~27 frames (1500ms / 67ms)

                                        15:00:04 - 15:00:30
                                        [Continuous receive]
                                        │
                                        ├─► OnWebcamDataReceived()
                                        │   ├─ ShowWebcamViewer() [once]
                                        │   │  └─ Create window
                                        │   │
                                        │   └─ UpdateFrame() every ~67ms
                                        │      └─ Render JPEG
                                        │
                                        └─ Video displayed in real-time

15:00:31
  User clicks stop
  │
  └─► StopSendingMyWebcamAsync()
      ├─ videoCaptureService.StopAsync()
      ├─ Also: audioCaptureService.StopAsync()
      └─► SendStopMyWebcamAsync()
          └─ Send via Hub

15:00:32                                OnStopWebcamReceived
                                        │
                                        ├─ Set state: Stopped
                                        ├─ CloseWebcamViewer()
                                        │  └─ Close window
                                        │
                                        └─ Clean up services

✅ 27 frames of video transmitted successfully
   ~2.1 MB data transferred (27 × ~80KB)
```

## Summary Table

### All Key Classes and Methods

| Component    | Class                                 | Key Methods                                                                                            | Purpose                             |
|--------------|---------------------------------------|--------------------------------------------------------------------------------------------------------|-------------------------------------|
| **UI**       | `MainForm.AudioVideo.cs`              | `ButtonMyMic_Click()`, `ButtonMyCam_Click()`, `UpdateButtonState()`, `AttachAudioVideoSessionEvents()` | User interface for A/V control      |
| **Service**  | `SessionCoordinator.AudioVideo.cs`    | `RequestToSendMyAudioAsync()`, `StartAudioCapture()`, `StartWebcamCapture()`, `ShowWebcamViewer()`     | Coordination & lifecycle management |
| **Capture**  | `AudioCaptureService`                 | `Start()`, `StopAsync()`, event `OnAudioCaptured`                                                      | Microphone recording                |
| **Playback** | `AudioPlaybackService`                | `Start()`, `StopAsync()`, `AddSamples()`, property `IsMuted`                                           | Speaker playback                    |
| **Capture**  | `VideoCaptureService`                 | `Start()`, `StopAsync()`, event `OnFrameCaptured`                                                      | Webcam recording                    |
| **Viewer**   | `WebcamViewerForm`                    | `UpdateFrame()`, `Show()`, `Close()`, event `OnViewerClosed`                                           | Video display window                |
| **State**    | `AudioVideoStateManager`              | `SetLocalAudioState()`, `SetRemoteWebcamState()`, properties `LocalAudioState`, etc.                   | State tracking                      |
| **Network**  | `PeerConnectionManager.AudioVideo.cs` | `SendAudioData()`, `SendWebcamData()`, `SetupAudioChannel()`, `SetupWebcamChannel()`                   | WebRTC transmission                 |

## Appendix: Glossary

| Term                | Definition                                       |
|---------------------|--------------------------------------------------|
| **PCM**             | Pulse Code Modulation (raw uncompressed audio)   |
| **JPEG**            | Lossy image compression (used for webcam frames) |
| **RTC**             | Real-Time Communication (WebRTC)                 |
| **STA Thread**      | Single-Threaded Apartment (for UI operations)    |
| **Channel ID**      | Unique identifier for WebRTC data channel        |
| **FPS**             | Frames Per Second (video frame rate)             |
| **Ticks**           | .NET DateTime ticks (100 nanosecond intervals)   |
| **Sequence Number** | Packet ordering identifier                       |
| **Timestamp**       | Temporal marker for synchronization              |
