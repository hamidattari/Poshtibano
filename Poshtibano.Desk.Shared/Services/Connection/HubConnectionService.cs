using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Poshtibano.Common;
using Poshtibano.Desk.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services.Connection
{
    public class HubConnectionService : IDisposable
    {
        private HubConnection _hubConnection;
        private readonly string _signalingUrl;
        private readonly ConnectionStateManager _stateManager;
        private readonly ClientRole _role;
        private CancellationTokenSource _reconnectCts;
        private bool _isDisposed;
        private bool _isConnecting = false;
        private int _reconnectAttempts;
        private const int MaxReconnectAttempts = 10;

        // ✅ NEW: Handshake events
        public event Action OnRequestPasswordInfo;
        public event Action<string, string> OnRequestPassword;
        public event Action OnPasswordIncorrect;
        public event Action OnPasswordCorrect;
        public event Action<string, string> OnRequestAccessPermission;
        public event Action OnAccessDenied;
        public event Action<string> OnSessionEnded;

        public event Action<string> OnVerifyPassword;
        public event Action<ClientRole> OnChaneRoleRequest;

        // Existing events
        public event Action<string> OnMessageReceived;
        public event Action<string> OnSdpOfferReceived;
        public event Action<string> OnSdpAnswerReceived;
        public event Action<string> OnIceCandidateReceived;
        public event Action OnPeerDisconnected;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public HubConnectionService(string signalingUrl, ClientRole role, ConnectionStateManager stateManager)
        {
            _signalingUrl = signalingUrl ?? throw new ArgumentNullException(nameof(signalingUrl));
            _role = role;
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            _reconnectCts = new CancellationTokenSource();
        }

        public async Task ConnectAsync(string sessionId, string callerDisplyName, string callerSessionId)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(HubConnectionService));

            if (_isConnecting)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Hub already connecting, ignoring");
                return;
            }

            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Hub already connected");
                return;
            }

            _isConnecting = true;

            try
            {
                _stateManager.UpdateHubState(ConnectionStatus.Connecting, "Initiating connection");

                if (_hubConnection != null)
                {
                    try
                    {
                        await _hubConnection.StopAsync();
                        await _hubConnection.DisposeAsync();
                    }
                    catch { }
                    _hubConnection = null;
                }

                _hubConnection = new HubConnectionBuilder()
                    //.WithUrl(_signalingUrl)
                    .WithUrl(_signalingUrl, opt =>
                    {
                        opt.HttpMessageHandlerFactory = inner =>
                        {
                            if (inner is HttpClientHandler h)
                            {
                                h.UseProxy = false;
                            }
                            return inner;
                        };
                    })
                    .AddNewtonsoftJsonProtocol()
                    .WithAutomaticReconnect(new RetryPolicy())
                    .Build();

                RegisterHandlers();
                _hubConnection.Closed += OnHubConnectionClosed;
                _hubConnection.Reconnecting += OnHubReconnecting;
                _hubConnection.Reconnected += OnHubReconnected;

                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinSession", sessionId, _role, callerDisplyName, callerSessionId);

                _reconnectAttempts = 0;
                _stateManager.UpdateHubState(ConnectionStatus.Connected, "Hub connected successfully");

                Console.WriteLine($"[{DateTime.Now}] ✅ Hub connected as {_role} to session {sessionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Hub connection error: {ex.Message}");
                _stateManager.UpdateHubState(ConnectionStatus.Failed, ex.Message);

                if (_role == ClientRole.Agent && !_isDisposed)
                {
                    _ = Task.Run(() => RetryConnectionAsync(sessionId, callerDisplyName, callerSessionId));
                }

                throw;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private async Task RetryConnectionAsync(string sessionId, string callerName, string callerSessioIn)
        {
            while (!_isDisposed && _reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectAttempts++;
                var delay = Math.Min(1000 * (int)Math.Pow(2, _reconnectAttempts), 30000); // Exponential backoff, max 30s

                Console.WriteLine($"[{DateTime.Now}] Retry attempt {_reconnectAttempts}/{MaxReconnectAttempts} in {delay}ms");

                try
                {
                    await Task.Delay(delay, _reconnectCts.Token);
                    await ConnectAsync(sessionId, callerName, callerSessioIn);
                    return; // Success
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Retry failed: {ex.Message}");
                }
            }

            _stateManager.UpdateHubState(ConnectionStatus.Failed, "Max reconnection attempts reached");
        }

        private void RegisterHandlers()
        {
            _hubConnection.Remove("ReceiveMessage");
            _hubConnection.Remove("ReceiveSdpOffer");
            _hubConnection.Remove("ReceiveSdpAnswer");
            _hubConnection.Remove("ReceiveIceCandidate");
            _hubConnection.Remove("PeerDisconnected");
            _hubConnection.Remove("RequestPasswordInfo");
            _hubConnection.Remove("RequestPassword");
            _hubConnection.Remove("PasswordIncorrect");
            _hubConnection.Remove("RequestAccessPermission");
            _hubConnection.Remove("AccessDenied");
            _hubConnection.Remove("SessionEnded");
            _hubConnection.Remove("VerifyPassword");
            _hubConnection.Remove("ChangeRoleRequest");

            // ✅ NEW: Handshake handlers
            _hubConnection.On("RequestPasswordInfo", () =>
            {
                Console.WriteLine($"[{DateTime.Now}] 📋 Server requesting password info");
                OnRequestPasswordInfo?.Invoke();
            });

            _hubConnection.On<string, string>("RequestPassword", (caller, sessionId) =>
            {
                Console.WriteLine($"[{DateTime.Now}] 🔐 Server requesting password");
                OnRequestPassword?.Invoke(caller, sessionId);
            });

            _hubConnection.On("PasswordIncorrect", () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Password incorrect");
                OnPasswordIncorrect?.Invoke();
            });

            _hubConnection.On("PasswordCorrect", () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ✅ Password correct");
                OnPasswordCorrect?.Invoke();
            });

            _hubConnection.On<string, string>("RequestAccessPermission", (caller, sessionId) =>
            {
                Console.WriteLine($"[{DateTime.Now}] 🔐 Server requesting access permission");
                OnRequestAccessPermission?.Invoke(caller, sessionId);
            });

            _hubConnection.On("AccessDenied", () =>
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Access denied");
                OnAccessDenied?.Invoke();
            });

            _hubConnection.On<string>("SessionEnded", (reason) =>
            {
                Console.WriteLine($"[{DateTime.Now}] 📢 Session ended: {reason}");
                OnSessionEnded?.Invoke(reason);
            });

            // Existing handlers
            _hubConnection.On<string>("ReceiveMessage", msg => OnMessageReceived?.Invoke(msg));
            _hubConnection.On<string>("ReceiveSdpOffer", offer => OnSdpOfferReceived?.Invoke(offer));
            _hubConnection.On<string>("ReceiveSdpAnswer", answer => OnSdpAnswerReceived?.Invoke(answer));
            _hubConnection.On<string>("ReceiveIceCandidate", candidate => OnIceCandidateReceived?.Invoke(candidate));
            _hubConnection.On("PeerDisconnected", () => OnPeerDisconnected?.Invoke());

            _hubConnection.On<string>("VerifyPassword", password =>
            {
                Console.WriteLine($"[{DateTime.Now}] 🔐 Received password verification request: {password}");
                OnVerifyPassword?.Invoke(password);
            });

            _hubConnection.On<ClientRole>("ChangeRoleRequest", (role) =>
            {
                Console.WriteLine($"[{DateTime.Now}] 📋 Server change role");
                OnChaneRoleRequest?.Invoke(role);
            });
        }

        private Task OnHubConnectionClosed(Exception error)
        {
            if (_isDisposed) return Task.CompletedTask;

            Console.WriteLine($"[{DateTime.Now}] Hub connection closed: {error?.Message}");
            _stateManager.UpdateHubState(ConnectionStatus.Disconnected, error?.Message ?? "Connection closed");

            // For Agent mode, keep trying to reconnect
            if (_role == ClientRole.Agent && !_isDisposed)
            {
                _stateManager.UpdateHubState(ConnectionStatus.Reconnecting, "Attempting reconnection");
            }

            return Task.CompletedTask;
        }

        private Task OnHubReconnecting(Exception error)
        {
            Console.WriteLine($"[{DateTime.Now}] Hub reconnecting: {error?.Message}");
            _stateManager.UpdateHubState(ConnectionStatus.Reconnecting, "Reconnecting to hub");
            return Task.CompletedTask;
        }

        private Task OnHubReconnected(string connectionId)
        {
            OnSessionEnded?.Invoke("reconnected");

            Console.WriteLine($"[{DateTime.Now}] Hub reconnected with ID: {connectionId}");
            _stateManager.UpdateHubState(ConnectionStatus.Connected, "Reconnected successfully");
            _reconnectAttempts = 0;
            return Task.CompletedTask;
        }

        // ✅ NEW: Handshake methods
        public async Task SendPasswordInfoAsync(string sessionId, bool hasPassword)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SubmitPasswordInfo", sessionId, hasPassword);
            Console.WriteLine($"[{DateTime.Now}] 📤 Password info sent: hasPassword={hasPassword}");
        }

        public async Task SendPasswordAsync(string sessionId, string password)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SubmitPassword", sessionId, password);
            Console.WriteLine($"[{DateTime.Now}] 📤 Password submitted");
        }

        public async Task SendPasswordVerificationAsync(string sessionId, bool isCorrect)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SubmitPasswordVerification", sessionId, isCorrect);
            Console.WriteLine($"[{DateTime.Now}] 📤 Password verification sent:  isCorrect={isCorrect}");
        }

        public async Task SendAccessResponseAsync(string sessionId, bool allowed)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SubmitAccessResponse", sessionId, allowed);
            Console.WriteLine($"[{DateTime.Now}] 📤 Access response sent: allowed={allowed}");
        }

        // Existing methods
        public async Task SendMessageAsync(string sessionId, string message)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SendMessage", sessionId, message);
        }

        public async Task SendSdpOfferAsync(string sessionId, string offer)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SendSdpOffer", sessionId, offer);
        }

        public async Task SendSdpAnswerAsync(string sessionId, string answer)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SendSdpAnswer", sessionId, answer);
        }

        public async Task SendIceCandidateAsync(string sessionId, string candidate)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to hub");
            await _hubConnection.InvokeAsync("SendIceCandidate", sessionId, candidate);
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                Console.WriteLine($"[{DateTime.Now}] 🔌 Disconnecting hub...");

                try
                {
                    _hubConnection.Closed -= OnHubConnectionClosed;
                    _hubConnection.Reconnecting -= OnHubReconnecting;
                    _hubConnection.Reconnected -= OnHubReconnected;

                    await _hubConnection.StopAsync();
                    Console.WriteLine($"[{DateTime.Now}] ✅ Hub stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Error stopping hub: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Console.WriteLine($"[{DateTime.Now}] 🗑️ Disposing HubConnectionService");

            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();

            if (_hubConnection != null)
            {
                _hubConnection.Closed -= OnHubConnectionClosed;
                _hubConnection.Reconnecting -= OnHubReconnecting;
                _hubConnection.Reconnected -= OnHubReconnected;

                try
                {
                    _hubConnection.StopAsync().Wait(2000);
                    _hubConnection.DisposeAsync().AsTask().Wait(2000);
                }
                catch { }

                _hubConnection = null;
            }

            // Clear all events
            OnMessageReceived = null;
            OnSdpOfferReceived = null;
            OnSdpAnswerReceived = null;
            OnIceCandidateReceived = null;
            OnPeerDisconnected = null;
            OnRequestPasswordInfo = null;
            OnRequestPassword = null;
            OnPasswordIncorrect = null;
            OnRequestAccessPermission = null;
            OnAccessDenied = null;
            OnSessionEnded = null;

            Console.WriteLine($"[{DateTime.Now}] ✅ HubConnectionService disposed");
        }

        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                var delay = retryContext.PreviousRetryCount == 0
                    ? TimeSpan.Zero
                    : TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryContext.PreviousRetryCount), 30));

                Console.WriteLine($"[{DateTime.Now}] Hub retry #{retryContext.PreviousRetryCount + 1} after {delay.TotalSeconds}s");
                return delay;
            }
        }
    }
}