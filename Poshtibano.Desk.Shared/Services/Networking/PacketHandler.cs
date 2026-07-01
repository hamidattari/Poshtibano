using Poshtibano.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Poshtibano.Desk.Services.Networking
{
    public class PacketReceivedEventArgs : EventArgs
    {
        public Packet Packet { get; set; }
    }

    public class PacketHandler : IDisposable
    {
        private readonly ConcurrentDictionary<ulong, List<Packet>> _fragmentBuffer = new();
        private readonly object _fragmentLock = new object();
        private readonly SynchronizationContext _uiContext;
        private Timer _cleanupTimer;

        public event EventHandler<PacketReceivedEventArgs> OnFramePacket;
        public event EventHandler<PacketReceivedEventArgs> OnEventPacket;
        public event EventHandler<PacketReceivedEventArgs> OnChatPacket;
        public event EventHandler<PacketReceivedEventArgs> OnChatEventPacket;
        public event EventHandler<PacketReceivedEventArgs> OnFilePacket;
        public event EventHandler<PacketReceivedEventArgs> OnSettingsPacket;
        public event EventHandler<PacketReceivedEventArgs> OnFileCancelPacket;

        public event EventHandler<PacketReceivedEventArgs> OnAudioPacket;
        public event EventHandler<PacketReceivedEventArgs> OnWebcamPacket;


        public PacketHandler()
        {
            _uiContext = SynchronizationContext.Current;

            if (_uiContext == null)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ WARNING: PacketHandler created without UI context!");
            }

            // Cleanup old fragments every 30 seconds
            _cleanupTimer = new Timer(CleanupOldFragments, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void HandleRawData(byte[] data, PacketType expectedType)
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Received null or empty data");
                return;
            }

            try
            {
                var packet = Packet.DeSerialize(data);
                if (packet == null)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Failed to deserialize packet");
                    return;
                }

                if (!packet.IsFragmented)
                {
                    ProcessCompletePacket(packet);
                }
                else
                {
                    HandleFragmentedPacket(packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error handling packet: {ex.Message}");
            }
        }

        private void HandleFragmentedPacket(Packet packet)
        {
            lock (_fragmentLock)
            {
                var fragmentList = _fragmentBuffer.GetOrAdd(packet.SequenceNumber, _ => new List<Packet>());
                fragmentList.Add(packet);

                if (fragmentList.Count == packet.TotalFragments)
                {
                    try
                    {
                        var completePacket = ReassembleFragments(fragmentList, packet);
                        ProcessCompletePacket(completePacket);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ❌ Error reassembling fragments: {ex.Message}");
                    }
                    finally
                    {
                        _fragmentBuffer.TryRemove(packet.SequenceNumber, out _);
                    }
                }
            }
        }

        private Packet ReassembleFragments(List<Packet> fragments, Packet template)
        {
            fragments.Sort((a, b) => a.FragmentIndex.CompareTo(b.FragmentIndex));

            var totalLength = fragments.Sum(f => f.Data?.Length ?? 0);
            var reconstructed = new byte[totalLength];
            int offset = 0;

            foreach (var frag in fragments)
            {
                if (frag.Data != null && frag.Data.Length > 0)
                {
                    Buffer.BlockCopy(frag.Data, 0, reconstructed, offset, frag.Data.Length);
                    offset += frag.Data.Length;
                }
            }

            return new Packet
            {
                IsDropable = template.IsDropable,
                SequenceNumber = template.SequenceNumber,
                Type = template.Type,
                Data = reconstructed,
                IsFragmented = false,
                MessageId = template.MessageId,
                FileName = template.FileName,
                TransferId = template.TransferId,
                TotalSize = template.TotalSize,
                ChunkIndex = template.ChunkIndex,
                TotalChunks = template.TotalChunks,
                SenderRole = template.SenderRole,
                TimestampTicks = template.TimestampTicks
            };
        }

        private void ProcessCompletePacket(Packet packet)
        {
            var args = new PacketReceivedEventArgs { Packet = packet };

            EventHandler<PacketReceivedEventArgs> handler = packet.Type switch
            {
                PacketType.Frame => OnFramePacket,
                PacketType.Event => OnEventPacket,
                PacketType.Chat => OnChatPacket,
                PacketType.ChatEvent => OnChatEventPacket,
                PacketType.FileStart => OnFilePacket,
                PacketType.FileChunk => OnFilePacket,
                PacketType.FileEnd => OnFilePacket,
                PacketType.FileCancel => OnFileCancelPacket,
                PacketType.Settings => OnSettingsPacket,
                // ✅ NEW: Audio/Video packet types
                PacketType.AudioData => OnAudioPacket,
                PacketType.WebcamData => OnWebcamPacket,
                _ => null
            };

            if (handler != null)
            {
                try
                {
                    bool isLowLatencyPacket = packet.Type == PacketType.Frame ||
                                      packet.Type == PacketType.AudioData ||
                                      packet.Type == PacketType.WebcamData;



                    // ✅ Check if context available
                    if (_uiContext != null && packet.Type != PacketType.Frame)
                    {
                        _uiContext.Post(_ =>
                        {
                            try
                            {
                                handler.Invoke(this, args);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[{DateTime.Now}] ❌ Error in event handler: {ex.Message}");
                            }
                        }, null);
                    }
                    else if (!isLowLatencyPacket)
                    {
                        //Console.WriteLine($"[{DateTime.Now}] ⚠️ No UI context, executing on thread pool");
                        //Task.Run(() =>
                        //{
                        try
                        {
                            handler.Invoke(this, args);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] ❌ Error in event handler (no UI context): {ex.Message}");
                        }
                        //});
                    }
                    else if (packet.Type != PacketType.Frame)
                    {
                        // ✅ No UI context, but we need to handle this safely
                        //Console.WriteLine($"[{DateTime.Now}] ⚠️ No UI context, executing on thread pool");
                        //Task.Run(() =>
                        //{
                        try
                        {
                            handler.Invoke(this, args);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] ❌ Error in event handler (no UI context): {ex.Message}");
                        }
                        //});
                    }
                    else
                    {
                        handler.Invoke(this, args);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error processing packet: {ex.Message}");
                }
            }
        }

        private void CleanupOldFragments(object state)
        {
            try
            {
                var now = DateTime.UtcNow.Ticks;
                var timeout = TimeSpan.FromMinutes(5).Ticks;

                var oldKeys = _fragmentBuffer
                    .Where(kvp => kvp.Value.Count > 0 && (now - kvp.Value[0].TimestampTicks) > timeout)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in oldKeys)
                {
                    if (_fragmentBuffer.TryRemove(key, out _))
                    {
                        Console.WriteLine($"[{DateTime.Now}] Cleaned up old fragment buffer for sequence {key}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error in fragment cleanup: {ex.Message}");
            }
        }

        private void ClearAudioVideoPacketEvents()
        {
            OnAudioPacket = null;
            OnWebcamPacket = null;
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _fragmentBuffer.Clear();

            OnFramePacket = null;
            OnEventPacket = null;
            OnChatPacket = null;
            OnChatEventPacket = null;
            OnFilePacket = null;
            OnSettingsPacket = null;
        }
    }
}