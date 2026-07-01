# Poshtibano Desk File Transfer & Clipboard Sharing Architecture

> **Language:** English  
> **Focus:** Architecture, Flows, and Diagrams  

## Quick Navigation

| Topic | Overview |
|--------------------------------------------------------|---------------------------------|
| [System Architecture](#1-system-architecture)          | 3-layer architecture overview   |
| [File Transfer](#2-file-transfer-architecture)         | How files are sent and received |
| [Clipboard Sharing](#3-clipboard-sharing-architecture) | Text and file sharing mechanism |
| [Data Flow](#4-complete-data-flows)                    | Visual flow diagrams            |
| [Packet Types](#5-packet-types--structures)            | All packet formats              |
| [State Machines](#6-state-machines)                    | Transfer states                 |
| [Echo Prevention](#7-echo-prevention-strategy)         | Anti-loop mechanisms            |
| [Error Handling](#8-error-handling-recovery)           | Fault tolerance                 |
| [Scenarios](#9-end-to-end-scenarios)                   | Real-world examples             |

## 1. System Architecture

### 1.1 Three-Layer Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                    PRESENTATION LAYER (UI)                   │
│                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐          │
│  │   MainForm.          │  │   File Offer         │          │
│  │   Clipboard.cs       │  │   Notification       │          │
│  │                      │  │   Dialog             │          │
│  │ • File offers        │  │ • Accept/Reject      │          │
│  │ • Status messages    │  │ • Progress display   │          │
│  │ • Error alerts       │  │ • Transfer status    │          │
│  └──────────────────────┘  └──────────────────────┘          │
│                                                              │
└────────────────────────────┬─────────────────────────────────┘
                             │
                   UI Events & Callbacks
                             │
┌────────────────────────────▼─────────────────────────────────┐
│                                                              │
│                   COORDINATION LAYER                         │
│                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐          │
│  │ SessionCoordinator.  │  │ SessionCoordinator.  │          │
│  │ Clipboard.cs         │  │ cs (main)            │          │
│  │                      │  │                      │          │
│  │ • Permission logic   │  │ • Session lifecycle  │          │
│  │ • Event routing      │  │ • Service management │          │
│  │ • State tracking     │  │ • Error handling     │          │
│  └──────────────────────┘  └──────────────────────┘          │
│                                                              │
└────────────────────────────┬─────────────────────────────────┘
                             │
              Serialized Packets & Tasks
                             │
┌────────────────────────────▼─────────────────────────────────┐
│                                                              │
│                    SERVICE LAYER                             │
│                                                              │
│  ┌─────────────────────┐  ┌──────────────────────┐           │
│  │ FileTransfer        │  │ ClipboardSharing     │           │
│  │ Manager             │  │ Manager              │           │
│  │                     │  │                      │           │
│  │ • Send files        │  │ • Monitor clipboard  │           │
│  │ • Receive files     │  │ • Text sharing       │           │
│  │ • Progress tracking │  │ • File offers        │           │
│  │ • Sanitization      │  │ • Echo prevention    │           │
│  └─────────────────────┘  └──────────────────────┘           │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │           PacketHandler                                 │ │
│  │  • Packet routing  • Fragment reassembly                │ │
│  │  • Type dispatch   • Progress reporting                 │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
└────────────────────────────┬─────────────────────────────────┘
                             │
                 WebRTC Channels & SignalR
                             │
┌────────────────────────────▼─────────────────────────────────┐
│                                                              │
│              NETWORK / CONNECTION LAYER                      │
│                                                              │
│  ┌─────────────────────┐  ┌──────────────────────┐           │
│  │ PeerConnectionMgr   │  │ WebRTC Channels      │           │
│  │ (P2P Core)          │  │                      │           │
│  │                     │  │ • Channel id=2       │           │
│  │ • RTCDataChannels   │  │   (Control/Chat)     │           │
│  │ • Data routing      │  │ • Channel id=3       │           │
│  │ • Connection mgmt   │  │   (Bulk/Files)       │           │
│  └─────────────────────┘  └──────────────────────┘           │
│                                                              │
└────────────────────────────┬─────────────────────────────────┘
                             │
                    ═════════════════════
                    REMOTE PEER (Agent/Controller)
                    ═════════════════════
```

### 1.2 Communication Channels Summary

| Channel ID | Type    | Purpose                           | Reliability              |
|------------|---------|-----------------------------------|--------------------------|
| **2**      | Control | Chat messages, Clipboard metadata | Ordered, Retransmit      |
| **3**      | Bulk    | File transfer chunks              | Unordered, No Retransmit |
| **Hub**    | SignalR | Clipboard file offers/requests    | Ordered, Reliable        |

## 2. File Transfer Architecture

### 2.1 File Transfer Overview

```
┌─────────────────────────────────────────────────────────────┐
│                 FILE TRANSFER LIFECYCLE                     │
│                                                             │
│  Phase 1: PREPARATION                                       │
│  ─────────────────────                                      │
│  • Collect file paths (files & directories)                 │
│  • Calculate total size & chunk count                       │
│  • Generate unique transfer ID (UUID)                       │
│                                                             │
│  Phase 2: INITIATION                                        │
│  ──────────────────                                         │
│  • Send FileStart packet (metadata)                         │
│  • Fire OnTransferStarted event                             │
│                                                             │
│  Phase 3: DATA TRANSFER                                     │
│  ──────────────────────                                     │
│  • Read file in 8KB chunks                                  │
│  • Send FileChunk packets (multiple)                        │
│  • Report progress (OnProgressUpdated)                      │
│  • Wait 25ms between chunks (network throttle)              │
│                                                             │
│  Phase 4: COMPLETION                                        │
│  ──────────────────                                         │
│  • Send FileEnd packet                                      │
│  • Fire OnTransferCompleted event                           │
│  • Wait 10ms before next file                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 File Transfer Configuration

| Parameter                | Default  | Purpose                         |
|--------------------------|----------|---------------------------------|
| **MaxFileChunkSize**     | 8 KB     | Maximum bytes per chunk packet  |
| **DelayBetweenChunks**   | 25 ms    | Throttle between chunk sends    |
| **DelayBetweenFiles**    | 10 ms    | Throttle between files in batch |
| **TempDirectory**        | `./temp` | Storage for received files      |
| **UsePrefixForFileName** | false    | Add transferId to filename      |

### 2.3 Sender Side: File Transmission

```
┌────────────────────────────────────────────────────┐
│ SENDER: File Transmission Process                  │
├────────────────────────────────────────────────────┤
│                                                    │
│  START: User initiates SendPathsAsync()            │
│    │                                               │
│    ├─► Collect all paths (files & dirs)            │
│    ├─► For each directory: enumerate recursively   │
│    ├─► Build relative paths (preserve structure)   │
│    │                                               │
│    └─► For each file:                              │
│        │                                           │
│        ├─► Generate TransferId (UUID)              │
│        │                                           │
│        ├─► SEND: FileStart packet                  │
│        │   └─ FileName, TransferId, TotalSize      │
│        │                                           │
│        ├─► READ: File from disk in 8KB chunks      │
│        │   │                                       │
│        │   ├─► SEND: FileChunk[0]                  │
│        │   ├─► Wait 25ms                           │
│        │   ├─► SEND: FileChunk[1]                  │
│        │   ├─► Wait 25ms                           │
│        │   └─► ... repeat until EOF                │
│        │                                           │
│        ├─► SEND: FileEnd packet                    │
│        │   └─ FinalSize, ChunkCount                │
│        │                                           │
│        └─► Wait 10ms before next file              │
│                                                    │
│  END: All files transferred                        │
│                                                    │
└────────────────────────────────────────────────────┘
```

### 2.4 Receiver Side: File Reception

```
┌──────────────────────────────────────────────────────┐
│ RECEIVER: File Reception Process                     │
├──────────────────────────────────────────────────────┤
│                                                      │
│  EVENT: OnFileDataReceived (WebRTC packet)           │
│    │                                                 │
│    ├─► Deserialize Packet                            │
│    │                                                 │
│    └─► Switch on PacketType                          │
│        │                                             │
│        ├─► FileStart:                                │
│        │   ├─ Sanitize filename                      │
│        │   ├─ Create temp directory structure        │
│        │   ├─ Open FileStream (write mode)           │
│        │   ├─ Store transfer state                   │
│        │   └─ Fire OnTransferStarted event           │
│        │                                             │
│        ├─► FileChunk (multiple):                     │
│        │   ├─ fs.Seek(chunkIndex * 8KB)              │
│        │   ├─ fs.Write(data)                         │
│        │   ├─ fs.Flush()                             │
│        │   ├─ Update progress tracking               │
│        │   └─ Fire OnProgressUpdated event           │
│        │                                             │
│        └─► FileEnd:                                  │
│            ├─ fs.Dispose()                           │
│            ├─ Rename temp file → final file          │
│            ├─ Fire OnFileReceived(path) event        │
│            └─ Fire OnTransferCompleted event         │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### 2.5 Directory Structure Preservation

```
SENDER SIDE:                    RECEIVER SIDE:
───────────────                 ──────────────

C:\MyFolder\                    ./temp/
├── File1.pdf                   ├── File1.pdf
├── SubDir\                     ├── SubDir\
│   ├── File2.jpg               │   ├── File2.jpg
│   └── File3.txt               │   └── File3.txt
└── File4.docx                  └── File4.docx

ROOT ITEMS (for clipboard paste):
• ./temp/File1.pdf
• ./temp/SubDir              (folder as single item)
• ./temp/File4.docx
```

### 2.6 File Transfer State Diagram

```
SENDER SIDE:                        RECEIVER SIDE:

  ┌──────────┐                          ┌──────────┐
  │   Idle   │                          │   Idle   │
  └─────┬────┘                          └─────┬────┘
        │                                     │
User calls                            FileStart received
SendPathsAsync()                              │
        │                                     ▼
        ▼                              ┌──────────────┐
  ┌──────────────┐                     │  Receiving   │
  │ Transferring │◄────────────────────│   (Stream    │
  │              │   FileChunk[n]      │   opened)    │
  ├──────────────┤                     └──────────────┘
  │ • FileStart  │                            │
  │ • FileChunk  │◄───────────────────────────┤
  │ • Progress   │   FileChunk[0..n]          │
  └──────────────┘                            │
        │                                     │
        │                             FileEnd received
        │                                     │
        ▼                                     ▼
  ┌──────────────┐                    ┌──────────────┐
  │   Complete   │                    │   Complete   │
  │              │                    │              │
  │ FileEnd sent │                    │ File renamed │
  │ Transfer OK  │                    │ Transfer OK  │
  └──────────────┘                    └──────────────┘

CANCELLATION PATH:
    │
    ├─ User cancels → CancellationToken signals
    │
    ├─ FileCancel packet sent to remote
    │
    └─ Temp files deleted on both sides
```

## 3. Clipboard Sharing Architecture

### 3.1 Clipboard Overview

```
┌───────────────────────────────────────────────────┐
│        CLIPBOARD SHARING COMPONENTS               │
│                                                   │
│  LOCAL SIDE                                       │
│  ──────────                                       │
│  ┌─────────────────┐                              │
│  │ ClipboardMonitor│◄─── Monitors WM_CLIPBOARD    │
│  │ (Win32 Native)  │     UPDATE Windows message   │
│  └────────┬────────┘                              │
│           │                                       │
│           ▼                                       │
│  ┌─────────────────────────┐                      │
│  │ ClipboardSharingManager │                      │
│  ├─────────────────────────┤                      │
│  │ • Echo prevention       │                      │
│  │ • Hash tracking         │                      │
│  │ • Suppress counter      │                      │
│  │ • File offer management │                      │
│  └────────┬────────────────┘                      │
│           │                                       │
│           ▼                                       │
│  ┌─────────────────────────┐                      │
│  │ PeerConnectionManager   │                      │
│  │ (WebRTC + SignalR)      │                      │
│  └─────────────────────────┘                      │
│                                                   │
└───────────────────────────────────────────────────┘
```

### 3.2 Clipboard Sharing Types

| Type                 | Direction     | Mechanism                | Data Volume            |
|----------------------|---------------|--------------------------|------------------------|
| **Text**             | Bidirectional | Direct send              | Small (usually <100KB) |
| **Files (Offer)**    | Bidirectional | Negotiated request-grant | Metadata only          |
| **Files (Transfer)** | Bidirectional | Bulk channel (id=3)      | Large (can be GB)      |

### 3.3 Text Sharing Flow

```
┌──────────────────────────────────────────────────┐
│          TEXT CLIPBOARD SHARING                  │
├──────────────────────────────────────────────────┤
│                                                  │
│  SENDER:                    RECEIVER:            │
│  ──────────                 ────────             │
│                                                  │
│  User copies text           (waiting)            │
│       │                                          │
│       ├─► ClipboardMonitor                       │
│       │   detects change                         │
│       │                                          │
│       ├─► HandleLocalChange()                    │
│       │   ├─ Read clipboard (STA thread)         │
│       │   ├─ Calculate hash                      │
│       │   ├─ Check echo prevention               │
│       │   └─ If OK: SendClipboardText()          │
│       │                                          │
│       └─► Create ClipboardData {                 │
│           type: "text",                          │
│           TextContent: "...",                    │
│           Timestamp: ...                         │
│       }                                          │
│       │                                          │
│       └─► Serialize to JSON                      │
│           └─► Create Packet(ClipboardText)       │
│               └─► _peer.SendChat()               │
│                   (WebRTC Channel id=2)          │
│                                                  │
│                    Network (P2P)                 │
│                         ▼                        │
│                                                  │
│                              OnChatDataReceived  │
│                              └─► Parse JSON      │
│                                  └─► Update      │
│                                      suppress    │
│                                      counter     │
│                                      └─►         │
│                                  Clipboard.      │
│                                  SetText()       │
│                                  (STA thread)    │
│                                      │           │
│                                      ▼           │
│                                   [Text now      │
│                                    in local      │
│                                  clipboard]      │
│                                                  │
│  User sees text when they paste (Ctrl+V)         │
│                                                  │
└──────────────────────────────────────────────────┘
```

### 3.4 File Offer Negotiation

```
┌────────────────────────────────────────────────────────┐
│        FILE OFFER NEGOTIATION (3-Step)                 │
├────────────────────────────────────────────────────────┤
│                                                        │
│  STEP 1: OFFER PHASE                                   │
│  ──────────────────                                    │
│                                                        │
│  SENDER                          RECEIVER              │
│  ──────                          ────────              │
│  User copies files                                     │
│       │                                                │
│       ├─► HandleLocalChange()                          │
│       │   └─ Detect file drop list                     │
│       │                                                │
│       ├─► SendClipboardFileOffer()                     │
│       │   ├─ Generate TransferId (UUID)                │
│       │   ├─ Calculate file sizes                      │
│       │   ├─ Store in _pendingOffer                    │
│       │   │  (5-min validity)                          │
│       │   └─ Create ClipboardData {                    │
│       │       type: "files",                           │
│       │       FileNames: [...],                        │
│       │       FileSizes: [...],                        │
│       │       TransferId: "...",                       │
│       │       TotalSize: ...                           │
│       │   }                                            │
│       │                                                │
│       └─► Send via WebRTC Channel id=2                 │
│                                                        │
│                    Network                             │
│                         ▼                              │
│                                                        │
│                              OnChatDataReceived        │
│                              └─► Parse offer           │
│                                  └─► Fire              │
│                                  OnRemoteOffer         │
│                                  └─► MainForm          │
│                                      shows             │
│                                      dialog            │
│                                      "3 files          │
│                                       (50MB)"          │
│                                      [Accept]          │
│                                      [Reject]          │
│                                                        │
│  STEP 2: APPROVAL PHASE                                │
│  ──────────────────────                                │
│                                                        │
│  (User clicks Accept on dialog)                        │
│                                                        │
│                              AcceptFileOffer(id)       │
│                              └─► Create                │
│                                  FileRequest           │
│                                  packet                │
│                                  └─► Send via          │
│                                      Channel id=2      │
│                                                        │
│                    Network                             │
│                         ▼                              │
│                                                        │
│  HandleRemoteRequest()                                 │
│  └─► HandleFileRequestFromRemote()                     │
│      ├─ Validate TransferId                            │
│      ├─ Check 5-min expiry                             │
│      └─ Start file transfer                            │
│                                                        │
│  STEP 3: TRANSFER PHASE                                │
│  ──────────────────────                                │
│                                                        │
│  SendPathsAsync()                                      │
│  └─► Send via WebRTC                                   │
│       Channel id=3 (Bulk)                              │
│       ├─ FileStart                                     │
│       ├─ FileChunk[0..n]                               │
│       └─ FileEnd                                       │
│                                                        │
│                    Network                             │
│                         ▼                              │
│                                                        │
│                              OnFileReceived()          │
│                              └─► Files arrive          │
│                                  in temp/              │
│                                  └─►                   │
│                                  SetReceived           │
│                                  FilesInClip           │
│                                  board()               │
│                                  └─►                   │
│                                  Clipboard.            │
│                                  SetFile               │
│                                  DropList()            │
│                                  [Files ready          │
│                                   for Ctrl+V]          │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### 3.5 Clipboard File Offer State Machine

```
         ┌─────────────┐
         │   No Offer  │
         └──────┬──────┘
                │
      User copies files
                │
                ▼
    ┌─────────────────────┐
    │  Offer Generated    │
    │                     │
    │ TransferId stored   │
    │ 5-min timer started │
    └──────┬──────┬───────┘
           │      │
    [After │      │ [User accepts on
     5 min]│      │  dialog]
           │      │
           ▼      ▼
    ┌──────────┐  ┌──────────────┐
    │ Expired  │  │   Approved   │
    │ Rejected │  │              │
    │ Denied   │  │ Files start  │
    └──────────┘  │ transferring │
                  └──────┬───────┘
                         │
                    [Receiving files]
                         │
                         ▼
                  ┌────────────────┐
                  │  Complete      │
                  │                │
                  │ Files in temp  │
                  │ Ready to       │
                  │ paste (Ctrl+V) │
                  └────────────────┘
```

## 4. Complete Data Flows

### 4.1 Text Sharing Data Flow

```
SENDER                              RECEIVER
┌──────────────────────┐            ┌──────────────────────┐
│  Local Clipboard     │            │  Local Clipboard     │
│  "Hello World"       │            │  (Empty)             │
└──────────────────────┘            └──────────────────────┘
         │                                    ▲
         │ User copies (Ctrl+C)               │
         │                                    │
         ▼                                    │
┌──────────────────────┐                      │
│ ClipboardMonitor     │                      │
│ WM_CLIPBOARD_UPDATE  │                      │
└──────────────────────┘                      │
         │                                    │
         ▼                                    │
┌──────────────────────┐                      │
│ HandleLocalChange()  │                      │
│ • Read text          │                      │
│ • Hash text          │                      │
│ • Check echo prevent │                      │
└──────────────────────┘                      │
         │                                    │
         ▼                                    │
┌──────────────────────┐                      │
│ SendClipboardText()  │                      │
│ Create JSON payload  │                      │
│ Serialize Packet     │                      │
└──────────────────────┘                      │
         │                                    │
         ▼                                    │
┌──────────────────────┐                      │
│ _peer.SendChat()     │                      │
│ WebRTC Channel id=2  │                      │
└──────────────────────┘                      │
         │                                    │
         ├─► Binary packet                    │
         │                                    │
         ├─────────────────────────────────►  │
         │        P2P Network                 │
         │                                    │
         │                                    ▼
         │                            ┌──────────────────────┐
         │                            │ OnChatDataReceived() │
         │                            │ • Deserialize JSON   │
         │                            │ • Validate sender    │
         │                            └──────────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────────┐
         │                            │ HandleRemoteText()   │
         │                            │ • Increment suppress │
         │                            │ • Update hash        │
         │                            └──────────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────────┐
         │                            │ SetText (STA Thread) │
         │                            │ Clipboard.SetText()  │
         │                            └──────────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────────┐
         │                            │ Local Clipboard      │
         │                            │ "Hello World"        │
         │                            └──────────────────────┘
         │                                    │
         │                                    ▼
         │                            User pastes (Ctrl+V)
         │                            Text appears in app
         │
         └─ Paste event suppressed
            (echo prevention active)
```

### 4.2 File Offer Data Flow

```
SENDER                                  RECEIVER
┌──────────────────┐                   ┌──────────────────┐
│ Files in         │                   │ Waiting for      │
│ Clipboard:       │                   │ clipboard data   │
│ • Doc.pdf (10MB) │                   │                  │
│ • Image.zip (30M)│                   │                  │
│ • Config.txt (10)│                   │                  │
└──────────────────┘                   └──────────────────┘
         │                                    ▲
         │                                    │
         ▼                                    │
┌──────────────────────────┐                  │
│ ClipboardMonitor detects │                  │
│ file drop list change    │                  │
└──────────────────────────┘                  │
         │                                    │
         ▼                                    │
┌──────────────────────────┐                  │
│ SendClipboardFileOffer() │                  │
│ • Gen TransferId         │                  │
│ • Calc sizes: 50MB total │                  │
│ • Store _pendingOffer    │                  │
│ • 5-min timer            │                  │
└──────────────────────────┘                  │
         │                                    │
         ▼                                    │
┌──────────────────────────┐                  │
│ ClipboardData (JSON):    │                  │
│ {                        │                  │
│   type: "files",         │                  │
│   FileNames: [...],      │                  │
│   TransferId: "abc123",  │                  │
│   TotalSize: 52428800    │                  │
│ }                        │                  │
└──────────────────────────┘                  │
         │                                    │
         ▼                                    │
┌──────────────────────────┐                  │
│ Send via Channel id=2    │                  │
│ (Control/Chat channel)   │                  │
└──────────────────────────┘                  │
         │                                    │
         ├─► JSON Packet                      │
         │                                    │
         ├───────────────────────────────────►│
         │        P2P Network                 │
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ OnChatReceived() │
         │                            │ Parse JSON       │
         │                            └──────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ Show Dialog:     │
         │                            │ "3 files         │
         │                            │  offered (50MB)" │
         │                            │                  │
         │                            │ [Accept] [Reject]│
         │                            └──────────────────┘
         │                                    │
         │                                    │ [User clicks Accept]
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ AcceptFileOffer()│
         │                            │ Create           │
         │                            │ FileRequest      │
         │                            └──────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ Send via Ch. id=2│
         │                            │ "file_request"   │
         │                            │ payload          │
         │                            └──────────────────┘
         │                                    │
         │                                    ▼
         │◄───────────────────────────────────│
         │  FileRequest packet received       │
         │                                    │
         ▼                                    │
┌──────────────────────────┐                  │
│ HandleFileRequest()      │                  │
│ • Validate TransferId    │                  │
│ • Check 5-min expiry     │                  │
│ • Start SendPathsAsync() │                  │
└──────────────────────────┘                  │
         │                                    │
         ▼                                    │
┌──────────────────────────┐                  │
│ Send files via Channel3: │                  │
│ • FileStart              │                  │
│ • FileChunk[0..125]      │                  │
│ • FileEnd                │                  │
│ (+ more files)           │                  │
└──────────────────────────┘                  │
         │                                    │
         ├─► Binary chunks                    │
         │                                    │
         ├───────────────────────────────────►│
         │   WebRTC Channel id=3              │
         │   (Bulk transfer)                  │
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ OnFileReceived() │
         │                            │ × 3 times        │
         │                            │ (one per file)   │
         │                            └──────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ SetReceivedFiles │
         │                            │ InClipboard()    │
         │                            │ Clipboard.       │
         │                            │ SetFileDropList()│
         │                            └──────────────────┘
         │                                    │
         │                                    ▼
         │                            ┌──────────────────┐
         │                            │ Files ready in   │
         │                            │ clipboard for    │
         │                            │ Paste (Ctrl+V)   │
         │                            └──────────────────┘
         │
         └─► Files deleted from
             sender's pending offer
```

## 5. Packet Types & Structures

### 5.1 File Transfer Packet Summary

| Packet Type    | Direction       | Payload                                      | Size          |
|----------------|-----------------|----------------------------------------------|---------------|
| **FileStart**  | Sender→Receiver | Filename, TransferId, TotalSize, TotalChunks | ~200 bytes    |
| **FileChunk**  | Sender→Receiver | Data chunk (up to 8KB)                       | ~8KB + header |
| **FileEnd**    | Sender→Receiver | FinalSize, ChunkCount                        | ~100 bytes    |
| **FileCancel** | Bidirectional   | TransferId, Reason                           | ~100 bytes    |

### 5.2 Clipboard Packet Summary

| Packet Type              | Direction     | Payload Format                                        | Size       |
|--------------------------|---------------|-------------------------------------------------------|------------|
| **ClipboardText**        | Bidirectional | JSON: type, TextContent, Timestamp                    | Variable   |
| **ClipboardFiles**       | Bidirectional | JSON: FileNames[], FileSizes[], TransferId, TotalSize | Variable   |
| **ClipboardFileRequest** | Bidirectional | JSON: type, TransferId                                | ~100 bytes |

### 5.3 FileStart Packet Structure

```
┌──────────────────────────────────────────────────────────────┐
│      FileStart Packet                                        │
├──────────────────────────────────────────────────────────────┤
│ Field              │ Type   │ Value                          │
├────────────────────┼────────┼────────────────────────────────┤
│ PacketType         │ byte   │ FileStart                      │
│ FileName           │ string │ "path\to\file.txt"             │
│ TransferId         │ string │ UUID (32 chars)                │
│ TotalSize          │ long   │ File size in bytes             │
│ TotalChunks        │ int    │ Count of chunks                │
│ ChunkIndex         │ int    │ 0 (always for Start)           │
│ Data               │ byte[] │ Empty                          │
│ SenderRole         │ byte   │ Agent (1) or Controller (2)    │
│ TimestampTicks     │ long   │ DateTime.UtcNow.Ticks          │
└──────────────────────────────────────────────────────────────┘
```

### 5.4 FileChunk Packet Structure

```
┌───────────────────────────────────────────────────────────────────┐
│      FileChunk Packet                                             │
├───────────────────────────────────────────────────────────────────┤
│ Field              │ Type   │ Value                               │
├────────────────────┼────────┼─────────────────────────────────────┤
│ PacketType         │ byte   │ FileChunk                           │
│ FileName           │ string │ "path\to\file.txt" (same as Start)  │
│ TransferId         │ string │ UUID (same as Start)                │
│ TotalSize          │ long   │ File size (same as Start)           │
│ TotalChunks        │ int    │ (same as Start)                     │
│ ChunkIndex         │ int    │ 0, 1, 2, ... (incrementing)         │
│ Data               │ byte[] │ File bytes (up to 8KB)              │
│ SenderRole         │ byte   │ Agent or Controller                 │
│ TimestampTicks     │ long   │ DateTime.UtcNow.Ticks               │
└───────────────────────────────────────────────────────────────────┘
```

### 5.5 ClipboardData JSON Examples

#### Example 1: Text Packet
```json
{
  "type": "text",
  "TextContent": "Hello World",
  "Timestamp": 637857483920000000
}
```

#### Example 2: File Offer Packet
```json
{
  "type": "files",
  "FileNames": ["Document.pdf", "Images.zip", "Config.txt"],
  "FileSizes": [10485760, 31457280, 10485760],
  "TransferId": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "TotalSize": 52428800,
  "Timestamp": 637857483920000000
}
```

#### Example 3: File Request Packet
```json
{
  "type": "file_request",
  "TransferId": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
  "Timestamp": 637857483920000000
}
```

## 6. State Machines

### 6.1 File Transfer State Machine

```
SENDER:
┌────────────┐
│   IDLE     │
└─────┬──────┘
      │ User initiates send
      ▼
┌────────────────────────┐
│   PREPARING            │
│ • Collect files        │
│ • Calculate sizes      │
│ • Generate TransferId  │
└─────┬──────────────────┘
      │
      ▼
┌────────────────────────┐
│  TRANSFERRING          │
│ • Send FileStart       │
│ • Send FileChunk[0..N] │
│ • Progress: 0% → 100%  │
│ • Send FileEnd         │
└─────┬──────┬───────────┘
      │      │
   OK │      │ User cancels
      │      │
      ▼      ▼
┌──────────┐ ┌────────────┐
│COMPLETE  │ │CANCELLED   │
└──────────┘ └────────────┘

RECEIVER:
┌────────────┐
│   IDLE     │
└─────┬──────┘
      │ FileStart received
      ▼
┌────────────────────────┐
│  RECEIVING             │
│ • Stream opened        │
│ • FileChunk[0..N]      │
│ • Progress: 0% → 100%  │
└─────┬──────┬───────────┘
      │      │
   OK │      │ Remote cancels
      │      │
      ▼      ▼
┌──────────┐ ┌────────────┐
│COMPLETE  │ │CANCELLED   │
│ • Rename │ │ • Cleanup  │
│ • Ready  │ │ • Deleted  │
└──────────┘ └────────────┘
```

### 6.2 Clipboard Text State Machine

```
LOCAL CLIPBOARD:
┌─────────────────┐
│ Idle            │
│ (User viewing)  │
└────────┬────────┘
         │
    User copies
         │
         ▼
┌─────────────────┐
│ Content changed │
└────────┬────────┘
         │
    Echo prevention
    checks:
    • Hash match?
    • Suppress count?
    • Debounce (300ms)?
         │
         ├─ YES? Skip
         │
         └─ NO? Send
              │
              ▼
         ┌─────────────────┐
         │ Sent to remote  │
         └─────────────────┘

REMOTE CLIPBOARD:
         ┌─────────────────┐
         │ Idle            │
         │ (User viewing)  │
         └────────┬────────┘
                  │
         Receive packet
                  │
                  ▼
         ┌─────────────────┐
         │ Parse & validate│
         │ Update suppress │
         │ Update hash     │
         └────────┬────────┘
                  │
                  ▼
         ┌─────────────────┐
         │ SetText() on    │
         │ STA thread      │
         └────────┬────────┘
                  │
                  ▼
         ┌─────────────────┐
         │ Content changed │
         │ (Ready for      │
         │  Ctrl+V paste)  │
         └─────────────────┘
```

### 6.3 Clipboard File Offer State Machine

```
SENDER FILE OFFER:

┌──────────────┐
│ No Offer     │
└────────┬─────┘
         │ User copies files
         ▼
┌──────────────┐
│ Offer Gen    │
│ • TransferId │
│ • 5-min      │
│   timer      │
└────┬─────┬──┘
     │     │
  [5 min] [Request from remote]
  expires  │
     │     ▼
     │  ┌──────────────┐
     │  │ Transfer     │
     │  │ Started      │
     │  └────┬─────────┘
     │       │
     │       ▼
     │  ┌──────────────┐
     │  │ Complete     │
     │  └──────────────┘
     │
     ▼
┌──────────────┐
│ Expired      │
│ (Rejected/   │
│  Deleted)    │
└──────────────┘

RECEIVER DIALOG:

         ┌────────────────┐
         │ Offer received │
         │ Show dialog    │
         └────┬────────┬──┘
              │        │
         [Accept]  [Reject]
              │        │
              ▼        ▼
         ┌────┐    ┌────────┐
         │YES │    │   NO   │
         └─┬──┘    └────────┘
           │
           ▼
      ┌───────────┐
      │ Request   │
      │ files     │
      └─────┬─────┘
            │
            ▼
      ┌───────────┐
      │ Receiving │
      │ files     │
      └─────┬─────┘
            │
            ▼
      ┌───────────┐
      │ Complete  │
      │ Ready to  │
      │ paste     │
      └───────────┘
```

## 7. Echo Prevention Strategy

### 7.1 Four-Layer Echo Prevention

```
┌────────────────────────────────────────────────────┐
│   ECHO PREVENTION MECHANISM                        │
├────────────────────────────────────────────────────┤
│                                                    │
│  LAYER 1: SENDER ROLE CHECK                        │
│  ───────────────────────────                       │
│  • Ignore packets from ourselves                   │
│  • Check: packet.SenderRole == _localRole          │
│  • If match: SKIP (echo prevention)                │
│                                                    │
│  LAYER 2: HASH-BASED TRACKING                      │
│  ──────────────────────────────                    │
│  Sent side:                                        │
│  • Calculate: hash = clipboard.GetHashCode()       │
│  • Store: _lastSentTextHash                        │
│  • Next time: if same hash → skip                  │
│                                                    │
│  Received side:                                    │
│  • Calculate: hash = incomingText.GetHashCode()    │
│  • Store: _lastRemoteTextHash                      │
│  • When local change: if hash matches remote       │
│    → this was just set by remote → skip            │
│                                                    │
│  LAYER 3: SUPPRESS COUNTER                         │
│  ──────────────────────────                        │
│  • SetFileDropList() triggers 1-2 extra            │
│    WM_CLIPBOARDUPDATE messages                     │
│  • Counter logic:                                  │
│    - When setting remote: _suppressCount += 2      │
│    - On each local change: if count > 0:           │
│      decrement and skip this change                │
│                                                    │
│  LAYER 4: DEBOUNCE TIMER (300ms)                   │
│  ─────────────────────────────                     │
│  • Only process clipboard changes >300ms apart     │
│  • Prevents rapid-fire spurious events             │
│  • Compare: now - _lastClipboardChangeTime         │
│  • If < 300ms: skip                                │
│                                                    │
└────────────────────────────────────────────────────┘
```

### 7.2 Echo Scenario Prevention

```
SCENARIO: User copies text on Sender

Sender Side:
──────────
1. User copies "Hello"
2. Local clipboard = "Hello"
3. ClipboardMonitor fires
4. ✅ Echo prevention OFF (local change)
5. SendClipboardText("Hello")
6. Hash stored: "Hello".hashCode() = 12345

                    Network ►

Receiver Side:
──────────────
1. OnChatDataReceived()
2. Parse "Hello"
3. ✅ LAYER 1: Check sender role
   • packet.SenderRole = Sender
   • _localRole = Receiver
   • Different → PASS
4. ✅ LAYER 2: Check hash
   • Calculate: "Hello".hashCode() = 12345
   • _lastRemoteTextHash = ?
   • No match yet → PASS
5. ✅ LAYER 3: Suppress counter
   • _suppressCount += 2
   • (Now = 2)
6. ✅ LAYER 4: Debounce
   • First change → PASS
7. SetText("Hello") on STA thread
8. Local clipboard = "Hello"
9. WM_CLIPBOARDUPDATE fires
10. _suppressCount = 2 (active)
11. ❌ SKIP (suppressed)
12. _suppressCount = 1
13. WM_CLIPBOARDUPDATE fires again
14. _suppressCount = 1 (still active)
15. ❌ SKIP (suppressed)
16. _suppressCount = 0

User pastes:
└─ "Hello" appears (NO ECHO!)
```

## 8. Error Handling & Recovery

### 8.1 File Transfer Error Handling

| Error                   | Trigger                       | Handling                                 | Result             |
|-------------------------|-------------------------------|------------------------------------------|--------------------|
| **Invalid Filename**    | Received PacketType.FileStart | SanitizeRelativePath() removes bad chars | Safe file created  |
| **Directory Traversal** | Path contains `..`            | Strip `..\\` sequences                   | Contained in temp/ |
| **Disk Full**           | FileStream.Write() throws     | IOException caught, logged               | Transfer stops     |
| **File Locked**         | File in use by OS             | Retry logic or skip                      | Transfer continues |
| **Partial Transfer**    | Receiver disconnect           | Temp file remains in temp/               | Auto-cleanup       |
| **Chunk Out-of-Order**  | Network reorders              | Seek-based write handles it              | Correct position   |
| **Cancellation**        | User cancels transfer         | CancellationToken signals                | Temp files deleted |

### 8.2 Clipboard Error Handling

| Error                | Trigger            | Handling               | Result                |
|----------------------|--------------------|------------------------|-----------------------|
| **STA Thread Fail**  | SetText() throws   | Try/catch, log error   | Fallback to UI thread |
| **Clipboard Locked** | Multiple access    | Retry with timeout     | Eventual success      |
| **Invalid JSON**     | Parse error        | JsonSerializer throws  | Ignored silently      |
| **Offer Expired**    | 5+ minutes         | Check CreatedAt        | Offer rejected        |
| **Invalid Path**     | File doesn't exist | SanitizeRelativePath() | Stored as-is          |

### 8.3 Recovery Strategies

```
┌────────────────────────────────────────────────┐
│       ERROR RECOVERY STRATEGIES                │
├────────────────────────────────────────────────┤
│                                                │
│  FILE TRANSFER:                                │
│  ──────────────                                │
│  ┌─ Partial file received                      │
│  │  └─► Remain in temp/ for manual recovery    │
│  │                                             │
│  ├─ Transfer cancelled                         │
│  │  └─► Delete temp file immediately           │
│  │                                             │
│  ├─ Disk full                                  │
│  │  └─► Stop transfer, user retries            │
│  │                                             │
│  └─ Out of order chunks                        │
│     └─► Seek-based writes handle it            │
│                                                │
│  CLIPBOARD:                                    │
│  ──────────                                    │
│  ┌─ SetText() fails                            │
│  │  └─► Log error, attempt UI thread retry     │
│  │                                             │
│  ├─ Invalid JSON                               │
│  │  └─► Silently ignore packet                 │
│  │                                             │
│  ├─ File offer expired                         │
│  │  └─► User retries copy                      │
│  │                                             │
│  └─ Echo loop detected                         │
│     └─► Suppress counter stops it              │
│                                                │
└────────────────────────────────────────────────┘
```

## 9. End-to-End Scenarios

### 9.1 Text Sharing Example

```
SCENARIO: Agent copies "Important Meeting at 3PM"

TIMELINE:                           ACTION:
────────────────────────────────────────────────

Agent: 14:30:15
  User copies text                  Ctrl+C

  ClipboardMonitor triggers         WM_CLIPBOARDUPDATE

  Echo check: first time            PASS

  Read clipboard: "Important..."    STA thread

  Calculate hash                    hash = 1234567

  SendClipboardText()               Create JSON packet

                                    ═══ NETWORK ═══

Controller: 14:30:16               (1 second delay)

  OnChatDataReceived()              Binary packet arrives

  Echo check: sender ≠ receiver     PASS

  Parse JSON                        Extract text

  Hash check: not in _lastRemoteHash  PASS

  Suppress counter += 2             (Now = 2)

  SetText() on STA thread           Clipboard.SetText()

  WM_CLIPBOARDUPDATE fires          Suppress counter = 1
                                    SKIP

  WM_CLIPBOARDUPDATE fires again    Suppress counter = 0
                                    SKIP

  Fire OnRemoteTextReceived         UI event

User: 14:30:17
  Pastes text                       Ctrl+V

  "Important Meeting at 3PM"        ✅ Appears correctly
  appears in app
```

### 9.2 File Transfer Example

```
SCENARIO: Agent sends 3 large files (total 75MB)

TIMELINE:

Agent: 15:45:00
  Files copied: ✓ Selected 3 items
                 • Report.pdf (25MB)
                 • Archive.zip (40MB)
                 • ReadMe.txt (10MB)

Agent: 15:45:02
  User clicks Send               SessionCoordinator.SendFilesAsync()

  Prepare files:
  ├─ Report.pdf
  │  • TransferId: abc123def456
  │  • Chunks: 3,125 (25MB ÷ 8KB)
  │
  ├─ Archive.zip
  │  • TransferId: ghi789jkl012
  │  • Chunks: 5,000 (40MB ÷ 8KB)
  │
  └─ ReadMe.txt
     • TransferId: mno345pqr678
     • Chunks: 1,250 (10MB ÷ 8KB)

Agent: 15:45:02
  Send FileStart(Report.pdf)      TransferId: abc123...

                                  ═══ NETWORK ═══

Controller: 15:45:02
  FileStart received              Create temp\Report.pdf.part
                                  Open FileStream

Agent: 15:45:02 → 15:45:47
  Send FileChunk[0..3125]          • Wait 25ms between chunks
                                  • Progress: 0% → 100%
                                  • Takes ~45 seconds

                                  ═══ NETWORK ═══

Controller: 15:45:02 → 15:45:47
  FileChunk received              Write to disk (random-access)
                                  Report: 0% → 100%

Agent: 15:45:47
  Send FileEnd(Report.pdf)        Complete first file

                                  ═══ NETWORK ═══

Controller: 15:45:47
  FileEnd received                Rename: .part → final
                                  Fire OnFileReceived()

Agent: 15:45:47
  Wait 10ms between files         Throttle

Agent: 15:45:48
  Send FileStart(Archive.zip)     TransferId: ghi789...

[Same process repeats for Archive.zip (takes ~75 seconds)]

[Same process repeats for ReadMe.txt (takes ~20 seconds)]

Agent: 16:01:24
  All files sent ✅              Total time: ~15-16 minutes

Controller: 16:01:24
  All files received ✅          Files in temp/ directory:
                                 ├─ Report.pdf
                                 ├─ Archive.zip
                                 └─ ReadMe.txt
```

## 10. Performance & Optimization

### 10.1 Throughput Analysis

| Parameter                | Value     | Impact                               |
|--------------------------|-----------|--------------------------------------|
| **Chunk Size**           | 8 KB      | Balance between overhead and latency |
| **Delay Between Chunks** | 25 ms     | Prevents network saturation          |
| **Delay Between Files**  | 10 ms     | Allows other operations              |
| **Typical Speed**        | ~256 KB/s | ~16 MB/min (with throttling)         |
| **No Throttle**          | ~2-5 MB/s | Limited by bandwidth & codec         |

### 10.2 Concurrent Operations

```
WHILE file transfer is happening, SIMULTANEOUSLY:

├─ Audio streaming (Channel id=4)
├─ Webcam streaming (Channel id=5)
├─ Control input (Channel id=2, type=0x01)
├─ Chat messages (Channel id=2, type=0x02)
├─ Clipboard text sharing (Channel id=2)
└─ Multiple file transfers (multiplexed)

Note: WebRTC multiplexes on same connection,
but SFU/codec may limit total throughput to
network capacity (~1-10 Mbps typical)
```

## 11. Summary Comparison

### 11.1 Text vs File Transfer

| Aspect              | Text           | File                  |
|---------------------|----------------|-----------------------|
| **Channel**         | id=2 (Control) | id=3 (Bulk)           |
| **Packet Type**     | ClipboardText  | FileStart/Chunk/End   |
| **Size Limit**      | ~100 KB        | GB+                   |
| **Latency**         | <1 second      | Minutes (large files) |
| **Retry**           | Not needed     | Implicit (chunks)     |
| **Echo Prevention** | 4-layer        | File offers expire    |

### 11.2 Architecture Layers

| Layer             | Component          | Responsibility            |
|-------------------|--------------------|---------------------------|
| **UI**            | MainForm           | Display, user interaction |
| **Coordination**  | SessionCoordinator | Event routing, lifecycle  |
| **Service**       | Managers           | Business logic            |
| **Routing**       | PacketHandler      | Packet dispatch           |
| **Network**       | PeerConnectionMgr  | WebRTC management         |

---

## Key Takeaways

### ✅ File Transfer
- ✓ Preserves directory structure
- ✓ Handles random-access writes
- ✓ Supports cancellation
- ✓ Progress reporting
- ✓ Sanitization against attacks

### ✅ Clipboard Sharing
- ✓ Bidirectional (text & files)
- ✓ Negotiated file offers
- ✓ Multi-layer echo prevention
- ✓ 5-minute offer expiry
- ✓ STA thread safety

### ✅ Integration
- ✓ Separate concerns (services)
- ✓ Event-driven architecture
- ✓ Thread-safe operations
- ✓ Graceful error handling
- ✓ Scalable design


## Appendix: Glossary

| Term                   | Definition                                                |
|------------------------|-----------------------------------------------------------|
| **TransferId**         | Unique UUID for each file transfer                        |
| **STA Thread**         | Single-Threaded Apartment (required for Clipboard access) |
| **Echo Prevention**    | Mechanism to avoid infinite clipboard loops               |
| **Suppress Counter**   | Counter to ignore spurious clipboard events               |
| **File Offer**         | Metadata-only notification of available files             |
| **Relative Path**      | Path relative to parent dir (preserves structure)         |
| **Negotiated Channel** | WebRTC channel with agreed-upon ID on both sides          |
| **Seek-based Write**   | Random access file writing at specific offsets            |
