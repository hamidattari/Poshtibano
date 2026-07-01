using Poshtibano.Desk.Shared.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services
{
    /// <summary>
    /// Clipboard sharing integration for SessionCoordinator.
    /// Follows architecture: MainForm ↔ SessionCoordinator ↔ ClipboardSharingManager ↔ PeerConnectionManager
    /// </summary>
    public partial class SessionCoordinator
    {
        // Clipboard services
        private ClipboardSharingManager _clipboardSharingManager;
        private ClipboardMonitor _clipboardMonitor;

        // ✅ Clipboard events (exposed to MainForm)
        public event Action<string> OnClipboardRemoteTextReceived;
        public event Action<List<string>> OnClipboardRemoteFilesReceived;
        public event Action<string> OnClipboardStatusChanged;
        public event Action<string> OnClipboardError;

        // ✅ NEW: File offer event (transferId, fileNames, totalSize)
        public event Action<string, List<string>, long> OnClipboardFileOfferReceived;

        /// <summary>
        /// Clipboard sharing enabled/disabled
        /// </summary>
        public bool IsClipboardSharingEnabled
        {
            get => _clipboardSharingManager?.IsEnabled ?? false;
            set
            {
                if (_clipboardSharingManager != null)
                    _clipboardSharingManager.IsEnabled = value;
            }
        }

        /// <summary>
        /// Initialize clipboard sharing services. 
        /// Must be called after _peerManager and _fileTransferManager are created.
        /// </summary>
        private void InitializeClipboardServices()
        {
            try
            {
                if (_peerManager == null || _fileTransferManager == null)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Cannot initialize clipboard: PeerManager or FileTransferManager is null");
                    return;
                }

                // Create ClipboardSharingManager
                _clipboardSharingManager = new ClipboardSharingManager(_peerManager, _fileTransferManager, _role);
                AttachClipboardEvents();

                // Create ClipboardMonitor (must be on UI thread)
                if (_uiContext != null)
                {
                    _uiContext.Post(_ =>
                    {
                        try
                        {
                            _clipboardMonitor = new ClipboardMonitor();
                            _clipboardMonitor.OnClipboardChanged += OnLocalClipboardChangedHandler;
                            _clipboardMonitor.Start();
                            Console.WriteLine($"[{DateTime.Now}] 📋 ClipboardMonitor created and started on UI thread");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] ❌ ClipboardMonitor creation error: {ex.Message}");
                        }
                    }, null);
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ No UI context for ClipboardMonitor, clipboard monitoring disabled");
                }

                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard services initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error initializing clipboard services: {ex.Message}");
            }
        }

        private void AttachClipboardEvents()
        {
            if (_clipboardSharingManager == null) return;

            _clipboardSharingManager.OnRemoteTextReceived += OnClipboardRemoteTextReceivedHandler;
            _clipboardSharingManager.OnRemoteFilesReceived += OnClipboardRemoteFilesReceivedHandler;
            _clipboardSharingManager.OnClipboardStatusChanged += OnClipboardStatusChangedHandler;
            _clipboardSharingManager.OnClipboardError += OnClipboardErrorHandler;
            // ✅ NEW
            _clipboardSharingManager.OnRemoteFileOfferReceived += OnClipboardFileOfferReceivedHandler;

            Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard events attached");
        }

        private void DetachClipboardEvents()
        {
            if (_clipboardSharingManager != null)
            {
                _clipboardSharingManager.OnRemoteTextReceived -= OnClipboardRemoteTextReceivedHandler;
                _clipboardSharingManager.OnRemoteFilesReceived -= OnClipboardRemoteFilesReceivedHandler;
                _clipboardSharingManager.OnClipboardStatusChanged -= OnClipboardStatusChangedHandler;
                _clipboardSharingManager.OnClipboardError -= OnClipboardErrorHandler;
                // ✅ NEW
                _clipboardSharingManager.OnRemoteFileOfferReceived -= OnClipboardFileOfferReceivedHandler;
            }

            if (_clipboardMonitor != null)
            {
                _clipboardMonitor.OnClipboardChanged -= OnLocalClipboardChangedHandler;
            }

            Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard events detached");
        }

        private void CleanupClipboardServices()
        {
            try
            {
                DetachClipboardEvents();

                if (_clipboardMonitor != null)
                {
                    if (_uiContext != null)
                    {
                        _uiContext.Post(_ =>
                        {
                            _clipboardMonitor?.Dispose();
                            _clipboardMonitor = null;
                        }, null);
                    }
                    else
                    {
                        _clipboardMonitor?.Dispose();
                        _clipboardMonitor = null;
                    }
                }

                _clipboardSharingManager?.Dispose();
                _clipboardSharingManager = null;

                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard services cleaned up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error cleaning up clipboard services: {ex.Message}");
            }
        }

        // ✅ NEW: Accept/Reject file offer (called from MainForm)
        public void AcceptClipboardFileOffer(string transferId)
        {
            _clipboardSharingManager?.AcceptFileOffer(transferId);
        }

        public void RejectClipboardFileOffer(string transferId)
        {
            _clipboardSharingManager?.RejectFileOffer(transferId);
        }

        #region === Clipboard Event Handlers ===

        private void OnLocalClipboardChangedHandler()
        {
            if (_isDisposed || !_isConnected) return;

            try
            {
                _clipboardSharingManager?.HandleLocalClipboardChange();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Clipboard change handler error: {ex.Message}");
            }
        }

        private void OnClipboardRemoteTextReceivedHandler(string text)
        {
            if (_isDisposed || !_isConnected) return;
            OnClipboardRemoteTextReceived?.Invoke(text);
        }

        private void OnClipboardRemoteFilesReceivedHandler(List<string> files)
        {
            if (_isDisposed || !_isConnected) return;
            OnClipboardRemoteFilesReceived?.Invoke(files);
        }

        private void OnClipboardStatusChangedHandler(string status)
        {
            if (_isDisposed) return;
            OnClipboardStatusChanged?.Invoke(status);
        }

        private void OnClipboardErrorHandler(string error)
        {
            if (_isDisposed) return;
            OnClipboardError?.Invoke(error);
        }

        // ✅ NEW
        private void OnClipboardFileOfferReceivedHandler(string transferId, List<string> fileNames, long totalSize)
        {
            if (_isDisposed || !_isConnected) return;
            OnClipboardFileOfferReceived?.Invoke(transferId, fileNames, totalSize);
        }

        #endregion
    }
}