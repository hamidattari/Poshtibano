using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Poshtibano.Desk.Services.Media
{
    /// <summary>
    /// Service for capturing video from webcam using AForge.NET. 
    /// </summary>
    public class VideoCaptureService : IDisposable
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private readonly int _deviceIndex;
        private readonly int _targetFps;
        private readonly int _quality;

        private bool _isCapturing;
        private bool _isDisposed;
        private uint _sequenceNumber;
        private DateTime _lastFrameTime;
        private readonly object _lock = new object();

        private readonly TimeSpan _minFrameInterval;
        private int _frameCount = 0;

        public event Action<byte[], int, int, long, uint> OnFrameCaptured;
        public event Action<string> OnCaptureError;
        public event Action OnCaptureStarted;
        public event Action OnCaptureStopped;

        public bool IsCapturing => _isCapturing;
        public Size FrameSize { get; private set; }

        public VideoCaptureService(int deviceIndex = 0, int targetFps = 15, int quality = 50)
        {
            _deviceIndex = deviceIndex;
            _targetFps = Math.Clamp(targetFps, 1, 30);
            _quality = Math.Clamp(quality, 10, 100);
            _minFrameInterval = TimeSpan.FromMilliseconds(1000.0 / _targetFps);
            _sequenceNumber = 0;

            RefreshDeviceList();

            Console.WriteLine($"[{DateTime.Now}] 📷 VideoCaptureService created - Device: {deviceIndex}, FPS:  {targetFps}, Quality: {quality}");
        }

        public void RefreshDeviceList()
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                Console.WriteLine($"[{DateTime.Now}] 📷 Found {_videoDevices.Count} video device(s)");

                for (int i = 0; i < _videoDevices.Count; i++)
                {
                    Console.WriteLine($"[{DateTime.Now}]   Device {i}:  {_videoDevices[i].Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error enumerating video devices: {ex.Message}");
                _videoDevices = null;
            }
        }

        public int DeviceCount => _videoDevices?.Count ?? 0;

        public void Start()
        {
            lock (_lock)
            {
                if (_isCapturing || _isDisposed)
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ VideoCaptureService already capturing or disposed");
                    return;
                }

                try
                {
                    if (_videoDevices == null || _videoDevices.Count == 0)
                    {
                        var error = "No video capture devices found";
                        Console.WriteLine($"[{DateTime.Now}] ❌ {error}");
                        OnCaptureError?.Invoke(error);
                        return;
                    }

                    // ✅ Select best device with real ones
                    int selectedDeviceIndex = SelectBestDevice();

                    if (selectedDeviceIndex < 0)
                    {
                        var error = "No suitable video device found";
                        Console.WriteLine($"[{DateTime.Now}] ❌ {error}");
                        OnCaptureError?.Invoke(error);
                        return;
                    }

                    string monikerString = _videoDevices[selectedDeviceIndex].MonikerString;
                    string deviceName = _videoDevices[selectedDeviceIndex].Name;

                    Console.WriteLine($"[{DateTime.Now}] 📷 Opening device #{selectedDeviceIndex}:  {deviceName}");

                    _videoSource = new VideoCaptureDevice(monikerString);

                    Console.WriteLine($"[{DateTime.Now}] 📷 Video capabilities: {_videoSource.VideoCapabilities.Length}");

                    if (_videoSource.VideoCapabilities.Length > 0)
                    {
                        var capability = SelectBestCapability(_videoSource.VideoCapabilities);
                        _videoSource.VideoResolution = capability;
                        FrameSize = new Size(capability.FrameSize.Width, capability.FrameSize.Height);
                        Console.WriteLine($"[{DateTime.Now}] 📷 Selected resolution: {capability.FrameSize.Width}x{capability.FrameSize.Height}");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ No video capabilities, using default");
                        FrameSize = new Size(640, 480); // Default
                    }

                    _videoSource.NewFrame += OnNewFrame;
                    _videoSource.VideoSourceError += OnVideoSourceError;

                    Console.WriteLine($"[{DateTime.Now}] 📷 Starting video source.. .");
                    _videoSource.Start();

                    Thread.Sleep(500);

                    if (_videoSource.IsRunning)
                    {
                        _isCapturing = true;
                        _sequenceNumber = 0;
                        _frameCount = 0;
                        _lastFrameTime = DateTime.MinValue;

                        OnCaptureStarted?.Invoke();
                        Console.WriteLine($"[{DateTime.Now}] ✅ Video capture started:   {deviceName}");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now}] ❌ Video source failed to start!");
                        OnCaptureError?.Invoke("Video source failed to start");
                        CleanupVideoSource();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] ❌ Error starting video capture: {ex.Message}");
                    OnCaptureError?.Invoke($"Failed to start video capture:  {ex.Message}");
                    CleanupVideoSource();
                }
            }
        }

        private int SelectBestDevice()
        {
            if (_videoDevices == null || _videoDevices.Count == 0)
                return -1;

            string[] virtualCameraKeywords = {
                "virtual", "obs", "manycam", "xsplit", "droidcam",
                "ivcam", "epoccam", "snap camera", "streamlabs"
            };

            int bestRealCameraIndex = -1;
            int firstVirtualIndex = -1;

            for (int i = 0; i < _videoDevices.Count; i++)
            {
                string name = _videoDevices[i].Name.ToLowerInvariant();
                bool isVirtual = false;

                foreach (var keyword in virtualCameraKeywords)
                {
                    if (name.Contains(keyword))
                    {
                        isVirtual = true;
                        break;
                    }
                }

                // ✅ ريالeal cameras usually have capabilities.
                var tempDevice = new VideoCaptureDevice(_videoDevices[i].MonikerString);
                bool hasCapabilities = tempDevice.VideoCapabilities?.Length > 0;
                tempDevice = null; // Cleanup

                Console.WriteLine($"[{DateTime.Now}]   Device {i}:  {_videoDevices[i].Name} - Virtual: {isVirtual}, HasCaps: {hasCapabilities}");

                if (!isVirtual && hasCapabilities)
                {
                    if (bestRealCameraIndex < 0)
                    {
                        bestRealCameraIndex = i;
                    }
                }
                else if (isVirtual || !hasCapabilities)
                {
                    if (firstVirtualIndex < 0)
                    {
                        firstVirtualIndex = i;
                    }
                }
            }

            if (bestRealCameraIndex >= 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ✅ Selected real camera: {_videoDevices[bestRealCameraIndex].Name}");
                return bestRealCameraIndex;
            }

            if (firstVirtualIndex >= 0)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Only virtual camera available:  {_videoDevices[firstVirtualIndex].Name}");
                return firstVirtualIndex;
            }

            Console.WriteLine($"[{DateTime.Now}] ⚠️ Using first available device");
            return 0;
        }

        private VideoCapabilities SelectBestCapability(VideoCapabilities[] capabilities)
        {
            foreach (var cap in capabilities)
            {
                Console.WriteLine($"[{DateTime.Now}]   Cap: {cap.FrameSize.Width}x{cap.FrameSize.Height} @ {cap.AverageFrameRate}fps");
            }

            int[] preferredWidths = { 640, 800, 720, 480, 1280 };

            foreach (int width in preferredWidths)
            {
                foreach (var cap in capabilities)
                {
                    if (cap.FrameSize.Width == width)
                        return cap;
                }
            }

            VideoCapabilities best = capabilities[0];
            int bestDiff = Math.Abs(best.FrameSize.Width - 640) + Math.Abs(best.FrameSize.Height - 480);

            foreach (var cap in capabilities)
            {
                int diff = Math.Abs(cap.FrameSize.Width - 640) + Math.Abs(cap.FrameSize.Height - 480);
                if (diff < bestDiff)
                {
                    best = cap;
                    bestDiff = diff;
                }
            }

            return best;
        }

        public async Task StopAsync()
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_isCapturing)
                        return;

                    Console.WriteLine($"[{DateTime.Now}] 📷 Stopping video capture...");

                    try
                    {
                        if (_videoSource != null && _videoSource.IsRunning)
                        {
                            _videoSource.SignalToStop();
                            _videoSource.WaitForStop();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] ⚠️ Error stopping video capture: {ex.Message}");
                    }
                    finally
                    {
                        _isCapturing = false;
                        CleanupVideoSource();
                        OnCaptureStopped?.Invoke();
                        Console.WriteLine($"[{DateTime.Now}] ⏸️ Video capture stopped, total frames: {_frameCount}");
                    }
                }
            });
        }


        private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            _frameCount++;

            if (_frameCount <= 5 || _frameCount % 100 == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] 📹 OnNewFrame called, frame #{_frameCount}");
            }

            if (!_isCapturing || _isDisposed)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Frame ignored:  isCapturing={_isCapturing}, isDisposed={_isDisposed}");
                return;
            }

            // Frame rate limiting
            var now = DateTime.UtcNow;
            if (now - _lastFrameTime < _minFrameInterval)
            {
                return; // Skip frame for rate limiting
            }

            _lastFrameTime = now;

            try
            {
                // ✅ Clone frame
                Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();

                FrameSize = new Size(bitmap.Width, bitmap.Height);

                // Encode frame as JPEG
                byte[] frameData = EncodeFrame(bitmap);

                // Dispose bitmap
                bitmap.Dispose();

                if (frameData != null && frameData.Length > 0)
                {
                    _sequenceNumber++;

                    if (_sequenceNumber <= 5 || _sequenceNumber % 30 == 0)
                    {
                        Console.WriteLine($"[{DateTime.Now}] 📹 Frame encoded: {frameData.Length} bytes, {FrameSize.Width}x{FrameSize.Height}, seq={_sequenceNumber}");
                    }

                    // ✅ Fire event
                    var handler = OnFrameCaptured;
                    if (handler != null)
                    {
                        handler.Invoke(
                            frameData,
                            FrameSize.Width,
                            FrameSize.Height,
                            now.Ticks,
                            _sequenceNumber
                        );
                    }
                    else
                    {
                        if (_sequenceNumber <= 5)
                        {
                            Console.WriteLine($"[{DateTime.Now}] ⚠️ OnFrameCaptured handler is null!");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] ⚠️ Frame encoding failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ❌ Error processing video frame: {ex.Message}");
            }
        }

        private byte[] EncodeFrame(Bitmap bitmap)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var encoder = GetEncoder(ImageFormat.Jpeg);
                    if (encoder == null)
                    {
                        // Fallback:  save without encoder params
                        bitmap.Save(ms, ImageFormat.Jpeg);
                    }
                    else
                    {
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)_quality);
                        bitmap.Save(ms, encoder, encoderParams);
                    }
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] ⚠️ Error encoding frame: {ex.Message}");
                return null;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }

        private void OnVideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            Console.WriteLine($"[{DateTime.Now}] ❌ Video source error: {eventArgs.Description}");
            OnCaptureError?.Invoke(eventArgs.Description);
        }

        private void CleanupVideoSource()
        {
            if (_videoSource != null)
            {
                _videoSource.NewFrame -= OnNewFrame;
                _videoSource.VideoSourceError -= OnVideoSourceError;

                if (_videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                }

                _videoSource = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            Console.WriteLine($"[{DateTime.Now}] 🗑️ Disposing VideoCaptureService");

            try
            {
                StopAsync().Wait(3000);
            }
            catch { }

            CleanupVideoSource();

            OnFrameCaptured = null;
            OnCaptureError = null;
            OnCaptureStarted = null;
            OnCaptureStopped = null;

            Console.WriteLine($"[{DateTime.Now}] ✅ VideoCaptureService disposed");
        }
    }
}