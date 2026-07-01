using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services.Connection
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        AccessDenied,
        AccessGranted,
        PasswordCorrect,
        PasswordIncorrect,
        WaitingPermission,
        Failed,
        SessionReady,
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStatus OldState { get; set; }
        public ConnectionStatus NewState { get; set; }
        public string Reason { get; set; }
    }

    public class ConnectionStateManager : IDisposable
    {
        private ConnectionStatus _hubState = ConnectionStatus.Disconnected;
        private ConnectionStatus _peerState = ConnectionStatus.Disconnected;
        private readonly object _stateLock = new object();
        private readonly SynchronizationContext _uiContext;

        public event EventHandler<ConnectionStateChangedEventArgs> HubStateChanged;
        public event EventHandler<ConnectionStateChangedEventArgs> PeerStateChanged;

        public ConnectionStatus HubState
        {
            get { lock (_stateLock) return _hubState; }
        }

        public ConnectionStatus PeerState
        {
            get { lock (_stateLock) return _peerState; }
        }

        public bool IsFullyConnected => HubState == ConnectionStatus.Connected && PeerState == ConnectionStatus.Connected;

        public ConnectionStateManager()
        {
            _uiContext = SynchronizationContext.Current;
        }

        public void UpdateHubState(ConnectionStatus newState, string reason = null)
        {
            ConnectionStatus oldState;
            lock (_stateLock)
            {
                if (_hubState == newState) return;
                oldState = _hubState;
                _hubState = newState;
            }

            Console.WriteLine($"[{DateTime.Now}] Hub State: {oldState} → {newState} ({reason ?? "No reason"})");
            RaiseEvent(HubStateChanged, oldState, newState, reason);
        }

        public void UpdatePeerState(ConnectionStatus newState, string reason = null)
        {
            ConnectionStatus oldState;
            lock (_stateLock)
            {
                if (_peerState == newState) return;
                oldState = _peerState;
                _peerState = newState;
            }

            Console.WriteLine($"[{DateTime.Now}] Peer State: {oldState} → {newState} ({reason ?? "No reason"})");
            RaiseEvent(PeerStateChanged, oldState, newState, reason);
        }

        private void RaiseEvent(EventHandler<ConnectionStateChangedEventArgs> handler,
            ConnectionStatus oldState, ConnectionStatus newState, string reason)
        {
            if (handler == null) return;

            var args = new ConnectionStateChangedEventArgs
            {
                OldState = oldState,
                NewState = newState,
                Reason = reason
            };

            if (_uiContext != null)
            {
                _uiContext.Post(_ => handler?.Invoke(this, args), null);
            }
            else
            {
                handler?.Invoke(this, args);
            }
        }

        public void Reset()
        {
            lock (_stateLock)
            {
                _hubState = ConnectionStatus.Disconnected;
                _peerState = ConnectionStatus.Disconnected;
            }
        }

        public void Dispose()
        {
            HubStateChanged = null;
            PeerStateChanged = null;
        }
    }
}