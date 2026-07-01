using Poshtibano.Common;
using Poshtibano.Desk.Shared;
using Poshtibano.Desk.Shared.Tools;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Poshtibano.Desk.Services.Capture
{
    public enum CursorRenderMode
    {
        /// <summary>
        /// No cursor drawn on the captured image.
        /// </summary>
        None,

        /// <summary>
        /// Draw the actual system cursor icon.
        /// </summary>
        SystemCursor,

        /// <summary>
        /// Draw a small red circle at the cursor position.
        /// </summary>
        RedDot
    }

    public class FrameCapturedEventArgs : EventArgs
    {
        public byte[] EncodedData { get; set; }
        public long CaptureTimeMs { get; set; }
        public ulong SequenceNumber { get; set; }
    }

    public class ScreenCaptureService : IDisposable
    {
        #region P/Invoke

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
                                          IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon,
                                              int cxWidth, int cyWidth, uint istepIfAniCur,
                                              IntPtr hbrFlickerFreeDraw, uint diFlags);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int CURSOR_SHOWING = 0x00000001;
        private const uint DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        #endregion

        private const long QUALITY_COEF = 10000;

        private Rectangle _screenBounds;
        private IH264Encoder _encoder;
        private readonly object _encoderLock = new object();
        private CancellationTokenSource _captureCts;
        private Task _captureTask;
        private bool _isCapturing;
        private ulong _sequence;
        private float _fps = 30f;
        private long _quality = 40L;
        private bool _fullframe = false;
        private MonitorInfo _currentMonitor;

        // Cursor settings
        private CursorRenderMode _cursorMode = CursorRenderMode.SystemCursor;
        private int _redDotRadius = 6;
        private Color _redDotColor = Color.Red;
        private float _redDotBorderWidth = 1.5f;

        public event EventHandler<FrameCapturedEventArgs> OnFrameCaptured;
        public event EventHandler<string> OnCaptureError;

        public bool IsCapturing => _isCapturing;

        public CursorRenderMode CursorMode
        {
            get => _cursorMode;
            set => _cursorMode = value;
        }

        /// <summary>
        /// Radius of the red dot cursor in pixels (default: 6).
        /// </summary>
        public int RedDotRadius
        {
            get => _redDotRadius;
            set
            {
                if (value >= 1 && value <= 50)
                    _redDotRadius = value;
            }
        }

        /// <summary>
        /// Color of the dot cursor (default: Red).
        /// </summary>
        public Color DotColor
        {
            get => _redDotColor;
            set => _redDotColor = value;
        }

        public float Fps
        {
            get => _fps;
            set
            {
                if (value > 0 && value <= 120)
                {
                    _fps = value;
                    if (_encoder == null || _encoder.IsDisposed) return;
                    _encoder.UpdateSettings(newFps: (int)value);
                }
            }
        }

        public long Quality
        {
            get => _quality;
            set
            {
                if (value >= 1 && value <= 100)
                {
                    _quality = value;
                    if (_encoder == null || _encoder.IsDisposed) return;
                    _encoder.UpdateSettings(newBitrate: (int)value * 10000);
                }
            }
        }

        public bool FullFrame
        {
            get => _fullframe;
            set
            {
                _fullframe = value;
                _encoder?.UpdateSettings(fullFrame: value);
            }
        }

        public void UpdateSettings(Poshtibano.Common.Settings settings)
        {
            _encoder.UpdateSettings((int)settings.ImageQuality * 10000, (int)settings.Fps, settings.FullFrame);
        }

        public MonitorInfo CurrentMonitor => _currentMonitor;

        public ScreenCaptureService(Rectangle screenBounds, float fps = 30f, long quality = 40L)
        {
            _screenBounds = screenBounds;
            _fps = fps;
            _quality = quality;
            _encoder = new H264SharpEncoder();
            _encoder.Initialize(screenBounds.Width, screenBounds.Height, (int)fps, (int)(quality * QUALITY_COEF), _fullframe);
        }

        public void ChangeCaptureMonitor(MonitorInfo monitor)
        {
            if (monitor == null) return;

            lock (_encoderLock)
            {
                _currentMonitor = monitor;
                _screenBounds = monitor.ScreenBounds;

                _encoder?.Dispose();
                _encoder = new H264SharpEncoder();
                _encoder.Initialize(
                    _screenBounds.Width,
                    _screenBounds.Height,
                    (int)_fps,
                    (int)(_quality * QUALITY_COEF),
                    _fullframe
                );

                Console.WriteLine($"[{DateTime.Now}] 🖥️ Change monitor:  #{monitor.Index} ({monitor.Name}) - Bounds: {monitor.ScreenBounds}");
            }
        }

        public void UpdateCaptureBounds(Rectangle bounds)
        {
            lock (_encoderLock)
            {
                _screenBounds = bounds;
                _encoder?.Dispose();
                _encoder = new H264SharpEncoder();
                _encoder.Initialize(_screenBounds.Width, _screenBounds.Height, (int)_fps, (int)(_quality * QUALITY_COEF), _fullframe);
                Console.WriteLine($"[{DateTime.Now}] 🖥️ Change screen bounds: {bounds}");
            }
        }

        public void Start()
        {
            if (_isCapturing)
            {
                Console.WriteLine($"[{DateTime.Now}] Capturing is in progress");
                return;
            }

            _captureCts = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureLoopAsync(_captureCts.Token), _captureCts.Token);
            Console.WriteLine($"[{DateTime.Now}] Start capturing");
        }

        public async Task StopAsync()
        {
            if (!_isCapturing && _captureTask == null)
                return;

            _captureCts?.Cancel();

            if (_captureTask != null)
            {
                try
                {
                    await Task.WhenAny(_captureTask, Task.Delay(2000));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Error in capturing: {ex.Message}");
                }
                finally
                {
                    _captureTask = null;
                }
            }

            _captureCts?.Dispose();
            _captureCts = null;

            Console.WriteLine($"[{DateTime.Now}] Stop capturing");
        }

        private async Task CaptureLoopAsync(CancellationToken cancellationToken)
        {
            _isCapturing = true;
            var sw = new Stopwatch();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    sw.Restart();

                    try
                    {
                        var frame = CaptureScreen();
                        if (frame != null)
                        {
                            byte[] encoded = null;

                            lock (_encoderLock)
                            {
                                if (_encoder != null && !_encoder.IsDisposed)
                                {
                                    try
                                    {
                                        encoded = _encoder.Encode(frame);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[{DateTime.Now}] Error in encoding: {ex.Message}");
                                        OnCaptureError?.Invoke(this, $"Encoding failed: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    _encoder = new H264SharpEncoder();
                                    _encoder.Initialize(_screenBounds.Width, _screenBounds.Height, (int)_fps, (int)(_quality * QUALITY_COEF));
                                }
                            }

                            frame.Dispose();

                            if (encoded != null && encoded.Length > 0)
                            {
                                var compressed = CompressionTools.Compress(encoded);
                                var args = new FrameCapturedEventArgs
                                {
                                    EncodedData = compressed,
                                    CaptureTimeMs = sw.ElapsedMilliseconds,
                                    SequenceNumber = Interlocked.Increment(ref _sequence)
                                };

                                OnFrameCaptured?.Invoke(this, args);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Error in capturing: {ex.Message}");
                        OnCaptureError?.Invoke(this, $"Failed capturing: {ex.Message}");
                    }

                    sw.Stop();
                    var delay = (int)(1000 / _fps) - (int)sw.ElapsedMilliseconds;
                    if (delay > 0)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[{DateTime.Now}] Capturing stopped");
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private Bitmap CaptureScreen()
        {
            var bitmap = new Bitmap(_screenBounds.Width, _screenBounds.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
            {
                var hdcBitmap = g.GetHdc();
                var hdcScreen = GetDC(IntPtr.Zero);

                try
                {
                    BitBlt(hdcBitmap, 0, 0, _screenBounds.Width, _screenBounds.Height,
                           hdcScreen, _screenBounds.X, _screenBounds.Y, 0x00CC0020);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdcScreen);
                    g.ReleaseHdc(hdcBitmap);
                }

                // Draw cursor on the captured image
                if (_cursorMode != CursorRenderMode.None)
                {
                    DrawCursor(g);
                }
            }

            return bitmap;
        }

        private void DrawCursor(Graphics g)
        {
            var cursorInfo = new CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

            if (!GetCursorInfo(ref cursorInfo))
                return;

            if ((cursorInfo.flags & CURSOR_SHOWING) == 0)
                return;

            // Get the DPI scale factor (e.g. 1.25 for 125%)
            float scale = _currentMonitor?.Scale ?? 1.0f;

            // GetCursorInfo returns logical (DPI-unaware) coordinates.
            // BitBlt captures physical pixels.
            // We need to convert logical cursor position to physical pixel position.
            int cursorX = (int)((cursorInfo.ptScreenPos.x - _screenBounds.X) * scale);
            int cursorY = (int)((cursorInfo.ptScreenPos.y - _screenBounds.Y) * scale);

            // Skip if cursor is outside the capture area
            if (cursorX < -64 || cursorX > _screenBounds.Width + 64 ||
                cursorY < -64 || cursorY > _screenBounds.Height + 64)
                return;

            switch (_cursorMode)
            {
                case CursorRenderMode.SystemCursor:
                    DrawSystemCursor(g, cursorInfo.hCursor, cursorX, cursorY, scale);
                    break;

                case CursorRenderMode.RedDot:
                    DrawDotCursor(g, cursorX, cursorY);
                    break;
            }
        }

        /// <summary>
        /// Draws the actual system cursor icon onto the graphics surface.
        /// The cursor icon is scaled to match the DPI of the monitor.
        /// </summary>
        private void DrawSystemCursor(Graphics g, IntPtr hCursor, int x, int y, float scale)
        {
            if (hCursor == IntPtr.Zero) return;

            // Get hotspot to position the cursor icon correctly
            int hotspotX = 0, hotspotY = 0;
            if (GetIconInfo(hCursor, out ICONINFO iconInfo))
            {
                // Hotspot is in logical coordinates, scale it to physical
                hotspotX = (int)(iconInfo.xHotspot * scale);
                hotspotY = (int)(iconInfo.yHotspot * scale);

                // Clean up GDI objects from GetIconInfo
                if (iconInfo.hbmMask != IntPtr.Zero)
                    DeleteObject(iconInfo.hbmMask);
                if (iconInfo.hbmColor != IntPtr.Zero)
                    DeleteObject(iconInfo.hbmColor);
            }

            x -= hotspotX;
            y -= hotspotY;

            // Default cursor size is 32x32 logical pixels; scale to physical
            int cursorSize = (int)(32 * scale);

            IntPtr hdc = g.GetHdc();
            try
            {
                // DrawIconEx with explicit size so the cursor scales with DPI
                DrawIconEx(hdc, x, y, hCursor, cursorSize, cursorSize, 0, IntPtr.Zero, DI_NORMAL);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        /// <summary>
        /// Draws a small filled dot with a contrasting border at the cursor position.
        /// The border ensures visibility on both light and dark backgrounds.
        /// </summary>
        private void DrawDotCursor(Graphics g, int x, int y)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int r = _redDotRadius;
            var rect = new RectangleF(x - r, y - r, r * 2, r * 2);

            // White border for visibility on dark backgrounds
            using (var borderPen = new Pen(Color.White, _redDotBorderWidth + 1f))
            {
                g.DrawEllipse(borderPen, rect);
            }

            // Filled dot
            using (var brush = new SolidBrush(_redDotColor))
            {
                g.FillEllipse(brush, rect);
            }

            // Thin dark border for visibility on light backgrounds
            using (var pen = new Pen(Color.FromArgb(180, 0, 0, 0), _redDotBorderWidth))
            {
                g.DrawEllipse(pen, rect);
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();

            lock (_encoderLock)
            {
                _encoder?.Dispose();
            }

            OnFrameCaptured = null;
            OnCaptureError = null;
        }
    }
}