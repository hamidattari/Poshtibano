using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services.Media
{
    /// <summary>
    /// Service for capturing audio from microphone using NAudio. 
    /// </summary>
    public class AudioCaptureService : IDisposable
    {
        private WaveInEvent _waveIn;
        private readonly int _deviceIndex;
        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly int _bitsPerSample;

        private bool _isCapturing;
        private bool _isDisposed;
        private uint _sequenceNumber;

        public event Action<byte[], int, int, int, long, uint> OnAudioCaptured;
        public event Action<string> OnCaptureError;

        public bool IsCapturing => _isCapturing;

        public AudioCaptureService(int deviceIndex = 0, int sampleRate = 16000, int channels = 1, int bitsPerSample = 16)
        {
            _deviceIndex = deviceIndex;
            _sampleRate = sampleRate;
            _channels = channels;
            _bitsPerSample = bitsPerSample;
            _sequenceNumber = 0;

            Console.WriteLine($"[{DateTime.Now}] 🎤 AudioCaptureService created - Device: {deviceIndex}, Rate: {sampleRate}Hz");
        }

        public void Start()
        {
            if (_isCapturing || _isDisposed) return;

            try
            {
                int deviceCount = WaveInEvent.DeviceCount;
                if (deviceCount == 0)
                {
                    OnCaptureError?.Invoke("No audio input devices found");
                    return;
                }

                int actualDevice = Math.Min(_deviceIndex, deviceCount - 1);

                _waveIn = new WaveInEvent
                {
                    DeviceNumber = actualDevice,
                    WaveFormat = new WaveFormat(_sampleRate, _bitsPerSample, _channels),
                    BufferMilliseconds = 100
                };

                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;

                _waveIn.StartRecording();
                _isCapturing = true;
                _sequenceNumber = 0;

                Console.WriteLine($"[{DateTime.Now}] ✅ Audio capture started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error starting audio capture: {ex.Message}");
                OnCaptureError?.Invoke(ex.Message);
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_isCapturing || e.BytesRecorded == 0) return;

            try
            {
                _sequenceNumber++;

                byte[] audioData = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, audioData, 0, e.BytesRecorded);

                OnAudioCaptured?.Invoke(
                    audioData,
                    _sampleRate,
                    _channels,
                    _bitsPerSample,
                    DateTime.UtcNow.Ticks,
                    _sequenceNumber
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error processing audio:  {ex.Message}");
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            _isCapturing = false;

            if (e.Exception != null)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Recording stopped with error: {e.Exception.Message}");
                OnCaptureError?.Invoke(e.Exception.Message);
            }
        }

        public async Task StopAsync()
        {
            if (!_isCapturing) return;

            await Task.Run(() =>
            {
                try
                {
                    _waveIn?.StopRecording();
                    Console.WriteLine($"[{DateTime.Now}] ⏹️ Audio capture stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Error stopping audio:  {ex.Message}");
                }
                finally
                {
                    _isCapturing = false;
                }
            });
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= OnDataAvailable;
                    _waveIn.RecordingStopped -= OnRecordingStopped;

                    if (_isCapturing)
                    {
                        _waveIn.StopRecording();
                    }

                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
            catch { }

            OnAudioCaptured = null;
            OnCaptureError = null;

            Console.WriteLine($"[{DateTime.Now}] ✅ AudioCaptureService disposed");
        }
    }
}