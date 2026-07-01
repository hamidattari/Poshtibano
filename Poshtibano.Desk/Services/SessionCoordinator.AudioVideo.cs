using Poshtibano.Common;
using Poshtibano.Desk.Forms;
using Poshtibano.Desk.Services.Hardware;
using Poshtibano.Desk.Services.Media;
using Poshtibano.Desk.Services.Networking;
using System;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services
{
    public partial class SessionCoordinator
    {
        // ============================================================
        // AUDIO/VIDEO SERVICES
        // ============================================================

        private AudioCaptureService _audioCaptureService;
        private AudioPlaybackService _audioPlaybackService;
        private VideoCaptureService _videoCaptureService;
        private AudioVideoStateManager _avStateManager;

        private WebcamViewerForm _webcamViewerForm;
        private readonly object _webcamViewerLock = new object();

        private bool _myMicrophoneActive = true;
        private bool _myWebcamActive = true;
        private bool _theirMicrophoneAvailable = true;
        private bool _theirWebcamAvailable = true;

        private bool _avEventsAttached = false;

        private string _remoteUserName = "Remote User";

        // Hardware flags
        public bool HasMicrophone { get; private set; }
        public bool HasWebcam { get; private set; }

        // State manager
        public AudioVideoStateManager AVStateManager => _avStateManager;
        public bool IsAudioPlaybackMuted => _audioPlaybackService?.IsMuted ?? false;

        // ============================================================
        // EVENTS
        // ============================================================
        public event Action<bool, bool> OnTheirMicophoneAndWebcamAvailabilityReceived;

        public event Action<bool> OnMyMicrophoneActiveChanged;
        public event Action<bool> OnMyWebcamActiveChanged;
        public event Action<bool> OnTheirMicrophoneActiveChanged;
        public event Action<bool> OnTheirWebcamActiveChanged;

        // ============================================================
        // EVENTS
        // ============================================================

        public event Action<MediaPermissionRequest> OnTheyWantToSendAudio;
        public event Action<MediaPermissionRequest> OnTheyWantToSendWebcam;
        public event Action<MediaPermissionRequest> OnTheyWantToReceiveMyAudio;
        public event Action<MediaPermissionRequest> OnTheyWantToReceiveMyWebcam;

        // ============================================================
        // EVENTS 
        // ============================================================

        public event Action<bool> OnMyAudioSendPermissionResponse;
        public event Action<bool> OnMyWebcamSendPermissionResponse;
        public event Action<bool> OnTheirAudioPermissionResponse;
        public event Action<bool> OnTheirWebcamPermissionResponse;

        // ============================================================
        // EVENTS
        // ============================================================

        public event Action<MediaStreamState> OnLocalAudioStateChanged;
        public event Action<MediaStreamState> OnLocalWebcamStateChanged;
        public event Action<MediaStreamState> OnRemoteAudioStateChanged;
        public event Action<MediaStreamState> OnRemoteWebcamStateChanged;

        // ============================================================
        // INITIALIZATION
        // ============================================================

        private void InitializeAudioVideoServices()
        {
            Console.WriteLine($"[{DateTime.Now}] 🎬 Initializing Audio/Video services");

            try
            {
                _avStateManager = new AudioVideoStateManager();
                Console.WriteLine($"[{DateTime.Now}] 📊 AudioVideoStateManager created");

                // Detect hardware
                var (hasMic, hasCam, micCount, camCount) = MediaDeviceDetector.GetDeviceSummary();
                HasMicrophone = hasMic;
                HasWebcam = hasCam;

                Console.WriteLine($"[{DateTime.Now}] 🎤 Microphones: {micCount}, 📷 Webcams: {camCount}");

                // Attach peer manager events
                AttachPeerManagerAVEvents();

                Console.WriteLine($"[{DateTime.Now}] ✅ Audio/Video services initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error initializing A/V:  {ex.Message}");
            }
        }

        // ============================================================
        // PEER MANAGER EVENTS
        // ============================================================

        private void AttachPeerManagerAVEvents()
        {
            if (_peerManager == null || _avEventsAttached) return;

            _peerManager.OnTheyWantToSendAudioReceived += HandleTheyWantToSendAudio;
            _peerManager.OnTheyWantToSendWebcamReceived += HandleTheyWantToSendWebcam;
            _peerManager.OnTheyWantToReceiveMyAudioReceived += HandleTheyWantToReceiveMyAudio;
            _peerManager.OnTheyWantToReceiveMyWebcamReceived += HandleTheyWantToReceiveMyWebcam;

            _peerManager.OnMyAudioSendResponseReceived += HandleMyAudioSendResponse;
            _peerManager.OnMyWebcamSendResponseReceived += HandleMyWebcamSendResponse;
            _peerManager.OnTheirAudioResponseReceived += HandleTheirAudioResponse;
            _peerManager.OnTheirWebcamResponseReceived += HandleTheirWebcamResponse;

            _peerManager.OnAudioDataReceived += HandleAudioDataReceived;
            _peerManager.OnWebcamDataReceived += HandleWebcamDataReceived;

            _peerManager.OnStopAudioReceived += HandleStopAudioReceived;
            _peerManager.OnStopWebcamReceived += HandleStopWebcamReceived;

            _peerManager.OnTheirMicrophoneActiveReceived += OnTheirMicActiveReceived;
            _peerManager.OnTheirWebcamActiveReceived += OnTheirWebcamAactiveReceived;

            _peerManager.OnTheirMicophoneAndWebcamAvailabilityReceived += HandleOnTheirMicophoneAndWebcamAvailabilityReceived;

            _avEventsAttached = true;
            Console.WriteLine($"[{DateTime.Now}] ✅ Peer manager A/V events attached");
        }

        private void DetachPeerManagerAVEvents()
        {
            if (_peerManager == null || !_avEventsAttached) return;

            _peerManager.OnTheyWantToSendAudioReceived -= HandleTheyWantToSendAudio;
            _peerManager.OnTheyWantToSendWebcamReceived -= HandleTheyWantToSendWebcam;
            _peerManager.OnTheyWantToReceiveMyAudioReceived -= HandleTheyWantToReceiveMyAudio;
            _peerManager.OnTheyWantToReceiveMyWebcamReceived -= HandleTheyWantToReceiveMyWebcam;
            _peerManager.OnMyAudioSendResponseReceived -= HandleMyAudioSendResponse;
            _peerManager.OnMyWebcamSendResponseReceived -= HandleMyWebcamSendResponse;
            _peerManager.OnTheirAudioResponseReceived -= HandleTheirAudioResponse;
            _peerManager.OnTheirWebcamResponseReceived -= HandleTheirWebcamResponse;
            _peerManager.OnAudioDataReceived -= HandleAudioDataReceived;
            _peerManager.OnWebcamDataReceived -= HandleWebcamDataReceived;
            _peerManager.OnStopAudioReceived -= HandleStopAudioReceived;
            _peerManager.OnStopWebcamReceived -= HandleStopWebcamReceived;

            _peerManager.OnTheirMicrophoneActiveReceived -= OnTheirMicActiveReceived;
            _peerManager.OnTheirWebcamActiveReceived -= OnTheirWebcamAactiveReceived;

            _avEventsAttached = false;
        }

        public async Task SetMyMicrophoneActiveAsync(bool active)
        {
            if (_peerManager == null || !_isConnected || !HasMicrophone) return;

            _myMicrophoneActive = active;

            if (!active)
            {
                if (_audioCaptureService?.IsCapturing == true)
                {
                    await StopSendingMyAudioAsync();
                }
                _avStateManager?.SetLocalAudioState(MediaStreamState.Idle);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.Idle);
            }

            await _peerManager.SetMyMicrophoneActiveAsync(active);

            OnMyMicrophoneActiveChanged?.Invoke(active);
            Console.WriteLine($"[{DateTime.Now}] 🎤 My microphone:  {(active ? "Active" : "Inactive")}");
        }

        public async Task SetMyWebcamActiveAsync(bool active)
        {
            if (_peerManager == null || !_isConnected || !HasMicrophone) return;

            _myWebcamActive = active;

            if (!active)
            {
                if (_videoCaptureService?.IsCapturing == true)
                {
                    await StopSendingMyWebcamAsync();
                }
                _avStateManager?.SetLocalWebcamState(MediaStreamState.Idle);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Idle);
            }

            await _peerManager.SetMyWebcamActiveAsync(active);

            OnMyWebcamActiveChanged?.Invoke(active);
            Console.WriteLine($"[{DateTime.Now}] 📷 My webcam: {(active ? "Active" : "Inactive")}");
        }

        // ============================================================
        // REQUEST METHODS
        // ============================================================

        public async Task RequestToSendMyAudioAsync(string name)
        {
            if (_peerManager == null || !_isConnected || !HasMicrophone) return;
            if (!_myMicrophoneActive || !_myWebcamActive) return;

            try
            {
                _avStateManager?.SetLocalAudioState(MediaStreamState.Requesting);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.Requesting);

                Console.WriteLine($"[{DateTime.Now}] 📤 Requesting to send MY audio");
                await _peerManager.SendMyAudioRequestAsync(name, _role);

                _avStateManager?.SetLocalAudioState(MediaStreamState.WaitingPermission);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.WaitingPermission);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error:  {ex.Message}");
                _avStateManager?.SetLocalAudioState(MediaStreamState.Idle);
            }
        }

        public async Task RequestToSendMyWebcamAsync(string name)
        {
            if (_peerManager == null || !_isConnected || !HasWebcam) return;

            try
            {
                _avStateManager?.SetLocalWebcamState(MediaStreamState.Requesting);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Requesting);

                Console.WriteLine($"[{DateTime.Now}] 📤 Requesting to send MY webcam");
                await _peerManager.SendMyWebcamRequestAsync(name, _role);

                _avStateManager?.SetLocalWebcamState(MediaStreamState.WaitingPermission);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.WaitingPermission);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
                _avStateManager?.SetLocalWebcamState(MediaStreamState.Idle);
            }
        }

        public async Task RequestTheirAudioAsync(string name)
        {
            if (_peerManager == null || !_isConnected) return;

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 📤 Requesting to hear THEIR audio");
                await _peerManager.SendTheirAudioRequestAsync(name, _role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error:  {ex.Message}");
            }
        }

        public async Task RequestTheirWebcamAsync(string name)
        {
            if (_peerManager == null || !_isConnected) return;

            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    _remoteUserName = name;
                }

                Console.WriteLine($"[{DateTime.Now}] 📤 Requesting to see THEIR webcam");
                await _peerManager.SendTheirWebcamRequestAsync(name, _role);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // RESPONSE METHODS
        // ============================================================

        public async Task RespondToTheirAudioSendRequestAsync(bool allowed)
        {
            if (_peerManager == null) return;

            try
            {
                await _peerManager.SendTheirAudioSendResponseAsync(allowed);

                if (allowed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ✅ Allowed, starting PLAYBACK to hear them");
                    _avStateManager?.SetRemoteAudioState(MediaStreamState.Streaming);
                    OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                    StartAudioPlayback();
                }

                Console.WriteLine($"[{DateTime.Now}] 📤 Response sent:  {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task RespondToTheirWebcamSendRequestAsync(bool allowed, string name)
        {
            if (_peerManager == null) return;

            try
            {
                await _peerManager.SendTheirWebcamSendResponseAsync(allowed);

                if (allowed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ✅ Allowed, opening viewer");
                    _avStateManager?.SetRemoteWebcamState(MediaStreamState.Streaming);
                    OnRemoteWebcamStateChanged?.Invoke(MediaStreamState.Streaming);
                    // todo ShowWebcamViewer(name);
                    ShowWebcamViewer(_remoteUserName);

                    _avStateManager?.SetRemoteAudioState(MediaStreamState.Streaming);
                    OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                    StartAudioPlayback();
                }

                Console.WriteLine($"[{DateTime.Now}] 📤 Response sent: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task RespondToTheirReceiveMyAudioRequestAsync(bool allowed)
        {
            if (_peerManager == null) return;

            try
            {
                await _peerManager.SendMyAudioReceiveResponseAsync(allowed);

                if (allowed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ✅ Allowed, starting MY audio CAPTURE");
                    _avStateManager?.SetLocalAudioState(MediaStreamState.Streaming);
                    OnLocalAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                    StartAudioCapture();
                }

                Console.WriteLine($"[{DateTime.Now}] 📤 Response sent: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task RespondToTheirReceiveMyWebcamRequestAsync(string name, bool allowed)
        {
            if (_peerManager == null) return;

            try
            {
                await _peerManager.SendMyWebcamReceiveResponseAsync(name, allowed);

                if (allowed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ✅ Allowed, starting MY webcam CAPTURE");
                    _avStateManager?.SetLocalWebcamState(MediaStreamState.Streaming);
                    OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Streaming);
                    StartWebcamCapture();

                    if (HasMicrophone && _audioCaptureService?.IsCapturing != true)
                    {
                        _avStateManager?.SetLocalAudioState(MediaStreamState.Streaming);
                        OnLocalAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                        StartAudioCapture();
                        Console.WriteLine($"[{DateTime.Now}] 🎤 Audio capture started with webcam");
                    }
                }

                Console.WriteLine($"[{DateTime.Now}] 📤 Response sent: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // HANDLE INCOMING REQUESTS
        // ============================================================

        private void HandleTheyWantToSendAudio(MediaPermissionRequest request)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 They want to send their audio");
            OnTheyWantToSendAudio?.Invoke(request);
        }

        private void HandleTheyWantToSendWebcam(MediaPermissionRequest request)
        {
            if (!string.IsNullOrEmpty(request.RequesterName))
            {
                _remoteUserName = request.RequesterName;
            }

            Console.WriteLine($"[{DateTime.Now}] 📥 They want to send their webcam");
            OnTheyWantToSendWebcam?.Invoke(request);
        }

        private void HandleTheyWantToReceiveMyAudio(MediaPermissionRequest request)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 They want to receive my audio");
            OnTheyWantToReceiveMyAudio?.Invoke(request);
        }

        private void HandleTheyWantToReceiveMyWebcam(MediaPermissionRequest request)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 They want to receive my webcam");
            OnTheyWantToReceiveMyWebcam?.Invoke(request);
        }

        // ============================================================
        // HANDLE INCOMING RESPONSES
        // ============================================================

        private void HandleMyAudioSendResponse(bool allowed)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 My audio send response: {allowed}");

            if (allowed)
            {
                _avStateManager?.SetLocalAudioState(MediaStreamState.Streaming);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                StartAudioCapture();
            }
            else
            {
                _avStateManager?.SetLocalAudioState(MediaStreamState.Denied);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.Denied);
            }

            OnMyAudioSendPermissionResponse?.Invoke(allowed);
        }

        private void HandleMyWebcamSendResponse(bool allowed)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 My webcam send response: {allowed}");

            if (allowed)
            {
                _avStateManager?.SetLocalWebcamState(MediaStreamState.Streaming);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Streaming);
                StartWebcamCapture();

                if (HasMicrophone && _audioCaptureService?.IsCapturing != true)
                {
                    _avStateManager?.SetLocalAudioState(MediaStreamState.Streaming);
                    OnLocalAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                    StartAudioCapture();
                    Console.WriteLine($"[{DateTime.Now}] 🎤 Audio capture started with webcam");
                }
            }
            else
            {
                _avStateManager?.SetLocalWebcamState(MediaStreamState.Denied);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Denied);
            }

            OnMyWebcamSendPermissionResponse?.Invoke(allowed);
        }

        private void HandleTheirAudioResponse(bool allowed)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 Their audio response: {allowed}");

            if (allowed)
            {
                _avStateManager?.SetRemoteAudioState(MediaStreamState.Streaming);
                OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                StartAudioPlayback();
            }
            else
            {
                _avStateManager?.SetRemoteAudioState(MediaStreamState.Denied);
                OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Denied);
            }

            OnTheirAudioPermissionResponse?.Invoke(allowed);
        }

        private void HandleTheirWebcamResponse(string name, bool allowed)
        {
            Console.WriteLine($"[{DateTime.Now}] 📥 Their webcam response: {allowed}");

            if (allowed)
            {
                _avStateManager?.SetRemoteWebcamState(MediaStreamState.Streaming);
                OnRemoteWebcamStateChanged?.Invoke(MediaStreamState.Streaming);
                ShowWebcamViewer(name);

                _avStateManager?.SetRemoteAudioState(MediaStreamState.Streaming);
                OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Streaming);
                StartAudioPlayback();
            }
            else
            {
                _avStateManager?.SetRemoteWebcamState(MediaStreamState.Denied);
                OnRemoteWebcamStateChanged?.Invoke(MediaStreamState.Denied);
            }

            OnTheirWebcamPermissionResponse?.Invoke(allowed);
        }

        // ============================================================
        // HANDLE STOP COMMANDS
        // ============================================================

        private async void HandleStopAudioReceived()
        {
            Console.WriteLine($"[{DateTime.Now}] ⏹️ Stop audio received");

            if (_audioCaptureService != null && _audioCaptureService.IsCapturing)
            {
                await _audioCaptureService.StopAsync();
                _audioCaptureService.Dispose();
                _audioCaptureService = null;
                _avStateManager?.SetLocalAudioState(MediaStreamState.Stopped);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.Stopped);
            }

            _avStateManager?.SetRemoteAudioState(MediaStreamState.Stopped);
            OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Stopped);
        }

        private void HandleOnTheirMicophoneAndWebcamAvailabilityReceived(bool hasMicrophone, bool hasBwecam)
        {
            OnTheirMicophoneAndWebcamAvailabilityReceived?.Invoke(hasMicrophone, hasBwecam);
        }

        private void OnTheirWebcamAactiveReceived(bool available)
        {
            _theirWebcamAvailable = available;
            OnTheirWebcamActiveChanged?.Invoke(available);
        }

        private void OnTheirMicActiveReceived(bool available)
        {
            _theirMicrophoneAvailable = available;
            OnTheirMicrophoneActiveChanged?.Invoke(available);
        }
        private async void HandleStopWebcamReceived()
        {
            Console.WriteLine($"[{DateTime.Now}] ⏹️ Stop webcam received");

            if (_videoCaptureService != null && _videoCaptureService.IsCapturing)
            {
                await _videoCaptureService.StopAsync();
                _videoCaptureService.Dispose();
                _videoCaptureService = null;
                _avStateManager?.SetLocalWebcamState(MediaStreamState.Stopped);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Stopped);
            }

            _avStateManager?.SetRemoteWebcamState(MediaStreamState.Stopped);
            OnRemoteWebcamStateChanged?.Invoke(MediaStreamState.Stopped);
            CloseWebcamViewer();
        }

        // ============================================================
        // HANDLE DATA RECEIVED
        // ============================================================

        private void HandleAudioDataReceived(byte[] audioData, int sampleRate, int channels,
            int bitsPerSample, long timestampTicks, uint sequenceNumber)
        {
            if (audioData == null || audioData.Length == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Audio data is null or empty");
                return;
            }

            if (_audioPlaybackService == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ AudioPlaybackService is null, starting.. .");
                StartAudioPlayback();
            }

            if (_audioPlaybackService == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Failed to create AudioPlaybackService");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] ▶️ Adding {audioData.Length} bytes to playback, IsPlaying={_audioPlaybackService.IsPlaying}");
            _audioPlaybackService.AddSamples(audioData);
        }

        private void HandleWebcamDataReceived(byte[] videoData, byte[] audioData, int width, int height,
            int audioSampleRate, int audioChannels, long timestampTicks, uint sequenceNumber)
        {
            if (videoData == null || videoData.Length == 0) return;

            var remoteWebcamState = _avStateManager?.RemoteWebcamState ?? MediaStreamState.Idle;
            if (remoteWebcamState != MediaStreamState.Streaming)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Webcam data received but RemoteWebcamState={remoteWebcamState}, ignoring");
                return;
            }

            Console.WriteLine($"[{DateTime.Now}] 📹 Webcam data received: {videoData.Length} bytes, {width}x{height}");

            bool needsViewer = false;
            lock (_webcamViewerLock)
            {
                needsViewer = (_webcamViewerForm == null || _webcamViewerForm.IsDisposed);
            }

            if (needsViewer)
            {
                Console.WriteLine($"[{DateTime.Now}] 📺 Opening WebcamViewer...");
                ShowWebcamViewer(_remoteUserName);
                return;
            }

            try
            {
                lock (_webcamViewerLock)
                {
                    if (_webcamViewerForm != null && !_webcamViewerForm.IsDisposed)
                    {
                        _webcamViewerForm.UpdateFrame(videoData, width, height);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error updating webcam frame: {ex.Message}");
            }

            if (audioData != null && audioData.Length > 0)
            {
                _audioPlaybackService?.AddSamples(audioData);
            }
        }

        // ============================================================
        // CAPTURE SERVICES
        // ============================================================

        private void StartAudioCapture()
        {
            if (_audioCaptureService != null && _audioCaptureService.IsCapturing)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Audio capture already running");
                return;
            }

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 🎤 Starting audio capture...");

                _audioCaptureService = new AudioCaptureService(
                    deviceIndex: 0,
                    sampleRate: 16000,
                    channels: 1,
                    bitsPerSample: 16
                );

                _audioCaptureService.OnAudioCaptured += OnLocalAudioCaptured;
                _audioCaptureService.OnCaptureError += error =>
                    Console.WriteLine($"[{DateTime.Now}] ❌ Audio capture error: {error}");

                _audioCaptureService.Start();

                Console.WriteLine($"[{DateTime.Now}] ✅ Audio capture started, IsCapturing={_audioCaptureService.IsCapturing}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error starting audio capture:  {ex.Message}");
            }
        }

        private void OnLocalAudioCaptured(byte[] audioData, int sampleRate, int channels,
            int bitsPerSample, long timestampTicks, uint sequenceNumber)
        {
            Console.WriteLine($"[{DateTime.Now}] 🎤 Audio captured: {audioData?.Length ?? 0} bytes, seq={sequenceNumber}");

            if (_peerManager == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ PeerManager is null");
                return;
            }

            if (!_isConnected)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Not connected");
                return;
            }

            try
            {
                _peerManager.SendAudioData(_role, audioData, sampleRate, channels, bitsPerSample, timestampTicks, sequenceNumber);
                Console.WriteLine($"[{DateTime.Now}] 📤 Audio sent:  seq={sequenceNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending audio: {ex.Message}");
            }
        }

        private void StartWebcamCapture()
        {
            if (_videoCaptureService != null && _videoCaptureService.IsCapturing)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Webcam capture already running");
                return;
            }

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 📷 Starting webcam capture...");

                _videoCaptureService = new VideoCaptureService(
                    deviceIndex: -1, // Auto-select best device
                    targetFps: 15,
                    quality: 50
                );

                _videoCaptureService.OnFrameCaptured += OnLocalWebcamFrameCaptured;
                _videoCaptureService.OnCaptureError += error => Console.WriteLine($"[{DateTime.Now}] ❌ Webcam error: {error}");

                _videoCaptureService.Start();

                Console.WriteLine($"[{DateTime.Now}] ✅ Webcam capture started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        private void OnLocalWebcamFrameCaptured(byte[] videoData, int width, int height,
            long timestampTicks, uint sequenceNumber)
        {
            if (_peerManager == null || !_isConnected) return;

            try
            {
                _peerManager.SendWebcamData(_role, videoData, null, width, height, 0, 0, timestampTicks, sequenceNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending webcam:  {ex.Message}");
            }
        }

        // ============================================================
        // PLAYBACK SERVICE
        // ============================================================

        private void StartAudioPlayback()
        {
            if (_audioPlaybackService != null && _audioPlaybackService.IsPlaying)
            {
                return;
            }

            try
            {
                _audioPlaybackService = new AudioPlaybackService(
                    sampleRate: 16000,
                    channels: 1,
                    bitsPerSample: 16,
                    jitterBufferMs: 100
                );

                _audioPlaybackService.Start();
                Console.WriteLine($"[{DateTime.Now}] ▶️ Audio playback started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error:  {ex.Message}");
            }
        }

        // ============================================================
        // STOP METHODS
        // ============================================================

        public async Task StopSendingMyAudioAsync()
        {
            try
            {
                if (_audioCaptureService != null)
                {
                    await _audioCaptureService.StopAsync();
                    _audioCaptureService.Dispose();
                    _audioCaptureService = null;
                }

                _avStateManager?.SetLocalAudioState(MediaStreamState.Stopped);
                OnLocalAudioStateChanged?.Invoke(MediaStreamState.Stopped);

                await _peerManager?.SendStopMyAudioAsync();

                Console.WriteLine($"[{DateTime.Now}] ⏹️ My audio stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error:  {ex.Message}");
            }
        }

        public async Task StopSendingMyWebcamAsync()
        {
            try
            {
                if (_videoCaptureService != null)
                {
                    await _videoCaptureService.StopAsync();
                    _videoCaptureService.Dispose();
                    _videoCaptureService = null;
                }

                _avStateManager?.SetLocalWebcamState(MediaStreamState.Stopped);
                OnLocalWebcamStateChanged?.Invoke(MediaStreamState.Stopped);

                if (_audioCaptureService != null)
                {
                    await _audioCaptureService.StopAsync();
                    _audioCaptureService.Dispose();
                    _audioCaptureService = null;
                    _avStateManager?.SetLocalAudioState(MediaStreamState.Stopped);
                    OnLocalAudioStateChanged?.Invoke(MediaStreamState.Stopped);
                    Console.WriteLine($"[{DateTime.Now}] 🎤 Audio capture stopped with webcam");
                }

                await _peerManager?.SendStopMyWebcamAsync();

                Console.WriteLine($"[{DateTime.Now}] ⏹️ My webcam stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task StopReceivingTheirAudioAsync()
        {
            try
            {
                if (_audioPlaybackService != null)
                {
                    await _audioPlaybackService.StopAsync();
                    _audioPlaybackService.Dispose();
                    _audioPlaybackService = null;
                }

                _avStateManager?.SetRemoteAudioState(MediaStreamState.Stopped);
                OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Stopped);

                await _peerManager?.SendStopReceivingAudioAsync();

                Console.WriteLine($"[{DateTime.Now}] ⏹️ Their audio stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task StopReceivingTheirWebcamAsync()
        {
            try
            {
                CloseWebcamViewer();

                _avStateManager?.SetRemoteWebcamState(MediaStreamState.Stopped);
                OnRemoteWebcamStateChanged?.Invoke(MediaStreamState.Stopped);

                if (_audioPlaybackService != null)
                {
                    await _audioPlaybackService.StopAsync();
                    _audioPlaybackService.Dispose();
                    _audioPlaybackService = null;
                    _avStateManager?.SetRemoteAudioState(MediaStreamState.Stopped);
                    OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Stopped);
                    Console.WriteLine($"[{DateTime.Now}] 🔊 Audio playback stopped with webcam");
                }

                await _peerManager?.SendStopReceivingWebcamAsync();

                Console.WriteLine($"[{DateTime.Now}] ⏹️ Their webcam stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // WEBCAM VIEWER
        // ============================================================

        private void ShowWebcamViewer(string senderName)
        {
            if (_uiContext != null)
            {
                _uiContext.Send(_ => ShowWebcamViewerInternal(senderName), null);
            }
            else
            {
                ShowWebcamViewerInternal(senderName);
            }
        }

        private void ShowWebcamViewerInternal(string senderName)
        {
            lock (_webcamViewerLock)
            {
                if (_webcamViewerForm != null && !_webcamViewerForm.IsDisposed)
                {
                    try
                    {
                        _webcamViewerForm.Invoke(new Action(() =>
                        {
                            _webcamViewerForm.WindowState = FormWindowState.Normal;
                            _webcamViewerForm.TopMost = true;
                            _webcamViewerForm.Activate();
                            _webcamViewerForm.TopMost = false;
                        }));
                    }
                    catch { }
                    return;
                }

                try
                {
                    _webcamViewerForm = new WebcamViewerForm(senderName);
                    //_webcamViewerForm.Invoke(new Action(() =>
                    //{
                    //    _webcamViewerForm.TopMost = true;
                    //    _webcamViewerForm.Activate();
                    //    _webcamViewerForm.TopMost = false;
                    //    _webcamViewerForm.BringToFront();
                    //}));

                    _webcamViewerForm.OnViewerClosed += () =>
                    {
                        Console.WriteLine($"[{DateTime.Now}] 📺 WebcamViewer closed by user");

                        lock (_webcamViewerLock)
                        {
                            _webcamViewerForm = null;
                        }

                        _avStateManager?.SetRemoteWebcamState(MediaStreamState.Stopped);
                        OnRemoteWebcamStateChanged?.Invoke(MediaStreamState.Stopped);

                        if (_audioPlaybackService != null)
                        {
                            try
                            {
                                _audioPlaybackService.StopAsync();
                                _audioPlaybackService.Dispose();
                                _audioPlaybackService = null;
                                _avStateManager?.SetRemoteAudioState(MediaStreamState.Stopped);
                                OnRemoteAudioStateChanged?.Invoke(MediaStreamState.Stopped);
                                Console.WriteLine($"[{DateTime.Now}] 🔊 Audio playback stopped");
                            }
                            catch { }
                        }

                        Task.Run(async () =>
                        {
                            try
                            {
                                if (_peerManager != null)
                                {
                                    await _peerManager.SendStopReceivingWebcamAsync();
                                    await _peerManager.SendStopReceivingAudioAsync();
                                }
                            }
                            catch { }
                        });
                    };

                    _webcamViewerForm.Show();
                    _webcamViewerForm.Invoke(new Action(() =>
                    {
                        _webcamViewerForm.WindowState = FormWindowState.Normal;
                        _webcamViewerForm.TopMost = true;
                        _webcamViewerForm.Activate();
                        _webcamViewerForm.TopMost = false;
                    }));

                    Console.WriteLine($"[{DateTime.Now}] ✅ WebcamViewerForm shown and activated");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error creating WebcamViewer: {ex.Message}");
                }
            }
        }

        private void CloseWebcamViewer()
        {
            WebcamViewerForm formToClose = null;

            lock (_webcamViewerLock)
            {
                if (_webcamViewerForm != null && !_webcamViewerForm.IsDisposed)
                {
                    formToClose = _webcamViewerForm;
                    _webcamViewerForm = null;
                }
            }

            if (formToClose != null)
            {
                try
                {
                    if (_uiContext != null)
                    {
                        _uiContext.Post(_ =>
                        {
                            try
                            {
                                if (!formToClose.IsDisposed)
                                {
                                    formToClose.Close();
                                }
                            }
                            catch { }
                        }, null);
                    }
                    else if (formToClose.InvokeRequired)
                    {
                        formToClose.BeginInvoke(new Action(() =>
                        {
                            try { formToClose.Close(); } catch { }
                        }));
                    }
                    else
                    {
                        formToClose.Close();
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // MUTE
        // ============================================================

        public void ToggleAudioPlaybackMute()
        {
            if (_audioPlaybackService != null)
            {
                _audioPlaybackService.IsMuted = !_audioPlaybackService.IsMuted;
                Console.WriteLine($"[{DateTime.Now}] 🔊 Mute:  {_audioPlaybackService.IsMuted}");
            }
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        private async Task CleanupAudioVideoServicesAsync()
        {
            Console.WriteLine($"[{DateTime.Now}] 🧹 Cleaning up A/V services");

            DetachPeerManagerAVEvents();

            if (_audioCaptureService != null)
            {
                await _audioCaptureService.StopAsync();
                _audioCaptureService.Dispose();
                _audioCaptureService = null;
            }

            if (_audioPlaybackService != null)
            {
                await _audioPlaybackService.StopAsync();
                _audioPlaybackService.Dispose();
                _audioPlaybackService = null;
            }

            if (_videoCaptureService != null)
            {
                await _videoCaptureService.StopAsync();
                _videoCaptureService.Dispose();
                _videoCaptureService = null;
            }

            CloseWebcamViewer();

            Console.WriteLine($"[{DateTime.Now}] ✅ A/V services cleaned up");
        }
    }
}