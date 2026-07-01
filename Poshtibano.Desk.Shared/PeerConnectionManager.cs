using Newtonsoft.Json;
using Poshtibano.Common;
using Poshtibano.Desk.Services.Connection;
using SIPSorcery.Net;

namespace Poshtibano.Desk.Shared
{
    public partial class PeerConnectionManager : IDisposable
    {
        private ClientRole _role;
        private readonly string _sessionId;
        private readonly ConnectionStateManager _stateManager;
        private readonly HubConnectionService _hubService;

        private RTCPeerConnection _peerConnection;

        // ============================================================
        // All use negotiated=true for proper ID assignment
        // ============================================================
        private RTCDataChannel _frameChannel;    // id=1: Screen frames
        private RTCDataChannel _controlChannel;  // id=2: Events + Chat
        private RTCDataChannel _bulkChannel;     // id=3: File transfers

        private List<RTCIceCandidate> _localCandidates = new List<RTCIceCandidate>();
        private List<RTCIceCandidate> _remoteCandidates = new List<RTCIceCandidate>();
        private List<RTCIceCandidateInit> _pendingCandidates = new List<RTCIceCandidateInit>();

        private readonly object _pcLock = new object();
        private bool _remoteDescriptionSet = false;
        private TaskCompletionSource<bool> _iceGatheringCompleteTcs;

        // ============================================================
        // DATA CHANNEL EVENTS
        // ============================================================
        public event Action<byte[]> OnFrameDataReceived;
        public event Action<byte[]> OnEventDataReceived;
        public event Action<byte[]> OnChatDataReceived;
        public event Action<byte[]> OnFileDataReceived;
        public event Action OnSessionReady;

        // ============================================================
        // STATE FLAGS
        // ============================================================
        private bool _isDisposed = false;
        private bool _isConnecting = false;
        private bool _isResetting = false;
        private bool _sessionReadyFired = false;

        public ConnectionStateManager StateManager => _stateManager;

        // ============================================================
        // RATE LIMITING FOR BULK CHANNEL
        // ============================================================
        private readonly SemaphoreSlim _bulkSendLock = new SemaphoreSlim(1, 1);
        private const int BULK_SEND_DELAY_MS = 15;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================
        public PeerConnectionManager(ClientRole role, string sessionId, string signalingUrl)
        {
            _role = role;
            _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            _stateManager = new ConnectionStateManager();
            _hubService = new HubConnectionService(
                signalingUrl,
                role,
                _stateManager);

            SetupHubHandlers();
        }

        // ============================================================
        // HUB HANDLERS SETUP
        // ============================================================
        private void SetupHubHandlers()
        {
            _hubService.OnMessageReceived -= HandleHubMessage;
            _hubService.OnSdpOfferReceived -= HandleSdpOffer;
            _hubService.OnSdpAnswerReceived -= HandleSdpAnswer;
            _hubService.OnIceCandidateReceived -= HandleIceCandidate;
            _hubService.OnPeerDisconnected -= HandlePeerDisconnected;

            _hubService.OnRequestPasswordInfo -= HandleRequestPasswordInfo;
            _hubService.OnRequestPassword -= HandleRequestPassword;
            _hubService.OnPasswordIncorrect -= HandlePasswordIncorrect;
            _hubService.OnPasswordCorrect -= HandlePasswordCorrect;
            _hubService.OnRequestAccessPermission -= HandleRequestAccessPermission;
            _hubService.OnAccessDenied -= HandleAccessDenied;
            _hubService.OnSessionEnded -= HandleSessionEnded;

            _hubService.OnMessageReceived += HandleHubMessage;
            _hubService.OnSdpOfferReceived += HandleSdpOffer;
            _hubService.OnSdpAnswerReceived += HandleSdpAnswer;
            _hubService.OnIceCandidateReceived += HandleIceCandidate;
            _hubService.OnPeerDisconnected += HandlePeerDisconnected;

            _hubService.OnRequestPasswordInfo += HandleRequestPasswordInfo;
            _hubService.OnRequestPassword += HandleRequestPassword;
            _hubService.OnPasswordIncorrect += HandlePasswordIncorrect;
            _hubService.OnPasswordCorrect += HandlePasswordCorrect;
            _hubService.OnRequestAccessPermission += HandleRequestAccessPermission;
            _hubService.OnAccessDenied += HandleAccessDenied;
            _hubService.OnSessionEnded += HandleSessionEnded;

            _hubService.OnVerifyPassword -= HandleVerifyPassword;
            _hubService.OnVerifyPassword += HandleVerifyPassword;

            _hubService.OnChaneRoleRequest -= HandleChaneRoleRequest;
            _hubService.OnChaneRoleRequest += HandleChaneRoleRequest;
        }

        // ============================================================
        // CONNECTION MANAGEMENT
        // ============================================================
        public async Task ConnectAsync(string callerName, string callerSessionId)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PeerConnectionManager));

            if (_isConnecting)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Already connecting, ignoring duplicate request");
                return;
            }

            _isConnecting = true;

            try
            {
                _sessionReadyFired = false;
                await _hubService.ConnectAsync(_sessionId, callerName, callerSessionId);
                Console.WriteLine($"[{DateTime.Now}] ✅ Hub connected, waiting for handshake..");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Connection error: {ex.Message}");
                _stateManager.UpdatePeerState(ConnectionStatus.Failed, ex.Message);
                throw;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task CreatePeerConnectionAsync()
        {
            lock (_pcLock)
            {
                CreatePeerConnectionInternal();
            }
        }

        private List<RTCIceServer> GetIceServers()
        {
            return new List<RTCIceServer>
            {
                //new RTCIceServer { urls = "stun:stun.l.google.com:19302" },
                //new RTCIceServer { urls = "stun:stun1.l.google.com:19302" },
                //new RTCIceServer { urls = "stun:stun2.l.google.com:19302" },
                //new RTCIceServer { urls = "stun:stun3.l.google.com:19302" },
                //new RTCIceServer { urls = "stun:stun4.l.google.com:19302" },
                
                new RTCIceServer { urls = "stun:217.114.40.239:3478" },
                new RTCIceServer
                {
                    urls = "turn:217.114.40.239:3478?transport=udp",
                    username = "test",
                    credential = "test"
                },
                //new RTCIceServer
                //{
                //    urls = "turn:217.114.40.239:3478?transport=tcp",
                //    username = "test",
                //    credential = "test"
                //},
                //new RTCIceServer
                //{
                //    urls = "turn:openrelay.metered.ca:80",
                //    username = "openrelayproject",
                //    credential = "openrelayproject"
                //},
                //new RTCIceServer
                //{
                //    urls = "turn:openrelay.metered.ca:443",
                //    username = "openrelayproject",
                //    credential = "openrelayproject"
                //},
            };
        }

        // ============================================================
        // DATA CHANNELS CREATION - FIXED ARCHITECTURE
        // All channels use negotiated=true
        // ============================================================
        private void CreateDataChannels()
        {
            // ────────────────────────────────────────────────────────
            // CHANNEL 1: Frames (Screen sharing)
            // ────────────────────────────────────────────────────────
            var frameInit = new RTCDataChannelInit
            {
                ordered = false,
                maxPacketLifeTime = 500,
                //maxRetransmits = 0,
                negotiated = true,
                id = 1
            };
            _frameChannel = _peerConnection.createDataChannel("frames", frameInit).Result;
            SetupChannelHandlers(_frameChannel, "frames", data => OnFrameDataReceived?.Invoke(data));

            // ────────────────────────────────────────────────────────
            // CHANNEL 2: Control (Events + Chat)
            // ───────────────────────────────────────────────────────s─
            var controlInit = new RTCDataChannelInit
            {
                ordered = true,
                maxPacketLifeTime = 5000,
                maxRetransmits = 5,
                negotiated = true,
                id = 2
            };
            _controlChannel = _peerConnection.createDataChannel("control", controlInit).Result;
            SetupControlChannel();

            // ────────────────────────────────────────────────────────
            // CHANNEL 3: Bulk (File transfers)
            // ────────────────────────────────────────────────────────
            var bulkInit = new RTCDataChannelInit
            {
                ordered = true,
                maxRetransmits = 15,
                negotiated = true,
                id = 3
            };
            _bulkChannel = _peerConnection.createDataChannel("bulk", bulkInit).Result;
            SetupChannelHandlers(_bulkChannel, "bulk", data => OnFileDataReceived?.Invoke(data));

            SetupAudioChannel();

            SetupWebcamChannel();

            Console.WriteLine($"[{DateTime.Now}] ✅ All 5 data channels created");
        }

        // ============================================================
        // CHANNEL SETUP METHODS
        // ============================================================
        private void SetupChannelHandlers(RTCDataChannel channel, string channelName, Action<byte[]> onMessage)
        {
            if (channel == null) return;

            channel.onopen += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ✅ [{channelName}] Channel OPEN");
            };

            channel.onclose += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [{channelName}] Channel CLOSED");
            };

            channel.onerror += (error) =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [{channelName}] Channel ERROR: {error}");
            };

            channel.onmessage += (label, protocol, data) =>
            {
                if (data != null && data.Length > 0)
                {
                    onMessage?.Invoke(data);
                }
            };
        }

        private void SetupControlChannel()
        {
            _controlChannel.onopen += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ✅ [control] Channel OPEN");
            };

            _controlChannel.onclose += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [control] Channel CLOSED");
            };

            _controlChannel.onerror += (error) =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [control] Channel ERROR: {error}");
            };

            _controlChannel.onmessage += (label, protocol, data) =>
            {
                if (data == null || data.Length < 2) return;

                // First byte determines type: 0x01=Event, 0x02=Chat
                byte msgType = data[0];
                byte[] payload = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, payload, 0, payload.Length);

                switch (msgType)
                {
                    case 0x01: // Event
                        OnEventDataReceived?.Invoke(payload);
                        break;
                    case 0x02: // Chat
                        OnChatDataReceived?.Invoke(payload);
                        break;
                    default:
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ Unknown control message type: {msgType}");
                        break;
                }
            };
        }

        // ============================================================
        // PUBLIC SEND METHODS
        // ============================================================

        public void SendFrame(byte[] data)
        {
            if (_frameChannel?.readyState != RTCDataChannelState.open)
                return;

            try
            {
                _frameChannel.send(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Frame send error: {ex.Message}");
            }
        }

        public void SendEvent(byte[] data)
        {
            SendOnControlChannel(0x01, data);
        }

        public void SendChat(byte[] data)
        {
            SendOnControlChannel(0x02, data);
        }

        private void SendOnControlChannel(byte msgType, byte[] data)
        {
            if (_controlChannel?.readyState != RTCDataChannelState.open)
                return;

            var pool = System.Buffers.ArrayPool<byte>.Shared;
            var packet = pool.Rent(data.Length + 1);
            try
            {
                packet[0] = msgType;
                Buffer.BlockCopy(data, 0, packet, 1, data.Length);

                // SIPSorcery send expects exact-size array
                var exact = new byte[data.Length + 1];
                Buffer.BlockCopy(packet, 0, exact, 0, data.Length + 1);
                _controlChannel.send(exact);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Control send error: {ex.Message}");
            }
            finally
            {
                pool.Return(packet);
            }
        }

        public void SendFile(byte[] data)
        {
            _ = SendFileInternalAsync(data);
        }

        private async Task SendFileInternalAsync(byte[] data)
        {
            try
            {
                await SendFileAsync(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ SendFile background error: {ex.Message}");
            }
        }

        private async Task SendFileAsync(byte[] data)
        {
            if (_bulkChannel?.readyState != RTCDataChannelState.open)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Bulk channel not ready");
                return;
            }

            await _bulkSendLock.WaitAsync();
            try
            {
                _bulkChannel.send(data);
                await Task.Delay(BULK_SEND_DELAY_MS);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Bulk send error: {ex.Message}");
            }
            finally
            {
                _bulkSendLock.Release();
            }
        }

        // ============================================================
        // PEER CONNECTION HANDLERS
        // ============================================================
        private void AttachPeerConnectionHandlers()
        {
            _peerConnection.onicecandidate += OnIceCandidate;
            _peerConnection.onconnectionstatechange += OnConnectionStateChange;
            _peerConnection.onicegatheringstatechange += OnIceGatheringStateChange;
            _peerConnection.oniceconnectionstatechange += OnIceConnectionStateChange;

            _peerConnection.ondatachannel += (channel) =>
            {
                Console.WriteLine($"[{DateTime.Now}] 📨 Received channel: {channel.label} (negotiated channels don't fire this)");
            };
        }

        private async void OnIceCandidate(RTCIceCandidate candidate)
        {
            if (candidate != null)
            {
                lock (_pcLock)
                {
                    _localCandidates.Add(candidate);
                }
                var init = new RTCIceCandidateInit
                {
                    candidate = candidate.ToString(),
                    sdpMid = candidate.sdpMid,
                    sdpMLineIndex = candidate.sdpMLineIndex
                };

                try
                {
                    if (_hubService.IsConnected)
                    {
                        await _hubService.SendIceCandidateAsync(_sessionId, JsonConvert.SerializeObject(init));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Error sending ICE candidate: {ex.Message}");
                }
            }
        }

        private async void OnConnectionStateChange(RTCPeerConnectionState state)
        {
            var status = state switch
            {
                RTCPeerConnectionState.connected => ConnectionStatus.Connected,
                RTCPeerConnectionState.connecting => ConnectionStatus.Connecting,
                RTCPeerConnectionState.disconnected => ConnectionStatus.Disconnected,
                RTCPeerConnectionState.failed => ConnectionStatus.Failed,
                RTCPeerConnectionState.closed => ConnectionStatus.Disconnected,
                _ => ConnectionStatus.Disconnected
            };

            _stateManager.UpdatePeerState(status, $"Peer state: {state}");

            if (state == RTCPeerConnectionState.failed && _role == ClientRole.Agent)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Agent peer failed, waiting for sessionReady to reconnect");
                return;
            }

            if (state == RTCPeerConnectionState.connected)
            {
                await Task.Delay(2000);
                ShowConnectionInfo();
            }

            Console.WriteLine($"[{DateTime.Now}] PeerConnection state: {state}");
        }

        private void OnIceGatheringStateChange(RTCIceGatheringState state)
        {
            if (state == RTCIceGatheringState.complete)
            {
                _iceGatheringCompleteTcs?.TrySetResult(true);
                Console.WriteLine($"[{DateTime.Now}] ICE gathering complete. Candidates: {_localCandidates.Count}");
            }
        }

        private void OnIceConnectionStateChange(RTCIceConnectionState state)
        {
            Console.WriteLine($"[{DateTime.Now}] ICE connection state: {state}");

            if (state == RTCIceConnectionState.failed)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ ICE CONNECTION FAILED!");
            }
        }

        private void CreatePeerConnectionInternal()
        {
            ClosePeerConnection();
            _remoteDescriptionSet = false;
            _iceGatheringCompleteTcs = new TaskCompletionSource<bool>();

            var config = new RTCConfiguration
            {
                iceServers = GetIceServers(),
                iceTransportPolicy = RTCIceTransportPolicy.all,
                bundlePolicy = RTCBundlePolicy.max_bundle,
                rtcpMuxPolicy = RTCRtcpMuxPolicy.require,
            };

            _peerConnection = new RTCPeerConnection(config);
            CreateDataChannels();
            AttachPeerConnectionHandlers();

            Console.WriteLine($"[{DateTime.Now}] ✅ PeerConnection created on-demand");
        }

        // ============================================================
        // SDP HANDLING
        // ============================================================
        private async void HandleSdpOffer(string offerJson)
        {
            Console.WriteLine($"[{DateTime.Now}] Received SDP Offer");

            try
            {
                lock (_pcLock)
                {
                    if (_peerConnection == null)
                    {
                        CreatePeerConnectionInternal();
                    }
                }

                var offer = JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(offerJson);

                lock (_pcLock)
                {
                    if (_role != ClientRole.Controller)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ERROR: Non-Controller received offer!");
                        return;
                    }

                    _peerConnection.setRemoteDescription(offer);
                    _remoteDescriptionSet = true;

                    FlushPendingCandidates();
                }

                var answer = _peerConnection.createAnswer();
                await _peerConnection.setLocalDescription(answer);
                await _hubService.SendSdpAnswerAsync(_sessionId, JsonConvert.SerializeObject(answer));

                Console.WriteLine($"[{DateTime.Now}] Answer sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error handling offer: {ex.Message}");
            }
        }

        private void HandleSdpAnswer(string answerJson)
        {
            Console.WriteLine($"[{DateTime.Now}] Received SDP Answer");

            try
            {
                var answer = JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(answerJson);
                _peerConnection.setRemoteDescription(answer);

                lock (_pcLock)
                {
                    _remoteDescriptionSet = true;
                    FlushPendingCandidates();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error handling answer: {ex.Message}");
            }
        }

        private void HandleIceCandidate(string candidateJson)
        {
            try
            {
                var candidateInit = JsonConvert.DeserializeObject<RTCIceCandidateInit>(candidateJson);

                lock (_pcLock)
                {
                    if (!_remoteDescriptionSet)
                    {
                        _pendingCandidates.Add(candidateInit);
                    }
                    else
                    {
                        _peerConnection.addIceCandidate(candidateInit);
                        _remoteCandidates.Add(new RTCIceCandidate(candidateInit));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error handling ICE candidate: {ex.Message}");
            }
        }

        private void FlushPendingCandidates()
        {
            if (_pendingCandidates.Count > 0)
            {
                Console.WriteLine($"[{DateTime.Now}] Flushing {_pendingCandidates.Count} buffered candidates");

                foreach (var candidate in _pendingCandidates)
                {
                    try
                    {
                        _peerConnection.addIceCandidate(candidate);
                        _remoteCandidates.Add(new RTCIceCandidate(candidate));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Error adding buffered candidate: {ex.Message}");
                    }
                }

                _pendingCandidates.Clear();
            }
        }

        private void HandlePeerDisconnected()
        {
            Console.WriteLine($"[{DateTime.Now}] 📢 Peer disconnected event received");
            _stateManager.UpdatePeerState(ConnectionStatus.Disconnected, "Peer disconnected");

            if (_role == ClientRole.Agent && !_isDisposed)
            {
                Console.WriteLine($"[{DateTime.Now}] ℹ️ Agent: Peer disconnected, staying ready for reconnection");
            }
        }

        // ============================================================
        // OFFER CREATION
        // ============================================================
        public async Task CreateAndSendOfferAsync()
        {
            if (_role != ClientRole.Agent)
            {
                Console.WriteLine($"[{DateTime.Now}] ERROR: Only Agent should send offers!");
                return;
            }

            if (_peerConnection == null)
            {
                Console.WriteLine($"[{DateTime.Now}] PeerConnection is null! Recreating...");
                await CreatePeerConnectionAsync();
            }

            var offer = _peerConnection.createOffer(null);
            await _peerConnection.setLocalDescription(offer);

            try
            {
                var iceTask = _iceGatheringCompleteTcs.Task;
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(iceTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine($"[{DateTime.Now}] ICE gathering timeout, sending offer anyway");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error waiting for ICE: {ex.Message}");
            }

            await _hubService.SendSdpOfferAsync(_sessionId, JsonConvert.SerializeObject(offer));
            Console.WriteLine($"[{DateTime.Now}] Offer sent. Local candidates: {_localCandidates.Count}");
        }

        // ============================================================
        // CONNECTION RESET
        // ============================================================
        private async Task ResetPeerConnectionAsync()
        {
            if (_isResetting)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Already resetting, ignoring");
                return;
            }

            _isResetting = true;

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🔄 Resetting peer connection...");

                lock (_pcLock)
                {
                    ClosePeerConnection();
                    _remoteDescriptionSet = false;
                }

                _stateManager.UpdatePeerState(ConnectionStatus.Disconnected, "Peer connection reset");

                await Task.Delay(1000);

                await CreatePeerConnectionAsync();

                Console.WriteLine($"[{DateTime.Now}] ✅ New PeerConnection ready");
            }
            finally
            {
                _isResetting = false;
            }
        }

        // ============================================================
        // CLOSE PEER CONNECTION
        // ============================================================
        private void ClosePeerConnection()
        {
            if (_peerConnection != null)
            {
                try
                {
                    _peerConnection.onicecandidate -= OnIceCandidate;
                    _peerConnection.onconnectionstatechange -= OnConnectionStateChange;
                    _peerConnection.onicegatheringstatechange -= OnIceGatheringStateChange;
                    _peerConnection.oniceconnectionstatechange -= OnIceConnectionStateChange;

                    _frameChannel?.close();
                    _controlChannel?.close();
                    _bulkChannel?.close();
                    _audioChannel?.close();
                    _webcamChannel?.close();

                    _peerConnection.close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Error closing peer connection: {ex.Message}");
                }
                finally
                {
                    _peerConnection = null;
                }
            }

            _frameChannel = null;
            _controlChannel = null;
            _bulkChannel = null;
            _audioChannel = null;
            _webcamChannel = null;

            _localCandidates.Clear();
            _remoteCandidates.Clear();
            _pendingCandidates.Clear();
        }

        // ============================================================
        // CONNECTION INFO
        // ============================================================
        private void ShowConnectionInfo()
        {
            if (_localCandidates.Count == 0 || _remoteCandidates.Count == 0)
                return;

            var bestLocal = _localCandidates
                .OrderBy(c => c.type switch
                {
                    RTCIceCandidateType.srflx => 1,
                    RTCIceCandidateType.host => 2,
                    RTCIceCandidateType.relay => 3,
                    _ => 4
                })
                .FirstOrDefault();

            var bestRemote = _remoteCandidates
                .OrderBy(c => c.type switch
                {
                    RTCIceCandidateType.srflx => 1,
                    RTCIceCandidateType.prflx => 2,
                    RTCIceCandidateType.relay => 3,
                    _ => 4
                })
                .FirstOrDefault();

            if (bestLocal != null && bestRemote != null)
            {
                Console.WriteLine($"[{DateTime.Now}] Connection Info:");
                Console.WriteLine($"=========================================================");
                Console.WriteLine($"\tLocal :  {_localCandidates.Count}");
                ShowIceCandidateInfo(bestLocal);
                Console.WriteLine($"---------------------------------------------------------");
                Console.WriteLine($"\tRemote :  {_remoteCandidates.Count}");
                ShowIceCandidateInfo(bestRemote);
                Console.WriteLine($"=========================================================");
            }
        }

        private static void ShowIceCandidateInfo(RTCIceCandidate candidate)
        {
            var prefix = "                     ";
            var candidateText = "{" + Environment.NewLine;
            candidateText += $"{prefix}    type:                               {Enum.GetName(typeof(RTCIceCandidateType), candidate.type)}" + Environment.NewLine;
            candidateText += $"{prefix}    address:                           {candidate.address}" + Environment.NewLine;
            candidateText += $"{prefix}    relatedPort:                       {candidate.relatedPort}" + Environment.NewLine;
            candidateText += $"{prefix}    relatedAddress:                    {candidate.relatedAddress}" + Environment.NewLine;
            candidateText += $"{prefix}    candidate:                         {candidate.candidate}" + Environment.NewLine;
            candidateText += $"{prefix}    protocol:                          {candidate.protocol}" + Environment.NewLine;
            candidateText += $"{prefix}    priority:                          {candidate.priority}" + Environment.NewLine;
            if (candidate.IceServer != null)
            {
                candidateText += $"{prefix}    IceServer ----------------------------------" + Environment.NewLine;
                candidateText += $"{prefix}    IceServer.Protocol:                {candidate.IceServer.Protocol}" + Environment.NewLine;
                candidateText += $"{prefix}    IceServer.ServerEndPoint:          {candidate.IceServer.ServerEndPoint}" + Environment.NewLine;
                candidateText += $"{prefix}    IceServer.ServerReflexiveEndPoint:  {candidate.IceServer.ServerReflexiveEndPoint}" + Environment.NewLine;
            }
            candidateText += $"{prefix}" + "}";
            Console.WriteLine(candidateText);
        }

        // ============================================================
        // DISCONNECT METHODS
        // ============================================================
        public async Task DisconnectAsync()
        {
            if (_isDisposed) return;

            Console.WriteLine($"[{DateTime.Now}] 🔌 Full disconnect...");

            _isDisposed = true;
            _sessionReadyFired = false;
            _isResetting = false;
            _isConnecting = false;

            try
            {
                await _hubService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error disconnecting hub: {ex.Message}");
            }

            lock (_pcLock)
            {
                ClosePeerConnection();
            }

            Console.WriteLine($"[{DateTime.Now}] ✅ Full disconnect completed");
        }

        public async Task DisconnectPeerOnlyAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] 🔌 Disconnecting peer only (keeping Hub alive)...");

            lock (_pcLock)
            {
                ClosePeerConnection();
                _remoteDescriptionSet = false;
            }

            _sessionReadyFired = false;
            _isResetting = false;

            _stateManager.UpdatePeerState(ConnectionStatus.Disconnected, "Peer disconnected (Hub alive)");

            Console.WriteLine($"[{DateTime.Now}] ✅ Peer disconnected (Hub still alive)");
        }

        public async Task RecreatePeerConnectionAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] 🔄 Recreating peer connection...");
            await CreatePeerConnectionAsync();
            Console.WriteLine($"[{DateTime.Now}] ✅ Peer connection recreated");
        }

        // ============================================================
        // DISPOSE
        // ============================================================
        public void Dispose()
        {
            if (_isDisposed) return;

            Console.WriteLine($"[{DateTime.Now}] 🗑️ Disposing PeerConnectionManager");

            Task.Run(async () =>
            {
                try { await DisconnectAsync(); }
                catch { }
            }).Wait(3000);

            _bulkSendLock?.Dispose();

            _hubService.OnMessageReceived -= HandleHubMessage;
            _hubService.OnSdpOfferReceived -= HandleSdpOffer;
            _hubService.OnSdpAnswerReceived -= HandleSdpAnswer;
            _hubService.OnIceCandidateReceived -= HandleIceCandidate;
            _hubService.OnPeerDisconnected -= HandlePeerDisconnected;

            _hubService.OnRequestPasswordInfo -= HandleRequestPasswordInfo;
            _hubService.OnRequestPassword -= HandleRequestPassword;
            _hubService.OnPasswordIncorrect -= HandlePasswordIncorrect;
            _hubService.OnRequestAccessPermission -= HandleRequestAccessPermission;
            _hubService.OnAccessDenied -= HandleAccessDenied;
            _hubService.OnSessionEnded -= HandleSessionEnded;

            _hubService?.Dispose();
            _stateManager?.Dispose();

            ClearAudioVideoEvents();

            OnFrameDataReceived = null;
            OnEventDataReceived = null;
            OnChatDataReceived = null;
            OnFileDataReceived = null;
            OnSessionReady = null;
            OnRequestPasswordInfo = null;
            OnRequestPassword = null;
            OnPasswordIncorrect = null;
            OnPasswordCorrect = null;
            OnRequestAccessPermission = null;
            OnAccessDenied = null;
            OnSessionEnded = null;
            OnVerifyPassword = null;
            OnChangeRoleRequest = null;
            OnMonitorListReceived = null;
            OnMonitorStatusUpdateReceived = null;
            OnMonitorChangedReceived = null;
            OnMonitorSelectReceived = null;
            OnRemoteMouseActionDenied = null;
            OnInvitationRequest = null;
            OnInvitationResponse = null;

            Console.WriteLine($"[{DateTime.Now}] ✅ PeerConnectionManager disposed");
        }
    }
}