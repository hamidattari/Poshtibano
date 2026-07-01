using Poshtibano.Common;
using Poshtibano.Desk.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Shared.Services
{
    public class ClipboardData
    {
        public string Type { get; set; }
        public string TextContent { get; set; }
        public List<string> FileNames { get; set; } = new();
        public List<long> FileSizes { get; set; } = new();
        public string TransferId { get; set; }
        public long Timestamp { get; set; }
        public long TotalSize { get; set; }
    }

    public class PendingClipboardFileOffer
    {
        public string TransferId { get; set; }
        public List<string> FilePaths { get; set; }
        public List<string> FileNames { get; set; }
        public long TotalSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ClipboardSharingManager : IDisposable
    {
        private readonly PeerConnectionManager _peer;
        private readonly FileTransferManager _fileTransferManager;
        private readonly ClientRole _localRole;
        private readonly SynchronizationContext _uiContext;

        private bool _isEnabled = true;
        private bool _isDisposed = false;
        private string _lastSentTextHash = string.Empty;
        private long _lastClipboardChangeTime = 0;

        // Suppress counter (SetFileDropList fires 1-2 WM_CLIPBOARDUPDATE)
        private int _suppressCount = 0;
        private readonly object _lock = new object();

        // Track hashes set by remote to prevent echo
        private string _lastRemoteTextHash = string.Empty;
        private string _lastRemoteFilesHash = string.Empty;
        private readonly object _hashLock = new object();

        // Pending file offer (sender side)
        private PendingClipboardFileOffer _pendingOffer;
        private readonly object _offerLock = new object();

        private long _lastFileClipboardChangeTicks = 0;

        // Received clipboard file paths
        private List<string> _receivedClipboardFiles = new();
        private string _receivedClipboardText = string.Empty;

        // Total Size trackers to debounce SetClipboard
        private long _expectedTotalSize = 0;
        private long _receivedTotalSize = 0;
        private readonly object _receiveLock = new object();
        private CancellationTokenSource _setClipboardCts;

        // Events
        public event Action<string> OnRemoteTextReceived;
        public event Action<List<string>> OnRemoteFilesReceived;
        public event Action<string> OnClipboardError;
        public event Action<string> OnClipboardStatusChanged;
        public event Action<string, List<string>, long> OnRemoteFileOfferReceived;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard sharing {(_isEnabled ? "enabled" : "disabled")}");
            }
        }

        public ClipboardSharingManager(
            PeerConnectionManager peerConnectionManager,
            FileTransferManager fileTransferManager,
            ClientRole localRole)
        {
            _peer = peerConnectionManager ?? throw new ArgumentNullException(nameof(peerConnectionManager));
            _fileTransferManager = fileTransferManager ?? throw new ArgumentNullException(nameof(fileTransferManager));
            _localRole = localRole;
            _uiContext = SynchronizationContext.Current;

            _peer.OnChatDataReceived += OnChatDataReceivedForClipboard;
            _fileTransferManager.OnFileReceived += OnClipboardFileReceived;

            Console.WriteLine($"[{DateTime.Now}] 📋 ClipboardSharingManager created for {_localRole}");
        }

        #region === SENDING (Copy side) ===

        public void HandleLocalClipboardChange()
        {
            if (!_isEnabled || _isDisposed) return;

            lock (_lock)
            {
                if (_suppressCount > 0)
                {
                    _suppressCount--;
                    Console.WriteLine($"[{DateTime.Now}] 📋 Suppressed echo clipboard change (remaining: {_suppressCount})");
                    return;
                }
            }

            try
            {
                var now = DateTime.UtcNow.Ticks;
                if (now - _lastClipboardChangeTime < TimeSpan.TicksPerMillisecond * 300)
                    return;
                _lastClipboardChangeTime = now;

                var thread = new Thread(() =>
                {
                    try
                    {
                        if (System.Windows.Forms.Clipboard.ContainsFileDropList())
                        {
                            var files = System.Windows.Forms.Clipboard.GetFileDropList();
                            var filePaths = new List<string>();
                            foreach (string f in files)
                            {
                                if (!string.IsNullOrEmpty(f))
                                    filePaths.Add(f);
                            }

                            if (filePaths.Count > 0)
                            {
                                var hash = string.Join("|", filePaths.OrderBy(f => f));
                                lock (_hashLock)
                                {
                                    if (hash == _lastRemoteFilesHash)
                                    {
                                        Console.WriteLine($"[{DateTime.Now}] 📋 Skipping: files were set by remote");
                                        return;
                                    }
                                }

                                SendClipboardFileOffer(filePaths);
                            }
                        }
                        else if (System.Windows.Forms.Clipboard.ContainsText())
                        {
                            var text = System.Windows.Forms.Clipboard.GetText();
                            if (!string.IsNullOrEmpty(text))
                            {
                                var hash = text.GetHashCode().ToString();

                                lock (_hashLock)
                                {
                                    if (hash == _lastRemoteTextHash)
                                    {
                                        Console.WriteLine($"[{DateTime.Now}] 📋 Skipping: text was set by remote");
                                        return;
                                    }
                                }

                                if (hash != _lastSentTextHash)
                                {
                                    _lastSentTextHash = hash;
                                    SendClipboardText(text);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard read error: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] 📋 HandleLocalClipboardChange error: {ex.Message}");
                OnClipboardError?.Invoke(ex.Message);
            }
        }

        private void SendClipboardText(string text)
        {
            try
            {
                var clipData = new ClipboardData
                {
                    Type = "text",
                    TextContent = text,
                    Timestamp = DateTime.UtcNow.Ticks
                };

                var json = JsonSerializer.Serialize(clipData);
                var packet = new Packet
                {
                    IsDropable = false,
                    SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                    Type = PacketType.ClipboardText,
                    Data = Encoding.UTF8.GetBytes(json),
                    SenderRole = (byte)_localRole,
                    TimestampTicks = DateTime.UtcNow.Ticks
                };

                var serialized = Packet.Serialize(packet);
                _peer.SendChat(serialized);

                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard text sent ({text.Length} chars)");
                OnClipboardStatusChanged?.Invoke($"📋 Text sent ({text.Length} chars)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ SendClipboardText error: {ex.Message}");
                OnClipboardError?.Invoke(ex.Message);
            }
        }

        private void SendClipboardFileOffer(List<string> filePaths)
        {
            try
            {
                var transferId = Guid.NewGuid().ToString("N");
                var fileNames = new List<string>();
                var fileSizes = new List<long>();
                long totalSize = 0;

                foreach (var path in filePaths)
                {
                    fileNames.Add(Path.GetFileName(path));
                    if (File.Exists(path))
                    {
                        var size = new FileInfo(path).Length;
                        fileSizes.Add(size);
                        totalSize += size;
                    }
                    else if (Directory.Exists(path))
                    {
                        var dirSize = GetDirectorySize(path);
                        fileSizes.Add(dirSize);
                        totalSize += dirSize;
                    }
                    else
                    {
                        fileSizes.Add(0);
                    }
                }

                lock (_offerLock)
                {
                    _pendingOffer = new PendingClipboardFileOffer
                    {
                        TransferId = transferId,
                        FilePaths = new List<string>(filePaths),
                        FileNames = fileNames,
                        TotalSize = totalSize
                    };
                }

                var clipData = new ClipboardData
                {
                    Type = "files",
                    FileNames = fileNames,
                    FileSizes = fileSizes,
                    TransferId = transferId,
                    TotalSize = totalSize,
                    Timestamp = DateTime.UtcNow.Ticks
                };

                var json = JsonSerializer.Serialize(clipData);
                var packet = new Packet
                {
                    IsDropable = false,
                    SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                    Type = PacketType.ClipboardFiles,
                    Data = Encoding.UTF8.GetBytes(json),
                    SenderRole = (byte)_localRole,
                    TimestampTicks = DateTime.UtcNow.Ticks
                };

                var serialized = Packet.Serialize(packet);
                _peer.SendChat(serialized);

                var sizeStr = FormatSize(totalSize);
                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard file offer sent: {fileNames.Count} file(s), {sizeStr}, tid={transferId}");
                OnClipboardStatusChanged?.Invoke($"📋 {fileNames.Count} file{(fileNames.Count == 1 ? "" : "s")} ({sizeStr}) suggested, awaiting confirmation...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ SendClipboardFileOffer error: {ex.Message}");
                OnClipboardError?.Invoke(ex.Message);
            }
        }

        private async void HandleFileRequestFromRemote(string requestedTransferId)
        {
            PendingClipboardFileOffer offer;
            lock (_offerLock)
            {
                offer = _pendingOffer;
            }

            if (offer == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ No pending clipboard file offer");
                return;
            }

            if (offer.TransferId != requestedTransferId)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ TransferId mismatch: expected {offer.TransferId}, got {requestedTransferId}");
                return;
            }

            if ((DateTime.UtcNow - offer.CreatedAt).TotalMinutes > 5)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Clipboard file offer expired");
                OnClipboardStatusChanged?.Invoke("📋 File suggestion has expired");
                return;
            }

            try
            {
                Console.WriteLine($"[{DateTime.Now}] 📋 Remote accepted, starting transfer...");
                OnClipboardStatusChanged?.Invoke($"📋 Accepted by the other party, sending {offer.FileNames.Count} file{(offer.FileNames.Count == 1 ? "" : "s")}...");

                await _fileTransferManager.SendPathsAsync(offer.FilePaths);

                var sizeStr = FormatSize(offer.TotalSize);
                Console.WriteLine($"[{DateTime.Now}] 📋 Clipboard files transferred: {offer.FileNames.Count} files ({sizeStr})");
                OnClipboardStatusChanged?.Invoke($"📋 {offer.FileNames.Count} file{(offer.FileNames.Count == 1 ? "" : "s")} ({sizeStr}) sent successfully ✅");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ HandleFileRequestFromRemote error: {ex.Message}");
                OnClipboardError?.Invoke(ex.Message);
            }
        }

        #endregion

        #region === RECEIVING ===

        private void OnChatDataReceivedForClipboard(byte[] data)
        {
            if (!_isEnabled || _isDisposed) return;

            try
            {
                var packet = Packet.DeSerialize(data);
                if (packet == null) return;

                if (packet.SenderRole == (byte)_localRole)
                    return;

                switch (packet.Type)
                {
                    case PacketType.ClipboardText:
                        HandleRemoteClipboardText(packet);
                        break;
                    case PacketType.ClipboardFiles:
                        HandleRemoteClipboardFileOffer(packet);
                        break;
                    case PacketType.ClipboardFileRequest:
                        HandleRemoteClipboardFileRequest(packet);
                        break;
                }
            }
            catch
            {
                // Not a clipboard packet, ignore silently
            }
        }

        private void HandleRemoteClipboardText(Packet packet)
        {
            try
            {
                var json = Encoding.UTF8.GetString(packet.Data);
                var clipData = JsonSerializer.Deserialize<ClipboardData>(json);

                if (clipData == null || string.IsNullOrEmpty(clipData.TextContent))
                    return;

                _receivedClipboardText = clipData.TextContent;

                Console.WriteLine($"[{DateTime.Now}] 📋 Remote clipboard text received ({clipData.TextContent.Length} chars)");

                lock (_lock)
                {
                    _suppressCount += 2;
                }

                lock (_hashLock)
                {
                    _lastRemoteTextHash = clipData.TextContent.GetHashCode().ToString();
                }

                var thread = new Thread(() =>
                {
                    try
                    {
                        System.Windows.Forms.Clipboard.SetText(clipData.TextContent);
                        Console.WriteLine($"[{DateTime.Now}] 📋 Remote text set in local clipboard");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ❌ Clipboard.SetText error: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(2000);

                if (_uiContext != null)
                    _uiContext.Post(_ => OnRemoteTextReceived?.Invoke(clipData.TextContent), null);
                else
                    OnRemoteTextReceived?.Invoke(clipData.TextContent);

                OnClipboardStatusChanged?.Invoke($"📋 Remote text received ({clipData.TextContent.Length} chars)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ HandleRemoteClipboardText error: {ex.Message}");
            }
        }

        private void HandleRemoteClipboardFileOffer(Packet packet)
        {
            try
            {
                var json = Encoding.UTF8.GetString(packet.Data);
                var clipData = JsonSerializer.Deserialize<ClipboardData>(json);

                if (clipData == null || clipData.FileNames == null || clipData.FileNames.Count == 0)
                    return;

                lock (_receiveLock)
                {
                    _receivedClipboardFiles.Clear();
                    _expectedTotalSize = clipData.TotalSize;
                    _receivedTotalSize = 0;
                }

                var sizeStr = FormatSize(clipData.TotalSize);
                Console.WriteLine($"[{DateTime.Now}] 📋 Remote file offer: {string.Join(", ", clipData.FileNames)} ({sizeStr})");

                if (_uiContext != null)
                    _uiContext.Post(_ => OnRemoteFileOfferReceived?.Invoke(clipData.TransferId, clipData.FileNames, clipData.TotalSize), null);
                else
                    OnRemoteFileOfferReceived?.Invoke(clipData.TransferId, clipData.FileNames, clipData.TotalSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ HandleRemoteClipboardFileOffer error: {ex.Message}");
            }
        }

        private void HandleRemoteClipboardFileRequest(Packet packet)
        {
            try
            {
                var json = Encoding.UTF8.GetString(packet.Data);
                var clipData = JsonSerializer.Deserialize<ClipboardData>(json);

                if (clipData == null || string.IsNullOrEmpty(clipData.TransferId))
                    return;

                Console.WriteLine($"[{DateTime.Now}] 📋 Remote accepted our file offer: {clipData.TransferId}");
                HandleFileRequestFromRemote(clipData.TransferId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ HandleRemoteClipboardFileRequest error: {ex.Message}");
            }
        }

        public void AcceptFileOffer(string transferId)
        {
            try
            {
                var clipData = new ClipboardData
                {
                    Type = "file_request",
                    TransferId = transferId,
                    Timestamp = DateTime.UtcNow.Ticks
                };

                var json = JsonSerializer.Serialize(clipData);
                var packet = new Packet
                {
                    IsDropable = false,
                    SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                    Type = PacketType.ClipboardFileRequest,
                    Data = Encoding.UTF8.GetBytes(json),
                    SenderRole = (byte)_localRole,
                    TimestampTicks = DateTime.UtcNow.Ticks
                };

                var serialized = Packet.Serialize(packet);
                _peer.SendChat(serialized);

                Console.WriteLine($"[{DateTime.Now}] 📋 File offer accepted, request sent: {transferId}");
                OnClipboardStatusChanged?.Invoke("📋 File receive request has been sent...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ AcceptFileOffer error: {ex.Message}");
                OnClipboardError?.Invoke(ex.Message);
            }
        }

        public void RejectFileOffer(string transferId)
        {
            Console.WriteLine($"[{DateTime.Now}] 📋 File offer rejected: {transferId}");
            OnClipboardStatusChanged?.Invoke("📋 File suggestion declined");
        }

        private void OnClipboardFileReceived(string transferId, string savedPath)
        {
            if (!_isEnabled || _isDisposed) return;

            try
            {
                if (string.IsNullOrEmpty(savedPath) || !File.Exists(savedPath))
                    return;

                string rootItemPath = savedPath;
                try
                {
                    // Walk up directory tree to find the Base TempDirectory, effectively yielding the Root folder of the clipboard drop.
                    var fileInfo = new FileInfo(savedPath);
                    var parent = fileInfo.Directory;
                    string currentPath = savedPath;
                    string tempDir = _fileTransferManager.TempDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    while (parent != null)
                    {
                        string parentName = parent.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (parentName.Equals(tempDir, StringComparison.OrdinalIgnoreCase))
                        {
                            rootItemPath = currentPath;
                            break;
                        }
                        currentPath = parent.FullName;
                        parent = parent.Parent;
                    }
                }
                catch { }

                long fileSize = new FileInfo(savedPath).Length;
                bool isComplete = false;

                lock (_receiveLock)
                {
                    _receivedTotalSize += fileSize;
                    isComplete = _receivedTotalSize >= _expectedTotalSize;

                    if (!_receivedClipboardFiles.Contains(rootItemPath, StringComparer.OrdinalIgnoreCase))
                    {
                        _receivedClipboardFiles.Add(rootItemPath);
                    }
                }

                Console.WriteLine($"[{DateTime.Now}] 📋 File part received: {Path.GetFileName(savedPath)} -> Root: {Path.GetFileName(rootItemPath)}");

                _setClipboardCts?.Cancel();

                if (isComplete)
                {
                    Console.WriteLine($"[{DateTime.Now}] 📋 All clipboard items received successfully!");
                    SetReceivedFilesInClipboard();
                }
                else
                {
                    _setClipboardCts = new CancellationTokenSource();
                    var token = _setClipboardCts.Token;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // A fallback delay, waiting longer to ensure folders finish copying
                            await Task.Delay(3000, token);
                            if (!token.IsCancellationRequested)
                                SetReceivedFilesInClipboard();
                        }
                        catch (TaskCanceledException) { }
                    }, token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ OnClipboardFileReceived error: {ex.Message}");
            }
        }

        private void SetReceivedFilesInClipboard()
        {
            try
            {
                List<string> filesToSet;
                lock (_receiveLock)
                {
                    filesToSet = new List<string>(_receivedClipboardFiles);
                }

                if (filesToSet.Count == 0) return;

                lock (_lock)
                {
                    _suppressCount += 2;
                }

                var echoHash = string.Join("|", filesToSet.OrderBy(f => f));
                lock (_hashLock)
                {
                    _lastRemoteFilesHash = echoHash;
                }

                var thread = new Thread(() =>
                {
                    try
                    {
                        var collection = new System.Collections.Specialized.StringCollection();
                        foreach (var item in filesToSet)
                        {
                            // Support adding Directories too!
                            if (File.Exists(item) || Directory.Exists(item))
                                collection.Add(item);
                        }

                        if (collection.Count > 0)
                        {
                            System.Windows.Forms.Clipboard.SetFileDropList(collection);
                            Console.WriteLine($"[{DateTime.Now}] 📋 {collection.Count} root item(s) set in local clipboard for paste");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ❌ Clipboard.SetFileDropList error: {ex.Message}");
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(2000);

                if (_uiContext != null)
                    _uiContext.Post(_ => OnRemoteFilesReceived?.Invoke(filesToSet), null);
                else
                    OnRemoteFilesReceived?.Invoke(filesToSet);

                OnClipboardStatusChanged?.Invoke($"📋 {filesToSet.Count} file/folder{(filesToSet.Count == 1 ? "" : "s")} ready for paste ✅");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ SetReceivedFilesInClipboard error: {ex.Message}");
            }
        }

        #endregion

        #region === UTILITY ===

        public void SuppressNextChange()
        {
            lock (_lock)
            {
                _suppressCount++;
            }
        }

        public List<string> GetReceivedFiles()
        {
            lock (_receiveLock)
            {
                return new List<string>(_receivedClipboardFiles);
            }
        }

        public string GetReceivedText() => _receivedClipboardText;

        private static long GetDirectorySize(string path)
        {
            try
            {
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }
            catch { return 0; }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _peer.OnChatDataReceived -= OnChatDataReceivedForClipboard;
            _fileTransferManager.OnFileReceived -= OnClipboardFileReceived;

            _setClipboardCts?.Cancel();
            _setClipboardCts?.Dispose();

            lock (_receiveLock)
            {
                _receivedClipboardFiles.Clear();
            }

            lock (_offerLock)
            {
                _pendingOffer = null;
            }

            OnRemoteTextReceived = null;
            OnRemoteFilesReceived = null;
            OnRemoteFileOfferReceived = null;
            OnClipboardError = null;
            OnClipboardStatusChanged = null;

            Console.WriteLine($"[{DateTime.Now}] 📋 ClipboardSharingManager disposed");
        }

        #endregion
    }
}