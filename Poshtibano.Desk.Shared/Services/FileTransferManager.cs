using Poshtibano.Common;
using Poshtibano.Desk.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Shared.Services
{
    public enum FileTransferProgressState
    {
        None,
        Start,
        InTransfer,
        Complete
    }

    public class FileTransferProgress
    {
        public string TransferId { get; set; }
        public string FileName { get; set; }
        public long TotalBytes { get; set; }
        public long TransferredBytes { get; set; }
        public bool IsReceiving { get; set; } = false;
        public FileTransferProgressState State { get; set; }

        private double? _customPercent;

        public double Percent
        {
            get
            {
                if (_customPercent.HasValue)
                    return _customPercent.Value;

                return TotalBytes == 0 ? 0 : TransferredBytes * 100.0 / TotalBytes;
            }
            set
            {
                _customPercent = value;
            }
        }
    }

    public class FileTransferManager : IDisposable
    {
        public int MaxFileChunkSize { get; set; } = 8 * 1024; // 8 * 1024
        public int DelayBetweenEachChunckForFileTransfer { get; set; } = 25; //25
        public int DelayBetweenEachFileTransfer { get; set; } = 10; //10

        private readonly PeerConnectionManager _peer;
        private readonly ClientRole _localRole;
        private readonly string _tempDirectory;
        private readonly SynchronizationContext _uiContext;

        public bool UsePrefixForFileName { get; set; } = false;

        // Expose TempDirectory for Clipboard sharing manager to rebuild roots
        public string TempDirectory => _tempDirectory;

        private readonly ConcurrentDictionary<string, (FileStream Stream, string TempPath, long ExpectedSize, int TotalChunks, int ReceivedChunks)> incomingTransfers = new();

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTransfers = new();
        private readonly ConcurrentDictionary<string, string> _activeFilenames = new();

        public event Action<FileTransferProgress> OnProgressUpdated;
        public event Action<string, string> OnFileReceived;
        public event Action<string, string, long> OnTransferStarted;
        public event Action<string> OnTransferCompleted;
        public event Action<string, string> OnTransferCancelled;

        public FileTransferManager(PeerConnectionManager peerConnectionManager, ClientRole localRole)
        {
            _peer = peerConnectionManager ?? throw new ArgumentNullException(nameof(peerConnectionManager));
            _localRole = localRole;
            _uiContext = SynchronizationContext.Current;

            _tempDirectory = Path.Combine(Environment.CurrentDirectory, "temp");
            Directory.CreateDirectory(_tempDirectory);

            _peer.OnFileDataReceived += Peer_OnFileDataReceived;
        }

        private void Peer_OnFileDataReceived(byte[] data)
        {
            var packet = Packet.DeSerialize(data);
            if (packet == null) return;

            switch (packet.Type)
            {
                case PacketType.FileStart:
                    HandleFileStart(packet);
                    break;
                case PacketType.FileChunk:
                    HandleFileChunk(packet);
                    break;
                case PacketType.FileEnd:
                    HandleFileEnd(packet);
                    break;
                case PacketType.FileCancel:
                    HandleFileCancel(packet);
                    break;
            }
        }

        public void HandleFileCancel(Packet packet)
        {
            try
            {
                var transferId = packet.TransferId;
                _activeFilenames.TryGetValue(transferId, out var transferFilename);
                _activeTransfers.TryGetValue(transferId, out var transferCts);

                if (transferCts != null)
                    transferCts.Cancel();
                else return;

                string cancelledFileName = transferFilename ?? "Unknown";

                Console.WriteLine($"[{DateTime.Now}] 🛑 File cancel received: {transferId}");

                if (incomingTransfers.TryRemove(transferId, out var tuple))
                {
                    try
                    {
                        cancelledFileName = Path.GetFileName(tuple.TempPath).Replace(".part", string.Empty).Replace(".part", string.Empty);

                        tuple.Stream?.Dispose();
                        if (File.Exists(tuple.TempPath))
                            File.Delete(tuple.TempPath);
                        Console.WriteLine($"[{DateTime.Now}] 🗑️ Temp file deleted: {tuple.TempPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ Error cleaning temp: {ex.Message}");
                    }
                }

                if (_activeTransfers.TryRemove(transferId, out var cts))
                {
                    _activeFilenames.TryRemove(transferId, out _);

                    cts.Cancel();
                    cts.Dispose();
                    Console.WriteLine($"[{DateTime.Now}] 🛑 Outgoing transfer cancelled");
                }

                try
                {
                    OnTransferCancelled?.Invoke(transferId, cancelledFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error invoking callback: {ex.Message}");
                }

                OnTransferCompleted?.Invoke(transferId);

                Console.WriteLine($"[{DateTime.Now}] ✅ Transfer cancelled and cleaned up: {transferId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ FileCancel handling error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void HandleFileStart(Packet packet)
        {
            try
            {
                var transferId = packet.TransferId;
                var relativeName = SanitizeRelativePath(packet.FileName);

                // Allow creating nested subdirectories in temp folder based on relative path
                var tempPath = Path.Combine(_tempDirectory, UsePrefixForFileName ? $"{transferId}_{relativeName}" : relativeName + ".part");

                var dir = Path.GetDirectoryName(tempPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                incomingTransfers[transferId] = (fs, tempPath, packet.TotalSize, packet.TotalChunks, 0);

                _activeTransfers.TryAdd(transferId, new CancellationTokenSource());
                _activeFilenames.TryAdd(transferId, packet.FileName);

                OnTransferStarted?.Invoke(transferId, packet.FileName, packet.TotalSize);
                UpdateProgress(transferId);

                Console.WriteLine($"[{DateTime.Now}] 📥 File transfer started: {packet.FileName} ({packet.TotalSize} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FileStart handling error: {ex.Message}");
            }
        }

        private void HandleFileChunk(Packet packet)
        {
            try
            {
                var transferId = packet.TransferId;

                if (_activeTransfers.TryGetValue(transferId, out var cts) && cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⛔ Transfer cancelled: {transferId}");
                    return;
                }

                if (!incomingTransfers.TryGetValue(transferId, out var tuple))
                {
                    Console.WriteLine($"Unknown transfer id {transferId} for chunk");
                    return;
                }

                var fs = tuple.Stream;
                fs.Seek(packet.ChunkIndex * (long)packet.Data.Length, SeekOrigin.Begin);
                fs.Write(packet.Data, 0, packet.Data.Length);
                fs.Flush();

                incomingTransfers[transferId] = (fs, tuple.TempPath, tuple.ExpectedSize, tuple.TotalChunks, tuple.ReceivedChunks + 1);
                UpdateProgress(transferId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FileChunk handling error: {ex.Message}");
            }
        }

        private void HandleFileEnd(Packet packet)
        {
            try
            {
                var transferId = packet.TransferId;
                if (!incomingTransfers.TryRemove(transferId, out var tuple))
                {
                    Console.WriteLine($"Unknown transfer end id {transferId}");
                    return;
                }

                var fs = tuple.Stream;
                fs.Flush();
                fs.Dispose();

                var finalName = SanitizeRelativePath(packet.FileName);
                var finalPath = Path.Combine(_tempDirectory, UsePrefixForFileName ? $"{transferId}_{finalName}" : finalName);

                var finalDir = Path.GetDirectoryName(finalPath);
                if (!Directory.Exists(finalDir))
                    Directory.CreateDirectory(finalDir);

                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(tuple.TempPath, finalPath);

                OnFileReceived?.Invoke(transferId, finalPath);
                OnTransferCompleted?.Invoke(transferId);

                _activeTransfers.TryRemove(transferId, out var cts);
                _activeFilenames.TryRemove(transferId, out _);

                cts?.Dispose();

                Console.WriteLine($"[{DateTime.Now}] ✅ File transfer completed: {finalName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FileEnd handling error: {ex.Message}");
            }
        }

        private void UpdateProgress(string transferId)
        {
            if (!incomingTransfers.TryGetValue(transferId, out var tuple)) return;

            var progress = new FileTransferProgress
            {
                TransferId = transferId,
                FileName = Path.GetFileName(tuple.TempPath),
                TotalBytes = tuple.ExpectedSize,
                TransferredBytes = new FileInfo(tuple.TempPath).Length,
                IsReceiving = true
            };

            if (_uiContext != null)
                _uiContext.Post(_ => OnProgressUpdated?.Invoke(progress), null);
            else
                OnProgressUpdated?.Invoke(progress);
        }

        private string SanitizeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "unknown";

            // Normalize path separators to Windows style
            path = path.Replace('/', '\\');
            // Prevent directory traversal attacks
            path = path.Replace("..\\", "").Replace("../", "");

            var invalidChars = new HashSet<char>(Path.GetInvalidPathChars());
            invalidChars.Add(':'); // Prevent root drives
            invalidChars.Add('*');
            invalidChars.Add('?');
            invalidChars.Add('"');
            invalidChars.Add('<');
            invalidChars.Add('>');
            invalidChars.Add('|');

            var sb = new StringBuilder();
            foreach (var c in path)
            {
                if (!invalidChars.Contains(c)) sb.Append(c);
                else sb.Append('_');
            }

            return sb.ToString().TrimStart('\\');
        }

        /// <summary>
        /// Sends files while maintaining relative paths, removing the ZIP logic to keep folder structures intact.
        /// </summary>
        public async Task SendPathsAsync(IEnumerable<string> paths, IProgress<FileTransferProgress> progress = null, CancellationToken cancellation = default)
        {
            var filesToSend = new List<(string AbsolutePath, string RelativeName, long Size)>();

            foreach (var p in paths)
            {
                cancellation.ThrowIfCancellationRequested();

                if (Directory.Exists(p))
                {
                    var dirInfo = new DirectoryInfo(p);
                    var dirName = dirInfo.Name;

                    var allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                    foreach (var fileInfo in allFiles)
                    {
                        string parentPath = dirInfo.Parent?.FullName;
                        string relPath = fileInfo.FullName;

                        if (!string.IsNullOrEmpty(parentPath))
                        {
                            relPath = fileInfo.FullName.Substring(parentPath.Length);
                            relPath = relPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        }
                        else
                        {
                            relPath = Path.Combine(dirName, fileInfo.Name);
                        }

                        filesToSend.Add((fileInfo.FullName, relPath, fileInfo.Length));
                    }
                }
                else if (File.Exists(p))
                {
                    var fileInfo = new FileInfo(p);
                    filesToSend.Add((fileInfo.FullName, fileInfo.Name, fileInfo.Length));
                }
            }

            foreach (var item in filesToSend)
            {
                cancellation.ThrowIfCancellationRequested();

                var absolutePath = item.AbsolutePath;
                var relativeName = item.RelativeName;
                var fileSize = item.Size;

                var transferId = Guid.NewGuid().ToString("N");
                var totalChunks = (int)((fileSize + (MaxFileChunkSize - 1)) / MaxFileChunkSize);
                if (totalChunks == 0) totalChunks = 1;

                var transferCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                _activeTransfers.TryAdd(transferId, transferCts);
                _activeFilenames.TryAdd(transferId, absolutePath);

                try
                {
                    var startPacket = new Packet
                    {
                        IsDropable = false,
                        SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                        Type = PacketType.FileStart,
                        FileName = relativeName, // Encode the relative path
                        TransferId = transferId,
                        TotalSize = fileSize,
                        ChunkIndex = 0,
                        TotalChunks = totalChunks,
                        Data = Array.Empty<byte>(),
                        SenderRole = (byte)_localRole,
                        TimestampTicks = DateTime.UtcNow.Ticks
                    };
                    _peer.SendFile(Packet.Serialize(startPacket));

                    progress?.Report(new FileTransferProgress
                    {
                        State = FileTransferProgressState.Start,
                        TransferId = transferId,
                        FileName = relativeName,
                        TotalBytes = fileSize,
                        TransferredBytes = 0,
                        IsReceiving = false
                    });

                    await Task.Delay(DelayBetweenEachFileTransfer, transferCts.Token);

                    using var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
                    int chunkIndex = 0;
                    var buffer = new byte[MaxFileChunkSize];
                    int read;
                    long sentBytes = 0;

                    if (fileSize > 0)
                    {
                        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length, transferCts.Token)) > 0)
                        {
                            if (transferCts.Token.IsCancellationRequested)
                            {
                                Console.WriteLine($"[{DateTime.Now}] ⛔ Transfer cancelled by user: {transferId}");
                                throw new OperationCanceledException();
                            }

                            var chunk = new byte[read];
                            Array.Copy(buffer, chunk, read);

                            var chunkPacket = new Packet
                            {
                                IsDropable = false,
                                SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                                Type = PacketType.FileChunk,
                                FileName = relativeName,
                                TransferId = transferId,
                                TotalSize = fileSize,
                                ChunkIndex = chunkIndex++,
                                TotalChunks = totalChunks,
                                Data = chunk,
                                SenderRole = (byte)_localRole,
                                TimestampTicks = DateTime.UtcNow.Ticks
                            };

                            _peer.SendFile(Packet.Serialize(chunkPacket));
                            sentBytes += read;

                            progress?.Report(new FileTransferProgress
                            {
                                State = FileTransferProgressState.InTransfer,
                                TransferId = transferId,
                                FileName = relativeName,
                                TotalBytes = fileSize,
                                TransferredBytes = sentBytes,
                                IsReceiving = false,
                                Percent = sentBytes * 100.0 / fileSize,
                            });

                            await Task.Delay(DelayBetweenEachChunckForFileTransfer, transferCts.Token);
                        }
                    }

                    var endPacket = new Packet
                    {
                        IsDropable = false,
                        SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                        Type = PacketType.FileEnd,
                        FileName = relativeName,
                        TransferId = transferId,
                        TotalSize = fileSize,
                        ChunkIndex = chunkIndex > 0 ? chunkIndex - 1 : 0,
                        TotalChunks = totalChunks,
                        Data = Array.Empty<byte>(),
                        SenderRole = (byte)_localRole,
                        TimestampTicks = DateTime.UtcNow.Ticks
                    };

                    _peer.SendFile(Packet.Serialize(endPacket));

                    progress?.Report(new FileTransferProgress
                    {
                        State = FileTransferProgressState.Complete,
                        TransferId = transferId,
                        FileName = relativeName,
                        TotalBytes = fileSize,
                        TransferredBytes = fileSize,
                        IsReceiving = false,
                        Percent = 100.0
                    });

                    Console.WriteLine($"[{DateTime.Now}] ✅ File sent: {relativeName} ({fileSize} bytes)");

                    await Task.Delay(DelayBetweenEachFileTransfer, transferCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⛔ File transfer cancelled: {absolutePath}");
                }
                finally
                {
                    _activeTransfers.TryRemove(transferId, out var cts);
                    cts?.Dispose();
                }
            }
        }

        public string CancelTransfer(string transferId)
        {
            Console.WriteLine($"[{DateTime.Now}] 🛑 Cancelling transfer: {transferId}");

            if (_activeTransfers.TryGetValue(transferId, out var cts))
            {
                cts.Cancel();
            }
            _activeFilenames.TryGetValue(transferId, out var fileName);

            try
            {
                var cancelPacket = new Packet
                {
                    IsDropable = false,
                    SequenceNumber = (ulong)DateTime.UtcNow.Ticks,
                    Type = PacketType.FileCancel,
                    TransferId = transferId,
                    Data = System.Text.Encoding.UTF8.GetBytes(transferId),
                    SenderRole = (byte)_localRole
                };
                _peer.SendFile(Packet.Serialize(cancelPacket));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending cancel: {ex.Message}");
            }

            if (incomingTransfers.TryRemove(transferId, out var tuple))
            {
                try
                {
                    tuple.Stream?.Dispose();
                    if (File.Exists(tuple.TempPath))
                        File.Delete(tuple.TempPath);
                }
                catch { }
            }

            return fileName;
        }

        public void CancelAllTransfers()
        {
            Console.WriteLine($"[{DateTime.Now}] 🛑 Cancelling all transfers...");

            foreach (var kvp in _activeTransfers)
            {
                kvp.Value?.Cancel();
            }

            foreach (var kvp in incomingTransfers)
            {
                try
                {
                    kvp.Value.Stream?.Dispose();
                    if (File.Exists(kvp.Value.TempPath))
                        File.Delete(kvp.Value.TempPath);
                }
                catch { }
            }

            incomingTransfers.Clear();
        }

        public void CleanupTemp()
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    var rootDirectory = new DirectoryInfo(_tempDirectory);
                    foreach (var file in rootDirectory.GetFiles())
                    {
                        try { file.Delete(); } catch { }
                    }
                    foreach (var directory in rootDirectory.GetDirectories())
                    {
                        try { directory.Delete(true); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning temp: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _peer.OnFileDataReceived -= Peer_OnFileDataReceived;

            CancelAllTransfers();

            foreach (var kvp in _activeTransfers)
            {
                kvp.Value?.Dispose();
            }
            _activeTransfers.Clear();
            _activeFilenames.Clear();

            OnTransferCancelled = null;
        }
    }
}