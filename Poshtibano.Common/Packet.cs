using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Poshtibano.Common
{
    public class Packet
    {
        // Keep relatively small to allow datachannel-friendly fragments.
        public const int MaxFragmentSize = 16 * 1024;
        public Guid MessageId { get; set; } = Guid.Empty;

        public bool IsDropable { get; set; }
        public ulong SequenceNumber { get; set; }
        public PacketType Type { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        // Fragmentation metadata
        public bool IsFragmented { get; set; } = false;
        public int FragmentIndex { get; set; } = 0;
        public int TotalFragments { get; set; } = 1;

        // File/transfer metadata
        public string FileName { get; set; } = string.Empty;
        public string TransferId { get; set; } = string.Empty;
        public long TotalSize { get; set; } = 0;
        public int ChunkIndex { get; set; } = 0;
        public int TotalChunks { get; set; } = 0;

        // Chat / sender metadata
        public byte SenderRole { get; set; } = 0;
        public long TimestampTicks { get; set; } = DateTime.UtcNow.Ticks;

        // ✅ Audio/Video metadata
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public int SampleRate { get; set; }
        public byte Channels { get; set; }
        public byte BitsPerSample { get; set; }
        public byte[] AudioData { get; set; }

        // ============================================================
        // SERIALIZE 
        // ============================================================
        public static byte[] Serialize(Packet packet)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(packet.IsDropable);
                writer.Write(packet.SequenceNumber);
                writer.Write((byte)packet.Type);
                writer.Write(packet.Data?.Length ?? 0);
                if (packet.Data != null && packet.Data.Length > 0)
                    writer.Write(packet.Data);

                writer.Write(packet.IsFragmented);
                writer.Write(packet.FragmentIndex);
                writer.Write(packet.TotalFragments);

                writer.Write(packet.FileName ?? string.Empty);
                writer.Write(packet.TransferId ?? string.Empty);
                writer.Write(packet.TotalSize);
                writer.Write(packet.ChunkIndex);
                writer.Write(packet.TotalChunks);

                writer.Write(packet.SenderRole);
                writer.Write(packet.TimestampTicks);

                writer.Write(packet.MessageId.ToByteArray());

                // ✅ Audio/Video fields
                writer.Write(packet.Width);
                writer.Write(packet.Height);
                writer.Write(packet.SampleRate);
                writer.Write(packet.Channels);
                writer.Write(packet.BitsPerSample);

                // ✅ AudioData 
                writer.Write(packet.AudioData?.Length ?? 0);
                if (packet.AudioData != null && packet.AudioData.Length > 0)
                    writer.Write(packet.AudioData);

                return ms.ToArray();
            }
        }

        // ============================================================
        // DESERIALIZE
        // ============================================================
        public static Packet DeSerialize(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            try
            {
                using (var ms = new MemoryStream(data))
                using (var reader = new BinaryReader(ms))
                {
                    var packet = new Packet();

                    packet.IsDropable = reader.ReadBoolean();
                    packet.SequenceNumber = reader.ReadUInt64();
                    packet.Type = (PacketType)reader.ReadByte();

                    int dataLength = reader.ReadInt32();
                    packet.Data = dataLength > 0 ? reader.ReadBytes(dataLength) : null;

                    packet.IsFragmented = reader.ReadBoolean();
                    packet.FragmentIndex = reader.ReadInt32();
                    packet.TotalFragments = reader.ReadInt32();

                    packet.FileName = reader.ReadString();
                    packet.TransferId = reader.ReadString();
                    packet.TotalSize = reader.ReadInt64();
                    packet.ChunkIndex = reader.ReadInt32();
                    packet.TotalChunks = reader.ReadInt32();

                    packet.SenderRole = reader.ReadByte();
                    packet.TimestampTicks = reader.ReadInt64();

                    // MessageId
                    if (ms.Position + 16 <= ms.Length)
                    {
                        byte[] messageIdBytes = reader.ReadBytes(16);
                        packet.MessageId = new Guid(messageIdBytes);
                    }
                    else
                    {
                        packet.MessageId = Guid.Empty;
                    }

                    // ✅ Audio/Video fields ,If there is more data.
                    if (ms.Position + 8 <= ms.Length) // Each field labeled A or V must be at least 8 bytes long.
                    {
                        packet.Width = reader.ReadUInt16();
                        packet.Height = reader.ReadUInt16();
                        packet.SampleRate = reader.ReadInt32();
                        packet.Channels = reader.ReadByte();
                        packet.BitsPerSample = reader.ReadByte();

                        // ✅ AudioData
                        if (ms.Position + 4 <= ms.Length)
                        {
                            int audioDataLength = reader.ReadInt32();
                            if (audioDataLength > 0 && ms.Position + audioDataLength <= ms.Length)
                            {
                                packet.AudioData = reader.ReadBytes(audioDataLength);
                            }
                        }
                    }

                    return packet;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deserializing packet: {ex.Message}");
                return null;
            }
        }


        private static void WriteString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(0);
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadString(BinaryReader reader)
        {
            int len = reader.ReadInt32();
            if (len <= 0) return string.Empty;
            var bytes = reader.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        public static List<Packet> FragmentPacket(Packet originalPacket)
        {
            var originalData = originalPacket.Data ?? Array.Empty<byte>();
            var totalSize = originalData.Length;
            var fragments = new List<Packet>();

            int fragmentIndex = 0;
            int totalFragments = (totalSize + MaxFragmentSize - 1) / MaxFragmentSize;
            if (totalFragments == 0) totalFragments = 1;

            for (int offset = 0; offset < totalSize; offset += MaxFragmentSize)
            {
                int length = Math.Min(MaxFragmentSize, totalSize - offset);
                var fragmentData = new byte[length];
                Buffer.BlockCopy(originalData, offset, fragmentData, 0, length);

                var fragment = new Packet
                {
                    IsDropable = originalPacket.IsDropable,
                    SequenceNumber = originalPacket.SequenceNumber,
                    Type = originalPacket.Type,
                    Data = fragmentData,
                    IsFragmented = true,
                    FragmentIndex = fragmentIndex++,
                    TotalFragments = totalFragments,
                    FileName = originalPacket.FileName,
                    TransferId = originalPacket.TransferId,
                    TotalSize = originalPacket.TotalSize,
                    ChunkIndex = originalPacket.ChunkIndex,
                    TotalChunks = originalPacket.TotalChunks,
                    SenderRole = originalPacket.SenderRole,
                    TimestampTicks = originalPacket.TimestampTicks,
                    // ✅ Audio/Video fields
                    Width = originalPacket.Width,
                    Height = originalPacket.Height,
                    SampleRate = originalPacket.SampleRate,
                    Channels = originalPacket.Channels,
                    BitsPerSample = originalPacket.BitsPerSample,
                    AudioData = originalPacket.AudioData
                };

                fragments.Add(fragment);
            }

            if (totalSize == 0)
            {
                fragments.Add(new Packet
                {
                    IsDropable = originalPacket.IsDropable,
                    SequenceNumber = originalPacket.SequenceNumber,
                    Type = originalPacket.Type,
                    Data = Array.Empty<byte>(),
                    IsFragmented = false,
                    FragmentIndex = 0,
                    TotalFragments = 1,
                    FileName = originalPacket.FileName,
                    TransferId = originalPacket.TransferId,
                    TotalSize = originalPacket.TotalSize,
                    ChunkIndex = originalPacket.ChunkIndex,
                    TotalChunks = originalPacket.TotalChunks,
                    SenderRole = originalPacket.SenderRole,
                    TimestampTicks = originalPacket.TimestampTicks,
                    // ✅ Audio/Video fields
                    Width = originalPacket.Width,
                    Height = originalPacket.Height,
                    SampleRate = originalPacket.SampleRate,
                    Channels = originalPacket.Channels,
                    BitsPerSample = originalPacket.BitsPerSample,
                    AudioData = originalPacket.AudioData
                });
            }

            return fragments;
        }
    }
}