using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poshtibano.Common;
using Poshtibano.Desk.Services.Connection;

namespace Poshtibano.Desk.Shared
{
    public partial class PeerConnectionManager
    {
        // ============================================================
        // HANDSHAKE EVENTS
        // ============================================================
        public event Action OnRequestPasswordInfo;
        public event Action<string, string> OnRequestPassword;
        public event Action<string> OnVerifyPassword;
        public event Action OnPasswordIncorrect;
        public event Action OnPasswordCorrect;
        public event Action<string, string> OnRequestAccessPermission;
        public event Action OnAccessDenied;
        public event Action<string> OnSessionEnded;
        public event Action<ClientRole> OnChangeRoleRequest;

        // ============================================================
        // MONITOR EVENTS
        // ============================================================
        public event Action<List<MonitorInfo>> OnMonitorListReceived;
        public event Action<int, bool> OnMonitorStatusUpdateReceived;
        public event Action<int> OnMonitorChangedReceived;
        public event Action<int> OnMonitorSelectReceived;

        // ============================================================
        // OTHER EVENTS
        // ============================================================
        public event Action<string, string> OnRemoteMouseActionDenied;
        public event Action<string, string, string> OnInvitationRequest;
        public event Action<string, string, string, bool> OnInvitationResponse;

        // ============================================================
        // HUB MESSAGE HANDLING
        // ============================================================
        private void HandleHubMessage(string message)
        {
            try
            {
                var root = JObject.Parse(message);
                var type = root["type"]?.Value<string>() ?? "n/a";

                Console.WriteLine($"[{DateTime.Now}] 📨 Hub Message: {type}");

                if (HandleAudioVideoHubMessage(type, root))
                {
                    return;
                }

                switch (type)
                {
                    case "session_end":
                        string reason = root["reason"]?.Value<string>() ?? "n/a";
                        OnSessionEnded?.Invoke(reason);
                        break;

                    case "invitation_response":
                        string responseInviteeId = root["inviteeId"]?.Value<string>() ?? "n/a";
                        string responseInviterId = root["inviterId"]?.Value<string>() ?? "n/a";
                        string responseInviteeName = root["inviteeName"]?.Value<string>() ?? "n/a";
                        bool responseAccept = root["accept"]?.Value<bool>() ?? false;
                        OnInvitationResponse?.Invoke(responseInviteeId, responseInviterId, responseInviteeName, responseAccept);
                        break;

                    case "invitation_request":
                        string inviteeId = root["inviteeId"]?.Value<string>() ?? "n/a";
                        string inviterId = root["inviterId"]?.Value<string>() ?? "n/a";
                        string inviterName = root["inviterName"]?.Value<string>() ?? "n/a"; ;
                        OnInvitationRequest?.Invoke(inviteeId, inviterId, inviterName);
                        break;

                    case "mouse_action_denied":
                        string name = root["name"]?.Value<string>() ?? "n/a";
                        string process = root["process"]?.Value<string>() ?? "n/a";
                        OnRemoteMouseActionDenied?.Invoke(name, process);
                        break;

                    case "monitor_list":
                        var monitorsToken = root["monitors"];

                        List<MonitorInfo>? monitors = null;

                        if (monitorsToken is JArray array)
                        {
                            monitors = array.ToObject<List<MonitorInfo>>();
                        }
                        break;

                    case "monitor_status_update":
                        int msIndex = root["monitorIndex"]?.Value<int>() ?? 0;
                        bool active = root["isActive"]?.Value<bool>() ?? false;
                        OnMonitorStatusUpdateReceived?.Invoke(msIndex, active);
                        break;

                    case "monitor_changed":
                        int chIndex = root["monitorIndex"]?.Value<int>() ?? 0;
                        OnMonitorChangedReceived?.Invoke(chIndex);
                        break;

                    case "monitor_select":
                        int selIndex = root["monitorIndex"]?.Value<int>() ?? 0;
                        OnMonitorSelectReceived?.Invoke(selIndex);
                        break;

                    case "sessionReady":
                        HandleSessionReady();
                        break;

                    case "sessionEnded":
                        HandleSessionEndedMessage();
                        break;

                    case "client_rejoin":
                        Console.WriteLine($"[{DateTime.Now}] ℹ️ Client rejoin notification received");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error handling hub message: {ex.Message}");
            }
        }

        // ============================================================
        // HUB COMMUNICATION METHODS
        // ============================================================
        public async Task SendRejoinNotificationAsync()
        {
            if (!_hubService.IsConnected)
                throw new InvalidOperationException("Hub not connected");

            try
            {
                var payload = new { type = "client_rejoin", role = _role.ToString() };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
                Console.WriteLine($"[{DateTime.Now}] 📤 Rejoin notification sent: {_role}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending rejoin: {ex.Message}");
                throw;
            }
        }

        // ============================================================
        // SESSION HANDLERS
        // ============================================================
        private void HandleSessionEnded(string reason)
        {
            Console.WriteLine($"[{DateTime.Now}] 📢 HandleSessionEnded called");
            OnSessionEnded?.Invoke(reason);
        }

        public async Task SendSessionEnd(string reason)
        {
            try
            {
                var payload = new { type = "session_end", reason = reason };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending SessionEnd: {ex.Message}");
            }
        }

        private void HandleSessionReady()
        {
            if (_role == ClientRole.Agent && !_sessionReadyFired)
            {
                _sessionReadyFired = true;
                _stateManager.UpdateHubState(ConnectionStatus.Connected, "Session ready");
                OnSessionReady?.Invoke();
                Console.WriteLine($"[{DateTime.Now}] ✅ Session ready (Agent)");
            }
            else if (_role == ClientRole.Agent && _sessionReadyFired)
            {
                Console.WriteLine($"[{DateTime.Now}] 🔄 Session ready (Controller rejoined), allowing reconnect");
                _sessionReadyFired = true;
                OnSessionReady?.Invoke();
            }
            else if (_role == ClientRole.Controller)
            {
                if (!_sessionReadyFired)
                {
                    _sessionReadyFired = true;
                    _stateManager.UpdateHubState(ConnectionStatus.Connected, "Session ready");
                    OnSessionReady?.Invoke();
                    Console.WriteLine($"[{DateTime.Now}] ✅ Session ready (Controller)");
                }
            }
        }

        private void HandleSessionEndedMessage()
        {
            _stateManager.UpdateHubState(ConnectionStatus.Disconnected, "Session ended");
            Console.WriteLine($"[{DateTime.Now}] 📢 Session ended");

            _sessionReadyFired = false;

            if (_role == ClientRole.Agent)
            {
                Console.WriteLine($"[{DateTime.Now}] ℹ️ Agent: Session ended, waiting for new Controller");
                _stateManager.UpdatePeerState(ConnectionStatus.Disconnected, "Waiting for new Controller");
            }
            else
            {
                _ = Task.Run(async () => await ResetPeerConnectionAsync());
            }
        }

        // ============================================================
        // CHANGEROLE HANDLERS & METHODS
        // ============================================================
        public void ChangeRole(ClientRole role)
        {
            _role = role;
        }

        private void HandleChaneRoleRequest(ClientRole role)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 HandleChaneRoleRequest called");
            OnChangeRoleRequest?.Invoke(role);
        }

        // ============================================================
        // HANDSHAKE HANDLERS
        // ============================================================
        private void HandleVerifyPassword(string password)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 HandleVerifyPassword called");
            OnVerifyPassword?.Invoke(password);
        }

        private void HandleRequestPasswordInfo()
        {
            Console.WriteLine($"[{DateTime.Now}] 📋 HandleRequestPasswordInfo called");
            OnRequestPasswordInfo?.Invoke();
        }

        private void HandleRequestPassword(string caller, string sessionId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 HandleRequestPassword called");
            OnRequestPassword?.Invoke(caller, sessionId);
        }

        private void HandlePasswordIncorrect()
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ HandlePasswordIncorrect called");
            OnPasswordIncorrect?.Invoke();
        }

        private void HandlePasswordCorrect()
        {
            Console.WriteLine($"[{DateTime.Now}] ✅ HandlePasswordCorrect called");
            OnPasswordCorrect?.Invoke();
        }

        private void HandleRequestAccessPermission(string caller, string sessionId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🔐 HandleRequestAccessPermission called");
            OnRequestAccessPermission?.Invoke(caller, sessionId);
        }

        // ============================================================
        // INVITATION HANDLERS & METHODS
        // ============================================================

        public async Task SendInvitationResponse(string sessionId, string inviteeId, string inviterId, string inviteeName, bool accept)
        {
            try
            {
                var payload = new
                {
                    type = "invitation_response",
                    inviteeId,
                    inviterId,
                    inviteeName,
                    accept
                };
                await _hubService.SendMessageAsync(sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending invitation response: {ex.Message}");
            }
        }

        public async Task SendInvitationToRemote(string sessionId, string inviteeId, string inviterId, string inviterName)
        {
            try
            {
                var payload = new
                {
                    type = "invitation_request",
                    inviteeId,
                    inviterId,
                    inviterName
                };
                await _hubService.SendMessageAsync(sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending invitation: {ex.Message}");
            }
        }


        // ============================================================
        // MOUSE ACCESS HANDLERS & METHODS
        // ============================================================
        public async Task SendMouseActionOnProcessDenied(string name, string process)
        {
            try
            {
                var payload = new { type = "mouse_action_denied", name, process };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Mouse action denied error: {ex.Message}");
            }
        }

        private void HandleAccessDenied()
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ HandleAccessDenied called");
            OnAccessDenied?.Invoke();
        }
        // ============================================================
        // MONITOR METHODS
        // ============================================================
        public async Task SendMonitorList(List<MonitorInfo> monitors)
        {
            try
            {
                var payload = new { type = "monitor_list", monitors };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending monitor list: {ex.Message}");
            }
        }

        public async Task SendMonitorStatusUpdate(int monitorIndex, bool isActive)
        {
            try
            {
                var payload = new { type = "monitor_status_update", monitorIndex, isActive };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending monitor status: {ex.Message}");
            }
        }

        public async Task SendMonitorChanged(int monitorIndex)
        {
            try
            {
                var payload = new { type = "monitor_changed", monitorIndex };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending monitor change: {ex.Message}");
            }
        }

        public async Task SendMonitorSelect(int monitorIndex)
        {
            try
            {
                var payload = new { type = "monitor_select", monitorIndex };
                await _hubService.SendMessageAsync(_sessionId, JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error sending monitor selection: {ex.Message}");
            }
        }

        // ============================================================
        // PASSWORD AND ACCESS METHODS
        // ============================================================
        public async Task SendPasswordInfoAsync(bool hasPassword)
        {
            await _hubService.SendPasswordInfoAsync(_sessionId, hasPassword);
        }

        public async Task SendPasswordAsync(string password)
        {
            await _hubService.SendPasswordAsync(_sessionId, password);
        }

        public async Task SendPasswordVerificationAsync(bool isCorrect)
        {
            await _hubService.SendPasswordVerificationAsync(_sessionId, isCorrect);
        }

        public async Task SendAccessResponseAsync(bool allowed)
        {
            await _hubService.SendAccessResponseAsync(_sessionId, allowed);
        }
    }
}