using Poshtibano.Common;
using Poshtibano.Desk.Services.Capture;
using Poshtibano.Desk.Services.Connection;
using Poshtibano.Desk.Services.Input;
using Poshtibano.Desk.Services.Networking;
using Poshtibano.Desk.Services.Rendering;
using Poshtibano.Desk.Shared;
using Poshtibano.Desk.Shared.Services;
using System.Text;
using System.Text.Json;

namespace Poshtibano.Desk.Services
{
    /// <summary>
    /// Coordinates all services for a remote desktop session
    /// Handles handshake, password verification, and access control
    /// </summary>
    public partial class SessionCoordinator : IDisposable
    {
        private ClientRole _role;
        private readonly string _sessionId;
        private readonly string _signalingUrl;
        private readonly SynchronizationContext _uiContext;

        // Core services
        private PeerConnectionManager _peerManager;
        private PacketHandler _packetHandler;
        private ChatManager _chatManager;
        private FileTransferManager _fileTransferManager;

        // Agent-specific services
        private ScreenCaptureService _captureService;
        private LocalInputHandler _localInputHandler;

        // Controller-specific services
        private FrameRenderService _renderService;
        private RemoteInputService _remoteInputService;

        private bool _isDisposed;
        private bool _isConnected;
        private bool _isInitializing = false;
        private bool _eventsAttached = false;

        // ✅ NEW: Handshake events
        public event Action OnRequestPasswordInfo;
        public event Action<string, string> OnRequestPassword;
        public event Action OnPasswordIncorrect;
        public event Action OnPasswordCorrect;
        public event Action<string, string> OnRequestAccessPermission;
        public event Action OnAccessDenied;
        public event Action<string> OnSessionEnded;
        public event Action<string> OnVerifyPassword;

        public event Action<ClientRole> OnChangeRoleRequest;
        // Existing events
        public event Action<ConnectionStatus> OnHubStateChanged;
        public event Action<ConnectionStatus> OnPeerStateChanged;
        public event Action OnSessionReady;
        public event Action<string> OnAccessRequested;

        public event Action<ChatMessage> OnChatMessage;
        public event Action<ChatEvent> OnChatEvent;
        public event Action<FileTransferProgress> OnFileProgress;
        public event Action<string, string> OnFileReceived;
        public event Action<string> OnError;

        // ✅ MULTI-MONITOR EVENTS
        public event Action<List<MonitorInfo>> OnMonitorListRemoteReceived;
        public event Action<int, bool> OnMonitorStatusUpdateRemoteReceived; // (index, isActive)
        public event Action<int> OnMonitorChangedRemoteReceived;
        public event Action<int> OnMonitorSelectRemoteReceived;

        public event Action<string> OnMouseOnProcess;
        public event Action<string> OnMouseDeniedOnProcess;
        public event Action<string, string> OnRemoteMouseActionDenied;
        public event Action<string, string, string> OnInvitationRequest;
        public event Action<string, string, string, bool> OnInvitationResponse;

        public event Action<string, string> OnTransferCancelled;

        // ✅ MULTI-MONITOR
        private List<MonitorInfo> _localMonitors = new List<MonitorInfo>();
        private List<MonitorInfo> _remoteMonitors = new List<MonitorInfo>();
        private int _currentMonitorIndex = 0;
        private MonitorInfo _monitor;

        public IReadOnlyList<MonitorInfo> LocalMonitors => _localMonitors;
        public IReadOnlyList<MonitorInfo> RemoteMonitors => _remoteMonitors;
        public int CurrentMonitorIndex => _currentMonitorIndex;

        public ConnectionStateManager StateManager => _peerManager?.StateManager;
        public ClientRole Role => _role;
        public bool IsConnected => _isConnected;

        public SessionCoordinator(ClientRole role, string sessionId, string signalingUrl)
        {
            _role = role;
            _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            _signalingUrl = signalingUrl ?? throw new ArgumentNullException(nameof(signalingUrl));
            _uiContext = SynchronizationContext.Current;
        }

        public void ChangeRole(ClientRole role)
        {
            _role = role;
            _peerManager.ChangeRole(role);
        }

        public async Task InitializeAsync(string caller, string sessionId)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🚀 Initializing session as {_role}");

                if (_peerManager != null || _eventsAttached)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Previous session exists, cleaning up first");
                    await CleanupPreviousSession();
                }

                _peerManager = new PeerConnectionManager(_role, _sessionId, _signalingUrl);
                AttachPeerManagerEvents();

                _packetHandler = new PacketHandler();
                AttachPacketHandlerEvents();

                _peerManager.OnFrameDataReceived += OnFrameDataReceivedHandler;
                _peerManager.OnEventDataReceived += OnEventDataReceivedHandler;
                _peerManager.OnChatDataReceived += OnChatDataReceivedHandler;
                _peerManager.OnFileDataReceived += OnFileDataReceivedHandler;

                _chatManager = new ChatManager(_peerManager, _role);
                _chatManager.OnNewMessage += OnChatMessageReceivedHandler;
                _chatManager.OnChatEventReceived += OnChatEventReceivedHandler;

                _fileTransferManager = new FileTransferManager(_peerManager, _role);
                _fileTransferManager.OnProgressUpdated += OnFileProgressUpdatedHandler;
                _fileTransferManager.OnFileReceived += OnFileReceivedHandler;
                _fileTransferManager.OnTransferCancelled += TransferCancelledHandler;

                // ✅ NEW: Initialize Audio/Video services
                InitializeAudioVideoServices();

                // ✅ NEW: Initialize Clipboard sharing services
                InitializeClipboardServices();

                // ✅ NOTE: Connect to Hub only, do NOT create peer connection yet
                await _peerManager.ConnectAsync(caller, sessionId);

                _isConnected = true;
                Console.WriteLine($"[{DateTime.Now}] ✅ Session initialized successfully (waiting for handshake)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Initialization error: {ex.Message}");
                OnError?.Invoke($"Initialization failed: {ex.Message}");

                await CleanupPreviousSession();
                throw;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void TransferCancelledHandler(string transferId, string fileName)
        {
            Console.WriteLine($"[{DateTime.Now}] 🛑 TransferCancelled received:  {transferId}");
            OnTransferCancelled?.Invoke(transferId, fileName);
        }

        public void Initialize(MonitorInfo monitor = null, Control renderTarget = null, Form parentForm = null)
        {
            InitializeAgentServices(monitor);
            InitializeControllerServices(renderTarget, parentForm);

            //if (_role == ClientRole.Agent)
            //{
            //}
            //else if (_role == ClientRole.Controller)
            //{
            //}
        }

        private async Task CleanupPreviousSession()
        {
            Console.WriteLine($"[{DateTime.Now}] 🧹 Cleaning up previous session");

            try
            {
                // ✅ NEW:  Cleanup Audio/Video services
                await CleanupAudioVideoServicesAsync();

                // ✅ NEW: Cleanup clipboard services
                CleanupClipboardServices();

                DetachAllEvents();

                // ✅ NEW: Detach clipboard events
                DetachClipboardEvents();

                if (_captureService != null)
                {
                    await _captureService.StopAsync();
                    _captureService.Dispose();
                    _captureService = null;
                }

                _renderService?.Dispose();
                _renderService = null;

                _remoteInputService?.Dispose();
                _remoteInputService = null;

                _packetHandler?.Dispose();
                _packetHandler = null;

                _chatManager?.Dispose();
                _chatManager = null;


                _fileTransferManager?.Dispose();
                _fileTransferManager = null;

                if (_peerManager != null)
                {
                    await _peerManager.DisconnectAsync();
                    _peerManager.Dispose();
                    _peerManager = null;
                }

                _localInputHandler = null;

                Console.WriteLine($"[{DateTime.Now}] ✅ Previous session cleaned up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error cleaning up previous session: {ex.Message}");
            }
        }

        private void AttachPeerManagerEvents()
        {
            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Cannot attach events: PeerManager is null");
                return;
            }

            if (_eventsAttached)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Events already attached, skipping");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📎 Attaching peer manager events");

            _peerManager.StateManager.HubStateChanged += OnHubStateChangedHandler;
            _peerManager.StateManager.PeerStateChanged += OnPeerStateChangedHandler;
            _peerManager.OnSessionReady += OnSessionReadyHandler;

            // ✅ NEW: Attach handshake events
            _peerManager.OnRequestPasswordInfo += OnRequestPasswordInfoHandler;
            _peerManager.OnRequestPassword += OnRequestPasswordHandler;
            _peerManager.OnPasswordIncorrect += OnPasswordIncorrectHandler;
            _peerManager.OnPasswordCorrect += OnPasswordCorrectHandler;
            _peerManager.OnRequestAccessPermission += OnRequestAccessPermissionHandler;
            _peerManager.OnAccessDenied += OnAccessDeniedHandler;
            _peerManager.OnSessionEnded += OnSessionEndedHandler;

            _peerManager.OnVerifyPassword += OnVerifyPasswordHandler;
            _peerManager.OnChangeRoleRequest += OnChangeRoleRequestHandler;

            // ✅ MULTI-MONITOR events
            _peerManager.OnMonitorListReceived += OnMonitorListReceived;
            _peerManager.OnMonitorStatusUpdateReceived += OnMonitorStatusUpdateReceived;
            _peerManager.OnMonitorChangedReceived += OnMonitorChangedReceived;
            _peerManager.OnMonitorSelectReceived += OnMonitorSelectReceived;

            _peerManager.OnRemoteMouseActionDenied += OnRemoteMouseActionDeniedHandler;
            _peerManager.OnInvitationRequest += OnInvitationRequestHandler;
            _peerManager.OnInvitationResponse += OnInvitationResponseHandler;

            _eventsAttached = true;
            Console.WriteLine($"[{DateTime.Now}] ✅ Peer manager events attached");
        }


        private void DetachPeerManagerEvents()
        {
            if (!_eventsAttached)
            {
                Console.WriteLine($"[{DateTime.Now}] ℹ️ Events not attached, nothing to detach");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📎 Detaching peer manager events");

            if (_peerManager?.StateManager != null)
            {
                _peerManager.StateManager.HubStateChanged -= OnHubStateChangedHandler;
                _peerManager.StateManager.PeerStateChanged -= OnPeerStateChangedHandler;
            }

            if (_peerManager != null)
            {
                _peerManager.OnSessionReady -= OnSessionReadyHandler;

                _peerManager.OnFrameDataReceived -= OnFrameDataReceivedHandler;
                _peerManager.OnEventDataReceived -= OnEventDataReceivedHandler;
                _peerManager.OnChatDataReceived -= OnChatDataReceivedHandler;
                _peerManager.OnFileDataReceived -= OnFileDataReceivedHandler;

                // ✅ NEW: Detach handshake events
                _peerManager.OnRequestPasswordInfo -= OnRequestPasswordInfoHandler;
                _peerManager.OnRequestPassword -= OnRequestPasswordHandler;
                _peerManager.OnPasswordIncorrect -= OnPasswordIncorrectHandler;
                _peerManager.OnRequestAccessPermission -= OnRequestAccessPermissionHandler;
                _peerManager.OnAccessDenied -= OnAccessDeniedHandler;
                _peerManager.OnSessionEnded -= OnSessionEndedHandler;

                _peerManager.OnVerifyPassword -= OnVerifyPasswordHandler;
                _peerManager.OnChangeRoleRequest -= OnChangeRoleRequestHandler;

                _peerManager.OnMonitorListReceived -= OnMonitorListReceived;
                _peerManager.OnMonitorStatusUpdateReceived -= OnMonitorStatusUpdateReceived;
                _peerManager.OnMonitorChangedReceived -= OnMonitorChangedReceived;
                _peerManager.OnMonitorSelectReceived -= OnMonitorSelectReceived;

                _peerManager.OnRemoteMouseActionDenied -= OnRemoteMouseActionDeniedHandler;
                _peerManager.OnInvitationRequest -= OnInvitationRequestHandler;
                _peerManager.OnInvitationResponse -= OnInvitationResponseHandler;
            }

            _eventsAttached = false;
            Console.WriteLine($"[{DateTime.Now}] ✅ Peer manager events detached");
        }

        private void OnInvitationResponseHandler(string inviteeId, string inviterId, string inviteeName, bool accept)
        {
            Console.WriteLine($"[{DateTime.Now}] 🎉 OnInvitationResponse Received");
            OnInvitationResponse?.Invoke(inviteeId, inviterId, inviteeName, accept);
        }

        private void OnInvitationRequestHandler(string inviteeId, string inviterId, string inviterName)
        {
            Console.WriteLine($"[{DateTime.Now}] 🎉 OnInvitationRequest Received");
            OnInvitationRequest?.Invoke(inviteeId, inviterId, inviterName);
        }

        private void OnRemoteMouseActionDeniedHandler(string name, string process)
        {
            Console.WriteLine($"[{DateTime.Now}] 🎉 OnRemoteMouseActionDenied Received");
            OnRemoteMouseActionDenied?.Invoke(name, process);
        }

        public void SetLocalMonitors(List<MonitorInfo> monitors, int activeIndex = 0)
        {
            _localMonitors = monitors ?? new List<MonitorInfo>();
            _currentMonitorIndex = activeIndex >= 0 && activeIndex < _localMonitors.Count ? activeIndex : 0;
            _monitor = _localMonitors[_currentMonitorIndex];

            if (_localInputHandler != null)
                _localInputHandler.UpdateMonitorList(_localMonitors, _currentMonitorIndex);

            Console.WriteLine($"[{DateTime.Now}] 🖥️ Local monitors has been set: {_localMonitors.Count} ");
        }

        private void OnMonitorSelectReceived(int index)
        {
            if (index >= 0 && index < _localMonitors.Count && _localMonitors[index].IsActive)
            {
                SwitchActiveMonitor(index);

                OnMonitorSelectRemoteReceived?.Invoke(index);

                Console.WriteLine($"[{DateTime.Now}] 📥 Monitor selection request: #{index} accepted");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ monitor #{index} is not active");
            }
        }

        private void OnMonitorChangedReceived(int index)
        {
            OnMonitorChangedRemoteReceived?.Invoke(index);
            Console.WriteLine($"[{DateTime.Now}] 📥 Remote monitor changed: #{index}");
        }

        private void OnMonitorStatusUpdateReceived(int index, bool active)
        {
            if (index >= 0 && index < _remoteMonitors.Count)
            {
                _remoteMonitors[index].IsActive = active;
                OnMonitorStatusUpdateRemoteReceived?.Invoke(index, active);
                Console.WriteLine($"[{DateTime.Now}] 📥 monitor status #{index} update: {(active ? "active" : "inactive")}");
            }
        }

        private void OnMonitorListReceived(List<MonitorInfo> monitors)
        {
            _remoteMonitors = monitors;
            OnMonitorListRemoteReceived?.Invoke(monitors);
            Console.WriteLine($"[{DateTime.Now}] 📥 Received list of {monitors.Count} monitors");

        }

        public async Task SendMouseActionOnPorcessDenined(string name, string process)
        {
            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Unable to mouse action denied on process");
                return;
            }

            try
            {
                await _peerManager.SendMouseActionOnProcessDenied(name, process);
                Console.WriteLine($"[{DateTime.Now}] 📤 Send mouse action denied on process {process}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Send mouse action denied on process: {ex.Message}");
            }
        }

        public async Task SendSessionEnd(string reason)
        {
            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Unable to send SessionEnd");
                return;
            }

            try
            {
                await _peerManager.SendSessionEnd(reason);
                Console.WriteLine($"[{DateTime.Now}] 📤 Send SessionEnd: {reason}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error to send SessionEnd: {ex.Message}");
            }
        }

        public async Task SendInvitationResponse(string sessionId, string inviteeId, string inviterId, string inviteeName, bool accept)
        {
            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Unable to send invitation response to inviter");
                return;
            }

            try
            {
                await _peerManager.SendInvitationResponse(sessionId, inviteeId, inviterId, inviteeName, accept);
                var acceptted = accept ? "accept" : "not accept";
                Console.WriteLine($"[{DateTime.Now}] 📤 Send invitation response to inviter {inviteeId} has {acceptted}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error to send invitation response to inviter: {ex.Message}");
            }
        }

        public async Task SendInvitationToRemote(string sessionId, string inviteeId, string inviterId, string inviterName)
        {
            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Unable to send invitation to remote");
                return;
            }

            try
            {
                await _peerManager.SendInvitationToRemote(sessionId, inviteeId, inviterId, inviterName);
                Console.WriteLine($"[{DateTime.Now}] 📤 Send invitation to remote {inviteeId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error to send invitation to remote: {ex.Message}");
            }
        }

        // ✅ Webcam and Microphone availbility
        public async Task SendWebcamAndMicrophoneAvailbilityToRemote(bool weHaveMicrophone, bool weHaveWebcam)
        {
            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Unable to send availbility of microphone and of webcam");
                return;
            }

            try
            {
                await _peerManager.SendWebcamAndMicrophoneAvailability(weHaveMicrophone, weHaveWebcam);
                Console.WriteLine($"[{DateTime.Now}] 📤 Availbility of microphone :{weHaveMicrophone} and of webcam :{weHaveWebcam}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending availbility of microphone and of webcam: {ex.Message}");
            }
        }

        public async Task SendMonitorListToRemote()
        {
            if (_peerManager == null || _localMonitors.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Unable to send list of monitors");
                return;
            }

            try
            {
                await _peerManager.SendMonitorList(_localMonitors);
                Console.WriteLine($"[{DateTime.Now}] 📤 list of {_localMonitors.Count} monitors sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending list of monitors: {ex.Message}");
            }
        }

        public async Task SendMonitorStatusUpdate(int monitorIndex, bool isActive)
        {
            if (_peerManager == null) return;

            try
            {
                await _peerManager.SendMonitorStatusUpdate(monitorIndex, isActive);
                Console.WriteLine($"[{DateTime.Now}] 📤 monitor status #{monitorIndex} change: {(isActive ? "active" : "inactive")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error updating status: {ex.Message}");
            }
        }

        public async Task SendMonitorChanged(int monitorIndex)
        {
            if (_peerManager == null) return;

            try
            {
                _currentMonitorIndex = monitorIndex;
                _monitor = _localMonitors[_currentMonitorIndex];

                await _peerManager.SendMonitorChanged(monitorIndex);
                Console.WriteLine($"[{DateTime.Now}] 📤 active monitor changed: #{monitorIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending monitor change: {ex.Message}");
            }
        }

        public async Task RequestMonitorSelect(int monitorIndex)
        {
            if (_peerManager == null) return;

            try
            {
                await _peerManager.SendMonitorSelect(monitorIndex);
                Console.WriteLine($"[{DateTime.Now}] 📤 request to select monitor #{monitorIndex}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Monitor request error: {ex.Message}");
            }
        }

        public void SwitchActiveMonitor(int index)
        {
            if (index < 0 || index >= _localMonitors.Count)
                return;

            if (!_localMonitors[index].IsActive)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ monitor #{index} is inactive");
                return;
            }

            _currentMonitorIndex = index;
            _monitor = _localMonitors[_currentMonitorIndex];

            if (_captureService != null)
                _captureService.ChangeCaptureMonitor(_localMonitors[index]);

            if (_localInputHandler != null)
                _localInputHandler.SetActiveMonitor(index);

            _ = SendMonitorChanged(index);

            Console.WriteLine($"[{DateTime.Now}] ✅ switch to monitor #{index}");
        }

        public void ToggleMonitorActive(int index)
        {
            if (index < 0 || index >= _localMonitors.Count)
                return;

            _localMonitors[index].IsActive = !_localMonitors[index].IsActive;

            _ = SendMonitorStatusUpdate(index, _localMonitors[index].IsActive);

            // If the current monitor is turned off, switch to the first active one.
            if (!_localMonitors[index].IsActive && _currentMonitorIndex == index)
            {
                var firstActive = _localMonitors.FindIndex(m => m.IsActive);
                if (firstActive >= 0)
                {
                    SwitchActiveMonitor(firstActive);
                }
            }

            Console.WriteLine($"[{DateTime.Now}] ✅ Status of monitor #{index} changed");
        }

        private void OnChangeRoleRequestHandler(ClientRole role)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 OnVerifyPasswordHandler invoked");
            OnChangeRoleRequest?.Invoke(role);
        }

        private void OnVerifyPasswordHandler(string password)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 OnVerifyPasswordHandler invoked");
            OnVerifyPassword?.Invoke(password);
        }

        // ✅ Named data channel handlers
        private void OnFrameDataReceivedHandler(byte[] data)
        {
            _packetHandler?.HandleRawData(data, PacketType.Frame);
        }

        private void OnEventDataReceivedHandler(byte[] data)
        {
            _packetHandler?.HandleRawData(data, PacketType.Event);
        }

        private void OnChatDataReceivedHandler(byte[] data)
        {
            _packetHandler?.HandleRawData(data, PacketType.Chat);
        }

        private void OnFileDataReceivedHandler(byte[] data)
        {
            _packetHandler?.HandleRawData(data, PacketType.FileStart);
        }

        private void InitializeAgentServices(MonitorInfo monitor)
        {
            Console.WriteLine($"[{DateTime.Now}] Initializing Agent services");

            _monitor = monitor;
            var bound = monitor.ScreenBounds;

            _captureService = new ScreenCaptureService(bound, fps: 4, 40L);
            _captureService.ChangeCaptureMonitor(monitor);

            _captureService.OnFrameCaptured += HandleFrameCaptured;
            _captureService.OnCaptureError += (sender, error) => OnError?.Invoke(error);

            _localInputHandler = new LocalInputHandler();
            _localInputHandler.UpdateMonitorList(_localMonitors);
            _localInputHandler.SetActiveMonitor(monitor.Index);
            _localInputHandler.OnMouseOnProcess += (process) => OnMouseOnProcess?.Invoke(process);
            _localInputHandler.OnMouseDeniedOnProcess += (process) => OnMouseDeniedOnProcess?.Invoke(process);
        }

        private void OnHubStateChangedHandler(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now}] 📡 Hub State:  {e.OldState} → {e.NewState}");
            OnHubStateChanged?.Invoke(e.NewState);
        }

        private void OnPeerStateChangedHandler(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔗 Peer State: {e.OldState} → {e.NewState}");
            OnPeerStateChanged?.Invoke(e.NewState);

            if (_role == ClientRole.Agent && _captureService != null)
            {
                if (e.NewState == ConnectionStatus.Connected)
                {
                    Console.WriteLine($"[{DateTime.Now}] ▶️ Starting screen capture");
                    _captureService.Start();
                }
                else if (e.NewState == ConnectionStatus.Disconnected || e.NewState == ConnectionStatus.Failed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⏸️ Stopping screen capture");
                    _ = _captureService.StopAsync();

                    if (e.NewState == ConnectionStatus.Failed)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ℹ️ Agent peer failed, will reconnect on sessionReady");
                    }
                }
            }
        }

        private async void OnSessionReadyHandler()
        {
            Console.WriteLine($"[{DateTime.Now}] 🎉 Session ready handler invoked");

            if (_role == ClientRole.Controller)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // ✅ Create and send SDP offer
                        await CreateAndSendOfferAsync();
                        Console.WriteLine($"[{DateTime.Now}] ✅ SDP Offer sent");
                        OnSessionReady?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ❌ Error sending SDP Offer: {ex.Message}");
                    }
                });
            }
            else if (_role == ClientRole.Agent)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now}] 🔄 Agent reconnecting peer (triggered by sessionReady)...");

                        await ReconnectPeerAsync();

                        Console.WriteLine($"[{DateTime.Now}] ✅ Agent peer reconnected, creating offer");

                        // ✅ Create and send SDP offer
                        await CreateAndSendOfferAsync();

                        Console.WriteLine($"[{DateTime.Now}] ✅ SDP Offer sent");

                        OnSessionReady?.Invoke();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ❌ Agent reconnect error: {ex.Message}");
                    }
                });
            }
        }

        // ✅ NEW:  Handshake event handlers
        private void OnRequestPasswordInfoHandler()
        {
            Console.WriteLine($"[{DateTime.Now}] 📋 OnRequestPasswordInfoHandler invoked");
            OnRequestPasswordInfo?.Invoke();
        }

        private void OnRequestPasswordHandler(string caller, string sessionId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 OnRequestPasswordHandler invoked");
            OnRequestPassword?.Invoke(caller, sessionId);
        }

        private void OnPasswordIncorrectHandler()
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ OnPasswordIncorrectHandler invoked");
            OnPasswordIncorrect?.Invoke();
        }

        private void OnPasswordCorrectHandler()
        {
            Console.WriteLine($"[{DateTime.Now}] ✅ OnPasswordCorrectHandler invoked");
            OnPasswordCorrect?.Invoke();
        }

        private void OnRequestAccessPermissionHandler(string caller, string sessionId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 OnRequestAccessPermissionHandler invoked");
            OnRequestAccessPermission?.Invoke(caller, sessionId);
        }

        private void OnAccessDeniedHandler()
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ OnAccessDeniedHandler invoked");
            OnAccessDenied?.Invoke();
        }

        private void OnSessionEndedHandler(string reason)
        {
            Console.WriteLine($"[{DateTime.Now}] 📢 OnSessionEndedHandler invoked");
            OnSessionEnded?.Invoke(reason);
        }

        private void AttachPacketHandlerEvents()
        {
            if (_packetHandler == null) return;

            Console.WriteLine($"[{DateTime.Now}] 📎 Attaching packet handler events");

            _packetHandler.OnFramePacket += OnFramePacketHandler;
            _packetHandler.OnEventPacket += OnEventPacketHandler;
            _packetHandler.OnChatPacket += OnChatPacketHandler;
            _packetHandler.OnChatEventPacket += OnChatEventPacketHandler;
            _packetHandler.OnSettingsPacket += OnSettingsPacketHandler;
            _packetHandler.OnFileCancelPacket += OnFileCancelPacketHandler;

            AttachPeerManagerAVEvents();

            Console.WriteLine($"[{DateTime.Now}] ✅ Packet handler events attached");
        }

        private void OnFileCancelPacketHandler(object s, PacketReceivedEventArgs e)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🛑 File cancel packet received in SessionCoordinator");

                if (_fileTransferManager != null)
                {
                    _fileTransferManager.HandleFileCancel(e.Packet);
                    Console.WriteLine($"[{DateTime.Now}] ✅ HandleFileCancel called");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ FileTransferManager is null!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error handling cancel packet: {ex.Message}");
            }
        }

        private void DetachPacketHandlerEvents()
        {
            if (_packetHandler == null) return;

            Console.WriteLine($"[{DateTime.Now}] 📎 Detaching packet handler events");

            _packetHandler.OnFramePacket -= OnFramePacketHandler;
            _packetHandler.OnEventPacket -= OnEventPacketHandler;
            _packetHandler.OnChatPacket -= OnChatPacketHandler;
            _packetHandler.OnChatEventPacket -= OnChatEventPacketHandler;
            _packetHandler.OnSettingsPacket -= OnSettingsPacketHandler;
            _packetHandler.OnFileCancelPacket -= OnFileCancelPacketHandler;

            DetachPeerManagerAVEvents();

            Console.WriteLine($"[{DateTime.Now}] ✅ Packet handler events detached");
        }

        private void DetachAllEvents()
        {
            Console.WriteLine($"[{DateTime.Now}] 🧹 Detaching all events");

            DetachPeerManagerEvents();
            DetachPacketHandlerEvents();

            if (_chatManager != null)
            {
                _chatManager.OnNewMessage -= OnChatMessageReceivedHandler;
                _chatManager.OnChatEventReceived -= OnChatEventReceivedHandler;
            }

            if (_fileTransferManager != null)
            {
                _fileTransferManager.OnProgressUpdated -= OnFileProgressUpdatedHandler;
                _fileTransferManager.OnFileReceived -= OnFileReceivedHandler;
                _fileTransferManager.OnTransferCancelled -= TransferCancelledHandler;
            }

            Console.WriteLine($"[{DateTime.Now}] ✅ All events detached");
        }

        // Named packet handlers
        private void OnFramePacketHandler(object s, PacketReceivedEventArgs e)
        {
            if (_role == ClientRole.Controller && _renderService != null)
            {
                _renderService.ProcessFrame(e.Packet.Data);
            }
        }

        private void OnEventPacketHandler(object s, PacketReceivedEventArgs e)
        {
            if (_role == ClientRole.Agent && _localInputHandler != null)
            {
                _localInputHandler.ProcessEventData(e.Packet.Data);
            }
        }

        private void OnChatPacketHandler(object s, PacketReceivedEventArgs e)
        {
            _chatManager?.HandlePacket(e.Packet);
        }

        private void OnChatEventPacketHandler(object s, PacketReceivedEventArgs e)
        {
            _chatManager?.HandlePacket(e.Packet);
        }

        private void OnSettingsPacketHandler(object s, PacketReceivedEventArgs e)
        {
            if (_role == ClientRole.Agent)
            {
                HandleSettingsPacket(e.Packet.Data);
            }
        }

        private void HandleSettingsPacket(byte[] data)
        {
            try
            {
                byte type = data[0];
                if (type == 5)
                {
                    var json = System.Text.Encoding.UTF8.GetString(data, 1, data.Length - 1);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Poshtibano.Common.Settings>(json);

                    if (_captureService != null)
                    {
                        _captureService.UpdateSettings(settings);

                        Console.WriteLine($"[{DateTime.Now}] ⚙️ Settings updated:  Quality={settings.ImageQuality}, FPS={settings.Fps}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ :  {ex.Message}");
            }
        }

        /// <summary>
        /// Notify server that we have rejoined (for triggering sessionReady)
        /// </summary>
        public async Task NotifyServerRejoinAsync()
        {
            if (_peerManager == null)
                throw new InvalidOperationException("PeerManager not initialized");

            Console.WriteLine($"[{DateTime.Now}] 🔔 Sending rejoin notification to server..  .");

            try
            {
                await _peerManager.SendRejoinNotificationAsync();
                Console.WriteLine($"[{DateTime.Now}] ✅ Rejoin notification sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending rejoin notification: {ex.Message}");
                throw;
            }
        }

        public async Task DisconnectAsync(bool keepHubAlive = false)
        {
            if (!_isConnected && !keepHubAlive)
            {
                Console.WriteLine($"[{DateTime.Now}] ℹ️ Already disconnected");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 🔌 Disconnecting session (keepHubAlive={keepHubAlive}).. .");

            try
            {
                if (_captureService != null)
                {
                    await _captureService.StopAsync();
                }

                if (_role == ClientRole.Controller)
                {
                    CleanupControllerServices();
                }

                if (keepHubAlive && _role == ClientRole.Agent)
                {
                    Console.WriteLine($"[{DateTime.Now}] 🔄 Agent mode:  Keeping Hub alive, only disconnecting peer");

                    if (_peerManager != null)
                    {
                        _peerManager.OnFrameDataReceived -= OnFrameDataReceivedHandler;
                        _peerManager.OnEventDataReceived -= OnEventDataReceivedHandler;
                        _peerManager.OnChatDataReceived -= OnChatDataReceivedHandler;
                        _peerManager.OnFileDataReceived -= OnFileDataReceivedHandler;
                    }

                    DetachPacketHandlerEvents();

                    if (_chatManager != null)
                    {
                        _chatManager.OnNewMessage -= OnChatMessageReceivedHandler;
                        _chatManager.OnChatEventReceived -= OnChatEventReceivedHandler;
                    }

                    if (_fileTransferManager != null)
                    {
                        _fileTransferManager.OnProgressUpdated -= OnFileProgressUpdatedHandler;
                        _fileTransferManager.OnFileReceived -= OnFileReceivedHandler;
                        _fileTransferManager.OnTransferCancelled -= TransferCancelledHandler;
                    }

                    if (_peerManager != null)
                    {
                        await _peerManager.DisconnectPeerOnlyAsync();
                    }

                    _packetHandler?.Dispose();
                    _packetHandler = null;

                    _chatManager?.Dispose();
                    _chatManager = null;

                    _fileTransferManager?.Dispose();
                    _fileTransferManager = null;

                    _localInputHandler = null;

                    _isConnected = false;
                }
                else if (keepHubAlive && _role == ClientRole.Controller)
                {
                    Console.WriteLine($"[{DateTime.Now}] 🔄 Controller mode: Keeping Hub alive, only disconnecting peer");

                    if (_peerManager != null)
                    {
                        _peerManager.OnFrameDataReceived -= OnFrameDataReceivedHandler;
                        _peerManager.OnEventDataReceived -= OnEventDataReceivedHandler;
                        _peerManager.OnChatDataReceived -= OnChatDataReceivedHandler;
                        _peerManager.OnFileDataReceived -= OnFileDataReceivedHandler;
                    }

                    DetachPacketHandlerEvents();

                    if (_chatManager != null)
                    {
                        _chatManager.OnNewMessage -= OnChatMessageReceivedHandler;
                        _chatManager.OnChatEventReceived -= OnChatEventReceivedHandler;
                    }

                    if (_fileTransferManager != null)
                    {
                        _fileTransferManager.OnProgressUpdated -= OnFileProgressUpdatedHandler;
                        _fileTransferManager.OnFileReceived -= OnFileReceivedHandler;
                        _fileTransferManager.OnTransferCancelled -= TransferCancelledHandler;
                    }

                    if (_peerManager != null)
                    {
                        await _peerManager.DisconnectPeerOnlyAsync();
                    }

                    _packetHandler?.Dispose();
                    _packetHandler = null;

                    _chatManager?.Dispose();
                    _chatManager = null;

                    _fileTransferManager?.Dispose();
                    _fileTransferManager = null;

                    _renderService?.Dispose();
                    _renderService = null;

                    _remoteInputService?.Dispose();
                    _remoteInputService = null;

                    _isConnected = false;
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] 🔌 Full disconnect");
                    await FullCleanupAsync();
                }

                Console.WriteLine($"[{DateTime.Now}] ✅ Session disconnected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error during disconnect: {ex.Message}");
            }
        }

        private async Task FullCleanupAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] 🧹 Full cleanup started");

            try
            {
                _isConnected = false;

                // ✅ NEW: Cleanup Audio/Video services
                await CleanupAudioVideoServicesAsync();

                DetachAllEvents();

                if (_captureService != null)
                {
                    await _captureService.StopAsync();
                    _captureService.Dispose();
                    _captureService = null;
                }

                if (_role == ClientRole.Controller)
                {
                    _remoteInputService?.Dispose();
                    _remoteInputService = null;
                    _renderService?.Dispose();
                    _renderService = null;
                }

                _packetHandler?.Dispose();
                _packetHandler = null;

                _chatManager?.Dispose();
                _chatManager = null;

                _fileTransferManager?.Dispose();
                _fileTransferManager = null;

                if (_peerManager != null)
                {
                    await _peerManager.DisconnectAsync();
                    _peerManager.Dispose();
                    _peerManager = null;
                }

                _localInputHandler = null;
                _eventsAttached = false;

                Console.WriteLine($"[{DateTime.Now}] ✅ Full cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error during full cleanup: {ex.Message}");
            }
        }

        public async Task ReconnectPeerAsync()
        {
            if (_role != ClientRole.Agent)
            {
                throw new InvalidOperationException("ReconnectPeerAsync is only for Agent mode");
            }

            if (_peerManager == null)
            {
                throw new InvalidOperationException("PeerManager not initialized");
            }

            Console.WriteLine($"[{DateTime.Now}] 🔄 Agent reconnecting peer connection.. .");

            try
            {
                if (_peerManager != null)
                {
                    _peerManager.OnFrameDataReceived -= OnFrameDataReceivedHandler;
                    _peerManager.OnEventDataReceived -= OnEventDataReceivedHandler;
                    _peerManager.OnChatDataReceived -= OnChatDataReceivedHandler;
                    _peerManager.OnFileDataReceived -= OnFileDataReceivedHandler;
                }

                if (_chatManager != null)
                {
                    _chatManager.OnNewMessage -= OnChatMessageReceivedHandler;
                    _chatManager.OnChatEventReceived -= OnChatEventReceivedHandler;
                }

                if (_fileTransferManager != null)
                {
                    _fileTransferManager.OnProgressUpdated -= OnFileProgressUpdatedHandler;
                    _fileTransferManager.OnFileReceived -= OnFileReceivedHandler;
                    _fileTransferManager.OnTransferCancelled -= TransferCancelledHandler;
                }

                await _peerManager.DisconnectPeerOnlyAsync();
                await Task.Delay(500);

                _packetHandler?.Dispose();
                _packetHandler = new PacketHandler();
                AttachPacketHandlerEvents();

                _peerManager.OnFrameDataReceived += OnFrameDataReceivedHandler;
                _peerManager.OnEventDataReceived += OnEventDataReceivedHandler;
                _peerManager.OnChatDataReceived += OnChatDataReceivedHandler;
                _peerManager.OnFileDataReceived += OnFileDataReceivedHandler;

                _chatManager?.Dispose();
                _chatManager = new ChatManager(_peerManager, _role);
                _chatManager.OnNewMessage += OnChatMessageReceivedHandler;
                _chatManager.OnChatEventReceived += OnChatEventReceivedHandler;

                _fileTransferManager?.Dispose();
                _fileTransferManager = new FileTransferManager(_peerManager, _role);
                _fileTransferManager.OnProgressUpdated += OnFileProgressUpdatedHandler;
                _fileTransferManager.OnFileReceived += OnFileReceivedHandler;
                _fileTransferManager.OnTransferCancelled += TransferCancelledHandler;

                if (_captureService == null)
                {
                    var bound = _monitor.ScreenBounds;

                    _captureService = new ScreenCaptureService(bound, 4f, 40L);
                    _captureService.ChangeCaptureMonitor(_monitor);

                    _captureService.OnFrameCaptured += HandleFrameCaptured;
                    _captureService.OnCaptureError += (sender, error) => OnError?.Invoke(error);
                }

                _localInputHandler = new LocalInputHandler();
                _localInputHandler.UpdateMonitorList(_localMonitors);
                _localInputHandler.SetActiveMonitor(_monitor.Index);
                _localInputHandler.OnMouseOnProcess += (process) => OnMouseOnProcess?.Invoke(process);
                _localInputHandler.OnMouseDeniedOnProcess += (process) => OnMouseDeniedOnProcess?.Invoke(process);

                await _peerManager.RecreatePeerConnectionAsync();

                // ✅ NEW: Reinitialize clipboard services
                CleanupClipboardServices();
                InitializeClipboardServices();

                _isConnected = true;
                Console.WriteLine($"[{DateTime.Now}] ✅ Agent peer reconnected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Agent reconnect error: {ex.Message}");
                OnError?.Invoke($"Reconnect failed: {ex.Message}");
                throw;
            }
        }

        // ✅ NEW: Password and access response methods
        public async Task SendPasswordInfoAsync(bool hasPassword)
        {
            try
            {
                await _peerManager.SendPasswordInfoAsync(hasPassword);
                Console.WriteLine($"[{DateTime.Now}] 📤 Password info sent: hasPassword={hasPassword}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending password info: {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task SendPasswordAsync(string password)
        {
            try
            {
                await _peerManager.SendPasswordAsync(password);
                Console.WriteLine($"[{DateTime.Now}] 📤 Password sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending password: {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task SendPasswordVerificationAsync(bool isCorrect)
        {
            try
            {
                await _peerManager.SendPasswordVerificationAsync(isCorrect);
                Console.WriteLine($"[{DateTime.Now}] 📤 Password verification sent: isCorrect={isCorrect}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending password verification:  {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task SendAccessResponseAsync(bool allowed)
        {
            try
            {
                await _peerManager.SendAccessResponseAsync(allowed);
                Console.WriteLine($"[{DateTime.Now}] 📤 Access response sent: allowed={allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending access response:  {ex.Message}");
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        // ✅ Named chat/file manager handlers
        private void OnChatMessageReceivedHandler(ChatMessage msg)
        {
            if (_isDisposed || !_isConnected)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session disposed/disconnected, ignoring chat:  {msg.Id}");
                return;
            }

            OnChatMessage?.Invoke(msg);
        }

        private void OnChatEventReceivedHandler(ChatEvent evt)
        {
            if (_isDisposed || !_isConnected)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session disposed/disconnected, ignoring chat event: {evt.MessageId}");
                return;
            }

            OnChatEvent?.Invoke(evt);
        }

        private void OnFileProgressUpdatedHandler(FileTransferProgress prog)
        {
            if (_isDisposed || !_isConnected)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session disposed/disconnected, ignoring file: {prog.TransferId}");
                return;
            }

            OnFileProgress?.Invoke(prog);
        }

        private void OnFileReceivedHandler(string id, string path)
        {
            if (_isDisposed || !_isConnected)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Session disposed/disconnected, ignoring file: {id}");
                return;
            }

            OnFileReceived?.Invoke(id, path);
        }

        public async Task ReconnectControllerAsync(Control renderTarget, Form parentForm)
        {
            if (_role != ClientRole.Controller)
            {
                throw new InvalidOperationException("ReconnectControllerAsync is only for Controller mode");
            }

            if (_peerManager == null)
            {
                throw new InvalidOperationException("PeerManager not initialized");
            }

            if (renderTarget == null)
                throw new ArgumentNullException(nameof(renderTarget));

            Console.WriteLine($"[{DateTime.Now}] 🔄 Controller reconnecting peer connection.. .");

            try
            {
                if (_peerManager != null)
                {
                    _peerManager.OnFrameDataReceived -= OnFrameDataReceivedHandler;
                    _peerManager.OnEventDataReceived -= OnEventDataReceivedHandler;
                    _peerManager.OnChatDataReceived -= OnChatDataReceivedHandler;
                    _peerManager.OnFileDataReceived -= OnFileDataReceivedHandler;
                }

                if (_chatManager != null)
                {
                    _chatManager.OnNewMessage -= OnChatMessageReceivedHandler;
                    _chatManager.OnChatEventReceived -= OnChatEventReceivedHandler;
                }

                if (_fileTransferManager != null)
                {
                    _fileTransferManager.OnProgressUpdated -= OnFileProgressUpdatedHandler;
                    _fileTransferManager.OnFileReceived -= OnFileReceivedHandler;
                    _fileTransferManager.OnTransferCancelled -= TransferCancelledHandler;
                }

                _renderService?.Dispose();
                _remoteInputService?.Dispose();
                _packetHandler?.Dispose();
                _chatManager?.Dispose();
                _fileTransferManager?.Dispose();

                _packetHandler = new PacketHandler();
                AttachPacketHandlerEvents();

                _peerManager.OnFrameDataReceived += OnFrameDataReceivedHandler;
                _peerManager.OnEventDataReceived += OnEventDataReceivedHandler;
                _peerManager.OnChatDataReceived += OnChatDataReceivedHandler;
                _peerManager.OnFileDataReceived += OnFileDataReceivedHandler;

                _chatManager = new ChatManager(_peerManager, _role);
                _chatManager.OnNewMessage += OnChatMessageReceivedHandler;
                _chatManager.OnChatEventReceived += OnChatEventReceivedHandler;

                _fileTransferManager = new FileTransferManager(_peerManager, _role);
                _fileTransferManager.OnProgressUpdated += OnFileProgressUpdatedHandler;
                _fileTransferManager.OnFileReceived += OnFileReceivedHandler;
                _fileTransferManager.OnTransferCancelled += TransferCancelledHandler;

                InitializeControllerServices(renderTarget, parentForm);

                await _peerManager.RecreatePeerConnectionAsync();

                // ✅ NEW: Reinitialize clipboard services
                CleanupClipboardServices();
                InitializeClipboardServices();

                _isConnected = true;
                Console.WriteLine($"[{DateTime.Now}] ✅ Controller peer reconnected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Controller reconnect error: {ex.Message}");
                OnError?.Invoke($"Reconnect failed: {ex.Message}");
                throw;
            }
        }

        private void InitializeControllerServices(Control renderTarget, Form parentForm)
        {
            if (renderTarget == null)
                throw new ArgumentNullException(nameof(renderTarget), "Render target required for Controller");

            Console.WriteLine($"[{DateTime.Now}] Initializing Controller services");

            _renderService = new FrameRenderService(renderTarget);
            _renderService.OnRenderError += (sender, error) => OnError?.Invoke(error);

            _remoteInputService = new RemoteInputService(_role, renderTarget);
            _remoteInputService.OnInputEventGenerated += SendInputEvent;

            if (parentForm != null)
            {
                _remoteInputService.InitializeForController(parentForm, renderTarget);
            }
        }

        private void HandleFrameCaptured(object sender, FrameCapturedEventArgs e)
        {
            if (!_isConnected || _peerManager == null)
                return;

            var packet = new Packet
            {
                IsDropable = true,
                SequenceNumber = e.SequenceNumber,
                Type = PacketType.Frame,
                Data = e.EncodedData
            };

            SendPacket(packet);
        }

        private void SendInputEvent(byte[] data)
        {
            var packet = new Packet
            {
                IsDropable = false,
                Type = PacketType.Event,
                Data = data,
                SequenceNumber = (ulong)DateTime.UtcNow.Ticks
            };

            SendPacket(packet);
        }

        public void SendPacket(Packet packet)
        {
            if (_peerManager == null || !_isConnected)
            {
                Console.WriteLine($"[{DateTime.Now}] Cannot send packet - not connected");
                return;
            }

            try
            {
                var serialized = Packet.Serialize(packet);

                if (serialized.Length > Packet.MaxFragmentSize)
                {
                    var fragments = Packet.FragmentPacket(packet);
                    foreach (var frag in fragments)
                    {
                        SendPacketDirect(frag);
                        Task.Delay(5).Wait();
                    }
                }
                else
                {
                    SendPacketDirect(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error sending packet: {ex.Message}");
            }
        }

        private void SendPacketDirect(Packet packet)
        {
            var serialized = Packet.Serialize(packet);

            switch (packet.Type)
            {
                case PacketType.Frame:
                    _peerManager.SendFrame(serialized);
                    break;
                case PacketType.Event:
                case PacketType.Settings:
                    _peerManager.SendEvent(serialized);
                    break;
                case PacketType.Chat:
                case PacketType.ChatEvent:
                    _peerManager.SendChat(serialized);
                    break;
                case PacketType.FileStart:
                case PacketType.FileChunk:
                case PacketType.FileEnd:
                    _peerManager.SendFile(serialized);
                    break;
            }
        }

        public async Task SendChatMessageAsync(string text)
        {
            if (_chatManager != null)
            {
                await _chatManager.SendMessageAsync(text);
            }
        }

        public void SendChatEvent(Guid messageId, ChatEventType eventType, object data)
        {
            _chatManager?.SendChatEvent(messageId, eventType, data);
        }

        public string CancelFileTransfer(string transferId)
        {
            if (_fileTransferManager != null)
            {
                var fileName = _fileTransferManager.CancelTransfer(transferId);
                Console.WriteLine($"[{DateTime.Now}] 🛑 Transfer cancelled: {transferId}");
                return fileName;
            }

            return string.Empty;
        }

        public void CancelAllFileTransfers()
        {
            if (_fileTransferManager != null)
            {
                _fileTransferManager.CancelAllTransfers();
                Console.WriteLine($"[{DateTime.Now}] 🛑 All transfers cancelled");
            }
        }

        public async Task SendFilesAsync(string[] paths)
        {
            if (_fileTransferManager != null)
            {
                var progress = new Progress<FileTransferProgress>(prog =>
                {
                    OnFileProgress?.Invoke(prog);
                    Console.WriteLine($"[{DateTime.Now}] 📤 Sending {prog.FileName}: {prog.Percent:F1}%");
                });

                await _fileTransferManager.SendPathsAsync(paths, progress);
            }
        }

        public async Task SendSettingsAsync(long quality, float fps, bool fullFrame)
        {
            var settings = new Poshtibano.Common.Settings { ImageQuality = quality, Fps = fps, FullFrame = fullFrame };
            var json = JsonSerializer.Serialize(settings);
            var data = Encoding.UTF8.GetBytes(json);
            var fullData = new byte[data.Length + 1];
            fullData[0] = 5;
            Array.Copy(data, 0, fullData, 1, data.Length);

            var packet = new Packet
            {
                IsDropable = false,
                Type = PacketType.Settings,
                Data = fullData,
                SequenceNumber = (ulong)DateTime.UtcNow.Ticks
            };

            SendPacket(packet);
        }

        private async Task CreateAndSendOfferAsync()
        {
            if (_peerManager != null && _role == ClientRole.Agent)
            {
                await _peerManager.CreateAndSendOfferAsync();
            }
        }

        // Controller-specific methods
        public void HandleMouseMove(System.Windows.Forms.MouseEventArgs e)
        {
            if (_remoteInputService != null && _renderService != null)
            {
                _remoteInputService.RenderRect = _renderService.LastRenderRect;
                _remoteInputService.HandleMouseMove(e);
            }
        }

        public void HandleMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            _remoteInputService?.HandleMouseDown(e);
        }

        public void HandleMouseUp(System.Windows.Forms.MouseEventArgs e)
        {
            _remoteInputService?.HandleMouseUp(e);
        }

        public void HandleMouseWheel(System.Windows.Forms.MouseEventArgs e, Point clientPos)
        {
            _remoteInputService?.HandleMouseWheel(e, clientPos);
        }

        public void HandleClipboardUpdate()
        {
            _remoteInputService?.HandleClipboardUpdate();
        }

        public void SetKeyboardSuppression(bool suppress)
        {
            _remoteInputService?.SetKeyboardSuppression(suppress);
        }

        private void CleanupControllerServices()
        {
            Console.WriteLine($"[{DateTime.Now}] 🧹 Cleaning up Controller services");

            try
            {
                if (_remoteInputService != null)
                {
                    _remoteInputService.Dispose();
                    _remoteInputService = null;
                }

                if (_renderService != null)
                {
                    _renderService.Dispose();
                    _renderService = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error cleaning up controller services: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            Console.WriteLine($"[{DateTime.Now}] 🗑️ Disposing SessionCoordinator");

            DisconnectAsync().Wait(2000);

            // ✅ NEW: Cleanup clipboard
            CleanupClipboardServices();

            _captureService?.Dispose();
            _renderService?.Dispose();
            _remoteInputService?.Dispose();
            _localInputHandler = null;
            _packetHandler?.Dispose();
            _chatManager?.Dispose();
            _fileTransferManager?.Dispose();
            _peerManager?.Dispose();

            // Clear events
            OnHubStateChanged = null;
            OnPeerStateChanged = null;
            OnSessionReady = null;
            OnAccessRequested = null;

            OnChatMessage = null;
            OnChatEvent = null;
            OnFileProgress = null;
            OnFileReceived = null;
            OnError = null;
            OnRequestPasswordInfo = null;
            OnRequestPassword = null;
            OnPasswordIncorrect = null;
            OnPasswordCorrect = null;
            OnRequestAccessPermission = null;
            OnAccessDenied = null;
            OnSessionEnded = null;

            _eventsAttached = false;

            Console.WriteLine($"[{DateTime.Now}] ✅ SessionCoordinator disposed");
        }
    }
}