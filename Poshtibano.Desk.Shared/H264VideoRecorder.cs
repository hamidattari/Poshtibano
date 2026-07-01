using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Poshtibano.Desk.Shared
{
    /// <summary>
    /// Records H264 video to Matroska (MKV) file format with variable frame rate support.
    /// Each frame is timestamped with its actual arrival time, so playback timing
    /// matches real-world timing regardless of how irregularly frames arrive.
    /// </summary>
    public class H264VideoRecorder : IDisposable
    {
        private FileStream _fileStream;
        private BinaryWriter _writer;
        private readonly object _locker = new object();
        private bool _isDisposed;
        private bool _isRecording;
        private string _outputPath;
        private Stopwatch _stopwatch;
        private List<FrameEntry> _frames;
        private int _width;
        private int _height;
        private bool _spsReceived;

        // MKV structure tracking
        private long _segmentSizePos;
        private long _clusterStart;
        private long _clusterSizePos;
        private bool _clusterOpen;
        private long _lastTimestampMs;
        private long _clusterTimestampMs;

        // Track info extracted from SPS
        private byte[] _spsData;
        private byte[] _ppsData;
        private long _trackEntryCodecPrivatePos;
        private int _codecPrivateLength;

        private struct FrameEntry
        {
            public long TimestampMs;
            public int Size;
            public bool IsKeyFrame;
        }

        public bool IsRecording => _isRecording;

        /// <summary>
        /// Start recording. No FPS parameter needed — timing is determined by actual frame arrival.
        /// </summary>
        public void StartRecording(string outputPath, int width = 1920, int height = 1080)
        {
            lock (_locker)
            {
                try
                {
                    Cleanup();

                    _outputPath = Path.ChangeExtension(outputPath, ".mkv");
                    _width = width;
                    _height = height;

                    _fileStream = new FileStream(_outputPath, FileMode.Create, FileAccess.ReadWrite);
                    _writer = new BinaryWriter(_fileStream);
                    _frames = new List<FrameEntry>();
                    _stopwatch = Stopwatch.StartNew();
                    _spsReceived = false;
                    _clusterOpen = false;
                    _lastTimestampMs = 0;
                    _clusterTimestampMs = 0;

                    WriteEbmlHeader();
                    WriteSegmentHeader();

                    _isRecording = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StartRecording error: {ex.Message}");
                    Cleanup();
                }
            }
        }

        /// <summary>
        /// Overload for backward compatibility — fps parameter is ignored.
        /// </summary>
        public void StartRecording(string outputPath, int width, int height, int fps)
        {
            StartRecording(outputPath, width, height);
        }

        public void WriteFrame(byte[] h264Data)
        {
            if (h264Data == null || h264Data.Length == 0) return;

            lock (_locker)
            {
                if (!_isRecording || _writer == null) return;

                try
                {
                    long timestampMs = _stopwatch.ElapsedMilliseconds;

                    // Extract SPS/PPS on first keyframe
                    if (!_spsReceived)
                    {
                        ExtractSpsAndPps(h264Data);
                        if (!_spsReceived) return;
                    }

                    bool isKeyFrame = IsKeyFrame(h264Data);

                    // Start a new cluster on keyframes or if cluster is too large (>5 seconds)
                    if (!_clusterOpen || (isKeyFrame && timestampMs - _clusterTimestampMs > 1000))
                    {
                        if (_clusterOpen) CloseCluster();
                        OpenCluster(timestampMs);
                    }

                    // Write SimpleBlock
                    short relativeTimestamp = (short)Math.Max(
                        short.MinValue,
                        Math.Min(short.MaxValue, timestampMs - _clusterTimestampMs));

                    WriteSimpleBlock(h264Data, relativeTimestamp, isKeyFrame);

                    _frames.Add(new FrameEntry
                    {
                        TimestampMs = timestampMs,
                        Size = h264Data.Length,
                        IsKeyFrame = isKeyFrame
                    });

                    _lastTimestampMs = timestampMs;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WriteFrame error: {ex.Message}");
                }
            }
        }

        public void StopRecording()
        {
            lock (_locker)
            {
                if (!_isRecording) return;
                _isRecording = false;
                _stopwatch?.Stop();

                try
                {
                    if (_frames != null && _frames.Count > 0)
                    {
                        if (_clusterOpen) CloseCluster();

                        // Write Cues (seek index) for keyframes
                        WriteCues();

                        // Update Segment size
                        UpdateSegmentSize();

                        Console.WriteLine($"MKV saved: {_outputPath} ({_frames.Count} frames, " +
                                          $"duration: {_lastTimestampMs}ms)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"StopRecording error: {ex.Message}");
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        #region NAL Parsing

        private void ExtractSpsAndPps(byte[] h264Data)
        {
            var nalUnits = ParseNalUnits(h264Data);
            foreach (var (offset, length, nalType) in nalUnits)
            {
                if (nalType == 7) // SPS
                {
                    _spsData = new byte[length];
                    Buffer.BlockCopy(h264Data, offset, _spsData, 0, length);
                }
                else if (nalType == 8) // PPS
                {
                    _ppsData = new byte[length];
                    Buffer.BlockCopy(h264Data, offset, _ppsData, 0, length);
                }
            }

            if (_spsData != null && _ppsData != null)
            {
                _spsReceived = true;
                WriteTrackEntry();
            }
        }

        private bool IsKeyFrame(byte[] h264Data)
        {
            var nalUnits = ParseNalUnits(h264Data);
            foreach (var (_, _, nalType) in nalUnits)
            {
                if (nalType == 5) return true; // IDR
            }
            return false;
        }

        private List<(int offset, int length, int nalType)> ParseNalUnits(byte[] data)
        {
            var result = new List<(int, int, int)>();
            int i = 0;
            while (i < data.Length - 4)
            {
                // Look for start code 0x00000001 or 0x000001
                int startCodeLen = 0;
                if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 0 && data[i + 3] == 1)
                    startCodeLen = 4;
                else if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 1)
                    startCodeLen = 3;

                if (startCodeLen > 0)
                {
                    int nalStart = i + startCodeLen;
                    int nalType = data[nalStart] & 0x1F;

                    // Find the end of this NAL unit
                    int nalEnd = data.Length;
                    for (int j = nalStart + 1; j < data.Length - 3; j++)
                    {
                        if ((data[j] == 0 && data[j + 1] == 0 && data[j + 2] == 0 && data[j + 3] == 1) ||
                            (data[j] == 0 && data[j + 1] == 0 && data[j + 2] == 1))
                        {
                            nalEnd = j;
                            break;
                        }
                    }

                    result.Add((nalStart, nalEnd - nalStart, nalType));
                    i = nalEnd;
                }
                else
                {
                    i++;
                }
            }
            return result;
        }

        #endregion

        #region MKV/EBML Writing

        private void WriteEbmlHeader()
        {
            // EBML element
            WriteEbmlId(0x1A45DFA3);
            var headerContent = new MemoryStream();
            using (var hw = new BinaryWriter(headerContent))
            {
                // EBMLVersion = 1
                WriteEbmlElement(hw, 0x4286, 1);
                // EBMLReadVersion = 1
                WriteEbmlElement(hw, 0x42F7, 1);
                // EBMLMaxIDLength = 4
                WriteEbmlElement(hw, 0x42F2, 4);
                // EBMLMaxSizeLength = 8
                WriteEbmlElement(hw, 0x42F3, 8);
                // DocType = "matroska"
                WriteEbmlElement(hw, 0x4282, "matroska");
                // DocTypeVersion = 4
                WriteEbmlElement(hw, 0x4287, 4);
                // DocTypeReadVersion = 2
                WriteEbmlElement(hw, 0x4285, 2);
            }
            byte[] content = headerContent.ToArray();
            WriteEbmlSize((ulong)content.Length);
            _writer.Write(content);
        }

        private void WriteSegmentHeader()
        {
            // Segment
            WriteEbmlId(0x18538067);
            _segmentSizePos = _fileStream.Position;
            // Write unknown size (8 bytes of 0xFF with first byte = 0x01)
            _writer.Write((byte)0x01);
            for (int i = 0; i < 7; i++) _writer.Write((byte)0xFF);

            // Segment Info
            WriteSegmentInfo();
        }

        private void WriteSegmentInfo()
        {
            WriteEbmlId(0x1549A966); // Info
            var infoContent = new MemoryStream();
            using (var iw = new BinaryWriter(infoContent))
            {
                // TimestampScale = 1000000 (1ms precision)
                WriteEbmlElement(iw, 0x2AD7B1, 1000000UL);
                // MuxingApp
                WriteEbmlElement(iw, 0x4D80, "Poshtibano.Desk.Shared");
                // WritingApp
                WriteEbmlElement(iw, 0x5741, "H264VideoRecorder");
            }
            byte[] content = infoContent.ToArray();
            WriteEbmlSize((ulong)content.Length);
            _writer.Write(content);
        }

        private void WriteTrackEntry()
        {
            // Tracks
            WriteEbmlId(0x1654AE6B);
            var tracksContent = new MemoryStream();
            using (var tw = new BinaryWriter(tracksContent))
            {
                // TrackEntry
                var trackContent = new MemoryStream();
                using (var tew = new BinaryWriter(trackContent))
                {
                    // TrackNumber = 1
                    WriteEbmlElement(tew, 0xD7, 1);
                    // TrackUID = 1
                    WriteEbmlElement(tew, 0x73C5, 1UL);
                    // TrackType = 1 (video)
                    WriteEbmlElement(tew, 0x83, 1);
                    // CodecID
                    WriteEbmlElement(tew, 0x86, "V_MPEG4/ISO/AVC");

                    // CodecPrivate (AVCDecoderConfigurationRecord)
                    byte[] codecPrivate = BuildAvcConfigRecord();
                    WriteEbmlId(tew, 0x63A2);
                    WriteEbmlSize(tew, (ulong)codecPrivate.Length);
                    tew.Write(codecPrivate);

                    // Video element
                    var videoContent = new MemoryStream();
                    using (var vw = new BinaryWriter(videoContent))
                    {
                        // PixelWidth
                        WriteEbmlElement(vw, 0xB0, (ulong)_width);
                        // PixelHeight
                        WriteEbmlElement(vw, 0xBA, (ulong)_height);
                    }
                    byte[] vc = videoContent.ToArray();
                    WriteEbmlId(tew, 0xE0);
                    WriteEbmlSize(tew, (ulong)vc.Length);
                    tew.Write(vc);
                }
                byte[] tc = trackContent.ToArray();
                WriteEbmlId(tw, 0xAE);
                WriteEbmlSize(tw, (ulong)tc.Length);
                tw.Write(tc);
            }
            byte[] content = tracksContent.ToArray();
            WriteEbmlSize((ulong)content.Length);
            _writer.Write(content);
        }

        private byte[] BuildAvcConfigRecord()
        {
            // AVCDecoderConfigurationRecord
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((byte)1); // configurationVersion
            bw.Write(_spsData[1]); // AVCProfileIndication
            bw.Write(_spsData[2]); // profile_compatibility
            bw.Write(_spsData[3]); // AVCLevelIndication
            bw.Write((byte)0xFF); // lengthSizeMinusOne = 3 (4 bytes NAL length)

            // SPS
            bw.Write((byte)0xE1); // numOfSequenceParameterSets = 1
            bw.Write((byte)(_spsData.Length >> 8));
            bw.Write((byte)(_spsData.Length & 0xFF));
            bw.Write(_spsData);

            // PPS
            bw.Write((byte)1); // numOfPictureParameterSets
            bw.Write((byte)(_ppsData.Length >> 8));
            bw.Write((byte)(_ppsData.Length & 0xFF));
            bw.Write(_ppsData);

            return ms.ToArray();
        }

        private void OpenCluster(long timestampMs)
        {
            WriteEbmlId(0x1F43B675); // Cluster
            _clusterSizePos = _fileStream.Position;
            // Unknown size
            _writer.Write((byte)0x01);
            for (int i = 0; i < 7; i++) _writer.Write((byte)0xFF);

            _clusterStart = _fileStream.Position;
            _clusterTimestampMs = timestampMs;

            // Cluster Timestamp
            WriteEbmlElement(0xE7, (ulong)timestampMs);

            _clusterOpen = true;
        }

        private void CloseCluster()
        {
            if (!_clusterOpen) return;

            long clusterEnd = _fileStream.Position;
            long clusterSize = clusterEnd - _clusterStart;

            // Go back and write the actual cluster size
            _fileStream.Position = _clusterSizePos;
            WriteEbmlFixedSize8((ulong)clusterSize);

            _fileStream.Position = clusterEnd;
            _clusterOpen = false;
        }

        private void WriteSimpleBlock(byte[] h264Data, short relativeTimestamp, bool isKeyFrame)
        {
            // Convert Annex B to AVCC format (replace start codes with NAL length)
            byte[] avccData = AnnexBToAvcc(h264Data);

            // SimpleBlock header: track number (1 byte EBML vint) + timestamp (2 bytes) + flags (1 byte)
            int headerSize = 1 + 2 + 1;
            int totalSize = headerSize + avccData.Length;

            WriteEbmlId(0xA3); // SimpleBlock
            WriteEbmlSize((ulong)totalSize);

            // Track number (EBML variable-length integer, track 1 = 0x81)
            _writer.Write((byte)0x81);

            // Relative timestamp (big-endian int16)
            _writer.Write((byte)(relativeTimestamp >> 8));
            _writer.Write((byte)(relativeTimestamp & 0xFF));

            // Flags: bit 0 = keyframe
            byte flags = isKeyFrame ? (byte)0x80 : (byte)0x00;
            _writer.Write(flags);

            // Frame data (AVCC format)
            _writer.Write(avccData);
        }

        private byte[] AnnexBToAvcc(byte[] annexB)
        {
            var nalUnits = ParseNalUnits(annexB);
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            foreach (var (offset, length, _) in nalUnits)
            {
                // Write 4-byte NAL length (big-endian)
                bw.Write((byte)(length >> 24));
                bw.Write((byte)(length >> 16));
                bw.Write((byte)(length >> 8));
                bw.Write((byte)(length & 0xFF));
                bw.Write(annexB, offset, length);
            }

            return ms.ToArray();
        }

        private void WriteCues()
        {
            // Simple cue points for keyframes
            WriteEbmlId(0x1C53BB6B); // Cues
            var cuesContent = new MemoryStream();
            using (var cw = new BinaryWriter(cuesContent))
            {
                foreach (var frame in _frames)
                {
                    if (!frame.IsKeyFrame) continue;

                    var pointContent = new MemoryStream();
                    using (var pw = new BinaryWriter(pointContent))
                    {
                        // CueTime
                        WriteEbmlElement(pw, 0xB3, (ulong)frame.TimestampMs);

                        // CueTrackPositions
                        var posContent = new MemoryStream();
                        using (var posW = new BinaryWriter(posContent))
                        {
                            // CueTrack = 1
                            WriteEbmlElement(posW, 0xF7, 1);
                            // CueClusterPosition (relative to Segment start)
                            // Note: simplified — in production you'd track actual positions
                            WriteEbmlElement(posW, 0xF1, (ulong)frame.TimestampMs);
                        }
                        byte[] pc = posContent.ToArray();
                        WriteEbmlId(pw, 0xB7);
                        WriteEbmlSize(pw, (ulong)pc.Length);
                        pw.Write(pc);
                    }
                    byte[] ptc = pointContent.ToArray();
                    WriteEbmlId(cw, 0xBB); // CuePoint
                    WriteEbmlSize(cw, (ulong)ptc.Length);
                    cw.Write(ptc);
                }
            }
            byte[] content = cuesContent.ToArray();
            WriteEbmlSize((ulong)content.Length);
            _writer.Write(content);
        }

        private void UpdateSegmentSize()
        {
            long fileEnd = _fileStream.Length;
            long segmentDataStart = _segmentSizePos + 8; // After the 8-byte size field
            long segmentSize = fileEnd - segmentDataStart;

            _fileStream.Position = _segmentSizePos;
            WriteEbmlFixedSize8((ulong)segmentSize);

            _fileStream.Position = fileEnd;
        }

        #endregion

        #region EBML Primitives

        private void WriteEbmlId(uint id)
        {
            WriteEbmlId(_writer, id);
        }

        private void WriteEbmlId(BinaryWriter w, uint id)
        {
            if (id <= 0xFF) { w.Write((byte)id); }
            else if (id <= 0xFFFF) { w.Write((byte)(id >> 8)); w.Write((byte)id); }
            else if (id <= 0xFFFFFF) { w.Write((byte)(id >> 16)); w.Write((byte)(id >> 8)); w.Write((byte)id); }
            else { w.Write((byte)(id >> 24)); w.Write((byte)(id >> 16)); w.Write((byte)(id >> 8)); w.Write((byte)id); }
        }

        private void WriteEbmlSize(ulong size)
        {
            WriteEbmlSize(_writer, size);
        }

        private void WriteEbmlSize(BinaryWriter w, ulong size)
        {
            if (size < 0x7F)
            {
                w.Write((byte)(size | 0x80));
            }
            else if (size < 0x3FFF)
            {
                w.Write((byte)((size >> 8) | 0x40));
                w.Write((byte)size);
            }
            else if (size < 0x1FFFFF)
            {
                w.Write((byte)((size >> 16) | 0x20));
                w.Write((byte)(size >> 8));
                w.Write((byte)size);
            }
            else if (size < 0x0FFFFFFF)
            {
                w.Write((byte)((size >> 24) | 0x10));
                w.Write((byte)(size >> 16));
                w.Write((byte)(size >> 8));
                w.Write((byte)size);
            }
            else
            {
                WriteEbmlFixedSize8(size);
            }
        }

        private void WriteEbmlFixedSize8(ulong size)
        {
            _writer.Write((byte)0x01);
            for (int i = 6; i >= 0; i--)
                _writer.Write((byte)(size >> (i * 8)));
        }

        private void WriteEbmlElement(uint id, ulong value)
        {
            WriteEbmlElement(_writer, id, value);
        }

        private void WriteEbmlElement(BinaryWriter w, uint id, ulong value)
        {
            WriteEbmlId(w, id);
            byte[] data = EncodeUnsignedInt(value);
            WriteEbmlSize(w, (ulong)data.Length);
            w.Write(data);
        }

        private void WriteEbmlElement(BinaryWriter w, uint id, int value)
        {
            WriteEbmlElement(w, id, (ulong)value);
        }

        private void WriteEbmlElement(BinaryWriter w, uint id, string value)
        {
            WriteEbmlId(w, id);
            byte[] data = Encoding.ASCII.GetBytes(value);
            WriteEbmlSize(w, (ulong)data.Length);
            w.Write(data);
        }

        private byte[] EncodeUnsignedInt(ulong value)
        {
            if (value == 0) return new byte[] { 0 };

            int byteCount = 0;
            ulong temp = value;
            while (temp > 0) { byteCount++; temp >>= 8; }

            byte[] result = new byte[byteCount];
            for (int i = byteCount - 1; i >= 0; i--)
            {
                result[i] = (byte)(value & 0xFF);
                value >>= 8;
            }
            return result;
        }

        #endregion

        private void Cleanup()
        {
            _isRecording = false;
            _spsReceived = false;
            _clusterOpen = false;

            try { _writer?.Dispose(); } catch { }
            try { _fileStream?.Dispose(); } catch { }

            _writer = null;
            _fileStream = null;
            _frames = null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            StopRecording();
        }
    }
}