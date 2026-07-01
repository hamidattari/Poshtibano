using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poshtibano.Common;
using SIPSorcery.Net;

namespace Poshtibano.Desk.Shared
{
    public partial class PeerConnectionManager
    {
        private RTCDataChannel _audioChannel;    // id=4: Audio data
        private RTCDataChannel _webcamChannel;   // id=5: Webcam data

        // ============================================================
        // AUDIO/VIDEO DATA EVENTS
        // ============================================================
        public event Action<byte[], int, int, int, long, uint> OnAudioDataReceived;
        public event Action<byte[], byte[], int, int, int, int, long, uint> OnWebcamDataReceived;

        // ============================================================
        // AVAILABILITY AND ACTIVE STATE EVENTS
        // ============================================================
        public event Action<bool, bool> OnTheirMicophoneAndWebcamAvailabilityReceived;
        public event Action<bool> OnTheirMicrophoneActiveReceived;
        public event Action<bool> OnTheirWebcamActiveReceived;

        // ============================================================
        // PERMISSION REQUEST EVENTS
        // ============================================================
        public event Action<MediaPermissionRequest> OnTheyWantToSendAudioReceived;
        public event Action<MediaPermissionRequest> OnTheyWantToSendWebcamReceived;
        public event Action<MediaPermissionRequest> OnTheyWantToReceiveMyAudioReceived;
        public event Action<MediaPermissionRequest> OnTheyWantToReceiveMyWebcamReceived;

        // ============================================================
        // PERMISSION RESPONSE EVENTS
        // ============================================================
        public event Action<bool> OnMyAudioSendResponseReceived;
        public event Action<bool> OnMyWebcamSendResponseReceived;
        public event Action<bool> OnTheirAudioResponseReceived;
        public event Action<string, bool> OnTheirWebcamResponseReceived;

        // ============================================================
        // STOP SIGNAL EVENTS
        // ============================================================
        public event Action OnStopAudioReceived;
        public event Action OnStopWebcamReceived;

        // ============================================================
        // AUDIO CHANNEL SETUP
        // ============================================================
        private void SetupAudioChannel()
        {
            // ────────────────────────────────────────────────────────
            // CHANNEL 4: Audio
            // ────────────────────────────────────────────────────────
            var audioInit = new RTCDataChannelInit
            {
                ordered = false,
                maxPacketLifeTime = 500,
                //maxRetransmits = 0,
                negotiated = true,
                id = 4
            };
            _audioChannel = _peerConnection.createDataChannel("audio", audioInit).Result;

            _audioChannel.onopen += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ✅ [audio] Channel OPEN");
            };

            _audioChannel.onclose += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [audio] Channel CLOSED");
            };

            _audioChannel.onerror += (error) =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [audio] Channel ERROR: {error}");
            };

            _audioChannel.onmessage += (label, protocol, data) =>
            {
                if (data == null || data.Length < 20) return;

                try
                {
                    // Parse audio packet:
                    // [sampleRate:4][channels:4][bitsPerSample:4][timestampTicks:8][sequenceNumber:4][audioData:rest]
                    int offset = 0;
                    int sampleRate = BitConverter.ToInt32(data, offset); offset += 4;
                    int channels = BitConverter.ToInt32(data, offset); offset += 4;
                    int bitsPerSample = BitConverter.ToInt32(data, offset); offset += 4;
                    long timestampTicks = BitConverter.ToInt64(data, offset); offset += 8;
                    uint sequenceNumber = BitConverter.ToUInt32(data, offset); offset += 4;

                    byte[] audioData = new byte[data.Length - offset];
                    Buffer.BlockCopy(data, offset, audioData, 0, audioData.Length);

                    OnAudioDataReceived?.Invoke(audioData, sampleRate, channels, bitsPerSample, timestampTicks, sequenceNumber);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error parsing audio data: {ex.Message}");
                }
            };
        }

        // ============================================================
        // WEBCAM CHANNEL SETUP
        // ============================================================
        private void SetupWebcamChannel()
        {
            // ────────────────────────────────────────────────────────
            // CHANNEL 5: Webcam
            // ────────────────────────────────────────────────────────
            var webcamInit = new RTCDataChannelInit
            {
                ordered = false,
                maxPacketLifeTime = 500,
                //maxRetransmits = 0,
                negotiated = true,
                id = 5
            };
            _webcamChannel = _peerConnection.createDataChannel("webcam", webcamInit).Result;

            _webcamChannel.onopen += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ✅ [webcam] Channel OPEN");
            };

            _webcamChannel.onclose += () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [webcam] Channel CLOSED");
            };

            _webcamChannel.onerror += (error) =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ [webcam] Channel ERROR: {error}");
            };

            _webcamChannel.onmessage += (label, protocol, data) =>
            {
                if (data == null || data.Length < 28) return;

                try
                {
                    // Parse webcam packet:
                    // [width:4][height:4][audioSampleRate:4][audioChannels:4][timestampTicks:8][sequenceNumber:4][videoDataLen:4][videoData][audioData]
                    int offset = 0;
                    int width = BitConverter.ToInt32(data, offset); offset += 4;
                    int height = BitConverter.ToInt32(data, offset); offset += 4;
                    int audioSampleRate = BitConverter.ToInt32(data, offset); offset += 4;
                    int audioChannels = BitConverter.ToInt32(data, offset); offset += 4;
                    long timestampTicks = BitConverter.ToInt64(data, offset); offset += 8;
                    uint sequenceNumber = BitConverter.ToUInt32(data, offset); offset += 4;
                    int videoDataLen = BitConverter.ToInt32(data, offset); offset += 4;

                    byte[] videoData = new byte[videoDataLen];
                    Buffer.BlockCopy(data, offset, videoData, 0, videoDataLen);
                    offset += videoDataLen;

                    byte[] audioData = null;
                    if (offset < data.Length)
                    {
                        audioData = new byte[data.Length - offset];
                        Buffer.BlockCopy(data, offset, audioData, 0, audioData.Length);
                    }

                    OnWebcamDataReceived?.Invoke(videoData, audioData, width, height, audioSampleRate, audioChannels, timestampTicks, sequenceNumber);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error parsing webcam data: {ex.Message}");
                }
            };
        }

        // ============================================================
        // SEND AUDIO DATA
        // Same signature as original for SessionCoordinator compatibility
        // ============================================================
        public void SendAudioData(ClientRole role, byte[] audioData, int sampleRate, int channels,
            int bitsPerSample, long timestampTicks, uint sequenceNumber)
        {
            if (_audioChannel?.readyState != RTCDataChannelState.open)
                return;

            try
            {
                // Pack audio data:
                // [sampleRate:4][channels:4][bitsPerSample:4][timestampTicks:8][sequenceNumber:4][audioData:rest]
                int headerSize = 4 + 4 + 4 + 8 + 4;
                byte[] packet = new byte[headerSize + audioData.Length];
                int offset = 0;

                BitConverter.GetBytes(sampleRate).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(channels).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(bitsPerSample).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(timestampTicks).CopyTo(packet, offset); offset += 8;
                BitConverter.GetBytes(sequenceNumber).CopyTo(packet, offset); offset += 4;
                Buffer.BlockCopy(audioData, 0, packet, offset, audioData.Length);

                _audioChannel.send(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Audio send error: {ex.Message}");
            }
        }

        // ============================================================
        // SEND WEBCAM DATA
        // ============================================================
        public void SendWebcamData(ClientRole role, byte[] videoData, byte[] audioData, int width, int height,
            int audioSampleRate, int audioChannels, long timestampTicks, uint sequenceNumber)
        {
            if (_webcamChannel?.readyState != RTCDataChannelState.open)
                return;

            try
            {
                // Pack webcam data:
                // [width:4][height:4][audioSampleRate:4][audioChannels:4][timestampTicks:8][sequenceNumber:4][videoDataLen:4][videoData][audioData]
                int audioLen = audioData?.Length ?? 0;
                int headerSize = 4 + 4 + 4 + 4 + 8 + 4 + 4;
                byte[] packet = new byte[headerSize + videoData.Length + audioLen];
                int offset = 0;

                BitConverter.GetBytes(width).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(height).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(audioSampleRate).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(audioChannels).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(timestampTicks).CopyTo(packet, offset); offset += 8;
                BitConverter.GetBytes(sequenceNumber).CopyTo(packet, offset); offset += 4;
                BitConverter.GetBytes(videoData.Length).CopyTo(packet, offset); offset += 4;
                Buffer.BlockCopy(videoData, 0, packet, offset, videoData.Length); offset += videoData.Length;

                if (audioData != null && audioData.Length > 0)
                {
                    Buffer.BlockCopy(audioData, 0, packet, offset, audioData.Length);
                }

                _webcamChannel.send(packet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Webcam send error: {ex.Message}");
            }
        }

        // ============================================================
        // AVAILABILITY AND ACTIVE STATE METHODS
        // ============================================================
        public async Task SendWebcamAndMicrophoneAvailability(bool hasMicrophone, bool hasWebcam)
        {
            try
            {
                var payload = new
                {
                    type = "media_availability",
                    hasMicrophone,
                    hasWebcam
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Media availability sent: mic={hasMicrophone}, cam={hasWebcam}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending availability: {ex.Message}");
            }
        }

        public async Task SetMyMicrophoneActiveAsync(bool active)
        {
            try
            {
                var payload = new
                {
                    type = "my_microphone_active",
                    active
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 My microphone active: {active}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SetMyWebcamActiveAsync(bool active)
        {
            try
            {
                var payload = new
                {
                    type = "my_webcam_active",
                    active
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 My webcam active: {active}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // REQUEST METHODS
        // ============================================================
        public async Task SendMyAudioRequestAsync(string name, ClientRole role)
        {
            try
            {
                var payload = new
                {
                    type = "request_send_my_audio",
                    requesterName = name,
                    requesterRole = role.ToString()
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Request to send my audio");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendMyWebcamRequestAsync(string name, ClientRole role)
        {
            try
            {
                var payload = new
                {
                    type = "request_send_my_webcam",
                    requesterName = name,
                    requesterRole = role.ToString()
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Request to send my webcam");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendTheirAudioRequestAsync(string name, ClientRole role)
        {
            try
            {
                var payload = new
                {
                    type = "request_receive_their_audio",
                    requesterName = name,
                    requesterRole = role.ToString()
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Request to receive their audio");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendTheirWebcamRequestAsync(string name, ClientRole role)
        {
            try
            {
                var payload = new
                {
                    type = "request_receive_their_webcam",
                    mediaType = MediaType.Webcam.ToString(),
                    requestType = MediaRequestType.IWantToReceive.ToString(),
                    requesterName = name,
                    requesterRole = role.ToString(),
                    timestampTicks = DateTime.UtcNow.Ticks
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Request to receive their webcam");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // RESPONSE METHODS
        // ============================================================
        public async Task SendTheirAudioSendResponseAsync(bool allowed)
        {
            try
            {
                var payload = new
                {
                    type = "response_their_audio_send",
                    allowed
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Response to their audio send: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendTheirWebcamSendResponseAsync(bool allowed)
        {
            try
            {
                var payload = new
                {
                    type = "response_their_webcam_send",
                    allowed
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Response to their webcam send: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendMyAudioReceiveResponseAsync(bool allowed)
        {
            try
            {
                var payload = new
                {
                    type = "response_my_audio_receive",
                    allowed
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Response to receive my audio: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendMyWebcamReceiveResponseAsync(string name, bool allowed)
        {
            try
            {
                var payload = new
                {
                    type = "response_my_webcam_receive",
                    name,
                    allowed
                };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Response to receive my webcam: {allowed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // STOP METHODS
        // ============================================================
        public async Task SendStopMyAudioAsync()
        {
            try
            {
                var payload = new { type = "stop_my_audio" };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Stop my audio sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendStopMyWebcamAsync()
        {
            try
            {
                var payload = new { type = "stop_my_webcam" };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Stop my webcam sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendStopReceivingAudioAsync()
        {
            try
            {
                var payload = new { type = "stop_receiving_audio" };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Stop receiving audio sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        public async Task SendStopReceivingWebcamAsync()
        {
            try
            {
                var payload = new { type = "stop_receiving_webcam" };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Stop receiving webcam sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error: {ex.Message}");
            }
        }

        // ============================================================
        // HUB MESSAGE HANDLING FOR AUDIO/VIDEO
        // Called from HandleHubMessage in main class
        // ============================================================
        private bool HandleAudioVideoHubMessage(string type, JObject root)
        {
            try
            {
                switch (type)
                {
                    case "media_availability":
                        {
                            bool hasMic = root["hasMicrophone"]?.Value<bool>() ?? false;
                            bool hasCam = root["hasWebcam"]?.Value<bool>() ?? false;
                            OnTheirMicophoneAndWebcamAvailabilityReceived?.Invoke(hasMic, hasCam);
                            Console.WriteLine($"[{DateTime.Now}] 📥 Media availability: mic={hasMic}, cam={hasCam}");
                            return true;
                        }

                    case "my_microphone_active":
                        {
                            bool active = root["active"]?.Value<bool>() ?? false;
                            OnTheirMicrophoneActiveReceived?.Invoke(active);
                            Console.WriteLine($"[{DateTime.Now}] 📥 Their microphone active: {active}");
                            return true;
                        }

                    case "my_webcam_active":
                        {
                            bool active = root["active"]?.Value<bool>() ?? false;
                            OnTheirWebcamActiveReceived?.Invoke(active);
                            Console.WriteLine($"[{DateTime.Now}] 📥 Their webcam active: {active}");
                            return true;
                        }

                    case "request_send_my_audio":
                        {
                            string roleStr = root["requesterRole"]?.Value<string>() ?? "Controller";
                            ClientRole role = Enum.TryParse<ClientRole>(roleStr, out var parsedRole) ? parsedRole : ClientRole.Controller;

                            var request = new MediaPermissionRequest
                            {
                                RequesterName = root["requesterName"]?.Value<string>() ?? "",
                                RequesterRole = role
                            };
                            OnTheyWantToSendAudioReceived?.Invoke(request);
                            Console.WriteLine($"[{DateTime.Now}] 📥 They want to send their audio");
                            return true;
                        }

                    case "request_send_my_webcam":
                        {
                            string roleStr = root["requesterRole"]?.Value<string>() ?? "Controller";
                            ClientRole role = Enum.TryParse<ClientRole>(roleStr, out var parsedRole) ? parsedRole : ClientRole.Controller;

                            var request = new MediaPermissionRequest
                            {
                                RequesterName = root["requesterName"]?.Value<string>() ?? "",
                                RequesterRole = role
                            };
                            OnTheyWantToSendWebcamReceived?.Invoke(request);
                            Console.WriteLine($"[{DateTime.Now}] 📥 They want to send their webcam");
                            return true;
                        }

                    case "request_receive_their_audio":
                        {
                            string roleStr = root["requesterRole"]?.Value<string>() ?? "Controller";
                            ClientRole role = Enum.TryParse<ClientRole>(roleStr, out var parsedRole) ? parsedRole : ClientRole.Controller;

                            var request = new MediaPermissionRequest
                            {
                                RequesterName = root["requesterName"]?.Value<string>() ?? "",
                                RequesterRole = role
                            };
                            OnTheyWantToReceiveMyAudioReceived?.Invoke(request);
                            Console.WriteLine($"[{DateTime.Now}] 📥 They want to receive my audio");
                            return true;
                        }

                    case "request_receive_their_webcam":
                        {
                            string roleStr = root["requesterRole"]?.Value<string>() ?? "Controller";
                            ClientRole role = Enum.TryParse<ClientRole>(roleStr, out var parsedRole) ? parsedRole : ClientRole.Controller;

                            var request = new MediaPermissionRequest
                            {
                                RequesterName = root["requesterName"]?.Value<string>() ?? "",
                                RequesterRole = role
                            };
                            OnTheyWantToReceiveMyWebcamReceived?.Invoke(request);
                            Console.WriteLine($"[{DateTime.Now}] 📥 They want to receive my webcam");
                            return true;
                        }

                    case "response_their_audio_send":
                        {
                            bool allowed = root["allowed"]?.Value<bool>() ?? false;
                            OnMyAudioSendResponseReceived?.Invoke(allowed);
                            Console.WriteLine($"[{DateTime.Now}] 📥 My audio send response: {allowed}");
                            return true;
                        }

                    case "response_their_webcam_send":
                        {
                            bool allowed = root["allowed"]?.Value<bool>() ?? false;
                            OnMyWebcamSendResponseReceived?.Invoke(allowed);
                            Console.WriteLine($"[{DateTime.Now}] 📥 My webcam send response: {allowed}");
                            return true;
                        }

                    case "response_my_audio_receive":
                        {
                            bool allowed = root["allowed"]?.Value<bool>() ?? false;
                            OnTheirAudioResponseReceived?.Invoke(allowed);
                            Console.WriteLine($"[{DateTime.Now}] 📥 Their audio response: {allowed}");
                            return true;
                        }

                    case "response_my_webcam_receive":
                        {
                            string name = root["name"]?.Value<string>() ?? "";
                            bool allowed = root["allowed"]?.Value<bool>() ?? false;
                            OnTheirWebcamResponseReceived?.Invoke(name, allowed);
                            Console.WriteLine($"[{DateTime.Now}] 📥 Their webcam response: {allowed}");
                            return true;
                        }

                    case "stop_my_audio":
                        OnStopAudioReceived?.Invoke();
                        Console.WriteLine($"[{DateTime.Now}] 📥 Stop audio received");
                        return true;

                    case "stop_my_webcam":
                        OnStopWebcamReceived?.Invoke();
                        Console.WriteLine($"[{DateTime.Now}] 📥 Stop webcam received");
                        return true;

                    case "stop_receiving_audio":
                        OnStopAudioReceived?.Invoke();
                        Console.WriteLine($"[{DateTime.Now}] 📥 Stop receiving audio");
                        return true;

                    case "stop_receiving_webcam":
                        OnStopWebcamReceived?.Invoke();
                        Console.WriteLine($"[{DateTime.Now}] 📥 Stop receiving webcam");
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error handling A/V hub message: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // CLEAR AUDIO/VIDEO EVENTS (called from Dispose)
        // ============================================================
        private void ClearAudioVideoEvents()
        {
            OnAudioDataReceived = null;
            OnWebcamDataReceived = null;
            OnTheirMicophoneAndWebcamAvailabilityReceived = null;
            OnTheirMicrophoneActiveReceived = null;
            OnTheirWebcamActiveReceived = null;
            OnTheyWantToSendAudioReceived = null;
            OnTheyWantToSendWebcamReceived = null;
            OnTheyWantToReceiveMyAudioReceived = null;
            OnTheyWantToReceiveMyWebcamReceived = null;
            OnMyAudioSendResponseReceived = null;
            OnMyWebcamSendResponseReceived = null;
            OnTheirAudioResponseReceived = null;
            OnTheirWebcamResponseReceived = null;
            OnStopAudioReceived = null;
            OnStopWebcamReceived = null;
        }
    }
}