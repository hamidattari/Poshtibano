using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services.Media
{
    /// <summary>
    /// Service for playing received audio using NAudio.
    /// </summary>
    public class AudioPlaybackService : IDisposable
    {
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _bufferedProvider;

        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly int _bitsPerSample;
        private readonly int _jitterBufferMs;

        private bool _isPlaying;
        private bool _isDisposed;
        private bool _isMuted;

        public event Action<string> OnPlaybackError;

        public bool IsPlaying => _isPlaying;
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                if (_waveOut != null)
                {
                    _waveOut.Volume = value ? 0f : 1f;
                }
            }
        }

        public AudioPlaybackService(int sampleRate = 16000, int channels = 1, int bitsPerSample = 16, int jitterBufferMs = 100)
        {
            _sampleRate = sampleRate;
            _channels = channels;
            _bitsPerSample = bitsPerSample;
            _jitterBufferMs = jitterBufferMs;

            Console.WriteLine($"[{DateTime.Now}] 🔊 AudioPlaybackService created - Rate: {sampleRate}Hz, Channels: {channels}");
        }

        public void Start()
        {
            if (_isPlaying || _isDisposed) return;

            try
            {
                var waveFormat = new WaveFormat(_sampleRate, _bitsPerSample, _channels);

                _bufferedProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferDuration = TimeSpan.FromMilliseconds(_jitterBufferMs * 10),
                    DiscardOnBufferOverflow = true
                };

                _waveOut = new WaveOutEvent
                {
                    DesiredLatency = _jitterBufferMs
                };

                _waveOut.Init(_bufferedProvider);
                _waveOut.PlaybackStopped += OnPlaybackStopped;
                _waveOut.Play();

                _isPlaying = true;

                Console.WriteLine($"[{DateTime.Now}] ▶️ Audio playback started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error starting playback: {ex.Message}");
                OnPlaybackError?.Invoke(ex.Message);
            }
        }

        public void AddSamples(byte[] audioData)
        {
            if (!_isPlaying || _bufferedProvider == null || audioData == null) return;

            try
            {
                _bufferedProvider.AddSamples(audioData, 0, audioData.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error adding audio samples: {ex.Message}");
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Playback error: {e.Exception.Message}");
                OnPlaybackError?.Invoke(e.Exception.Message);
            }
        }

        public async Task StopAsync()
        {
            if (!_isPlaying) return;

            await Task.Run(() =>
            {
                try
                {
                    _waveOut?.Stop();
                    _bufferedProvider?.ClearBuffer();
                    Console.WriteLine($"[{DateTime.Now}] ⏹️ Audio playback stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Error stopping playback: {ex.Message}");
                }
                finally
                {
                    _isPlaying = false;
                }
            });
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                if (_waveOut != null)
                {
                    _waveOut.PlaybackStopped -= OnPlaybackStopped;

                    if (_isPlaying)
                    {
                        _waveOut.Stop();
                    }

                    _waveOut.Dispose();
                    _waveOut = null;
                }

                _bufferedProvider = null;
            }
            catch { }

            OnPlaybackError = null;
            _isPlaying = false;

            Console.WriteLine($"[{DateTime.Now}] ✅ AudioPlaybackService disposed");
        }
    }
}