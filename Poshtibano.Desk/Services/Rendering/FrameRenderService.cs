using Poshtibano.Desk.Shared;
using Poshtibano.Desk.Shared.Tools;

namespace Poshtibano.Desk.Services.Rendering
{
    public class FrameRenderService : IDisposable
    {
        private readonly Control _targetControl;
        private readonly IH264Decoder _decoder;
        private readonly object _decoderLock = new object();
        private Bitmap _currentFrame;
        private Rectangle _lastRenderRect = Rectangle.Empty;
        private int _currentFrameHeight;
        private int _currentFrameWidth;
        private readonly SynchronizationContext _uiContext;

        public Rectangle LastRenderRect => _lastRenderRect;

        public event EventHandler<string> OnRenderError;

        public FrameRenderService(Control targetControl)
        {
            _targetControl = targetControl ?? throw new ArgumentNullException(nameof(targetControl));
            //_decoder = new H264Decoder();
            _decoder = new H264SharpDecoder();
            _uiContext = SynchronizationContext.Current;

            _targetControl.Paint += OnPaint;
        }

        public bool ProcessFrame(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return false;

            try
            {
                var decompressed = CompressionTools.Decompress(compressedData);

                if (_decoder != null && !_decoder.IsDisposed)
                {
                    Bitmap decodedFrame;
                    lock (_decoderLock)
                    {
                        decodedFrame = _decoder.Decode(decompressed);
                    }

                    if (decodedFrame != null)
                    {
                        Bitmap managedFrame = DeepCloneWithLockBits((Bitmap)decodedFrame);

                        if (managedFrame != null)
                        {
                            lock (_decoderLock)
                            {
                                _currentFrame?.Dispose();
                                _currentFrame = managedFrame;

                                Console.WriteLine();
                                _currentFrameHeight = _currentFrame.Height;
                                _currentFrameWidth = _currentFrame.Width;
                            }

                            InvalidateControl();
                            return true;
                        }
                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Frame processing error: {ex.Message}");
            }

            return false;
        }

        private Bitmap DeepCloneWithLockBits(Bitmap source)
        {
            if (source == null || source.Width <= 0 || source.Height <= 0)
                return null;

            Bitmap target = new Bitmap(source.Width, source.Height, source.PixelFormat);

            System.Drawing.Imaging.BitmapData sourceData = null;
            System.Drawing.Imaging.BitmapData targetData = null;

            try
            {
                Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);

                sourceData = source.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, source.PixelFormat);
                targetData = target.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, target.PixelFormat);

                int height = source.Height;
                int widthInBytes = Math.Abs(sourceData.Stride);

                int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(source.PixelFormat) / 8;
                int rowSize = source.Width * bytesPerPixel;

                unsafe
                {
                    byte* srcPtr = (byte*)sourceData.Scan0;
                    byte* destPtr = (byte*)targetData.Scan0;

                    for (int y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(
                            srcPtr + (y * sourceData.Stride),
                            destPtr + (y * targetData.Stride),
                            rowSize,
                            rowSize);
                    }
                }
            }
            catch (Exception ex)
            {
                target.Dispose();
                Console.WriteLine($"DeepClone Error: {ex.Message}");
                return null;
            }
            finally
            {
                if (sourceData != null) source.UnlockBits(sourceData);
                if (targetData != null) target.UnlockBits(targetData);
            }

            return target;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);

            if (_currentFrame == null)
                return;

            float sourceAspect = (float)_currentFrameWidth / _currentFrameHeight;
            float destAspect = (float)_targetControl.Width / _targetControl.Height;

            int renderWidth, renderHeight;
            int xOffset = 0, yOffset = 0;

            if (destAspect > sourceAspect)
            {
                // Letterbox (black bars on sides)
                renderHeight = _targetControl.Height;
                renderWidth = (int)(renderHeight * sourceAspect);
                xOffset = (_targetControl.Width - renderWidth) / 2;
            }
            else
            {
                // Pillarbox (black bars on top/bottom)
                renderWidth = _targetControl.Width;
                renderHeight = (int)(renderWidth / sourceAspect);
                yOffset = (_targetControl.Height - renderHeight) / 2;
            }

            _lastRenderRect = new Rectangle(xOffset, yOffset, renderWidth, renderHeight);

            try
            {
                lock (_decoderLock)
                {
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    e.Graphics.DrawImage(_currentFrame, _lastRenderRect);
                }
            }
            catch (AccessViolationException ex)
            {

            }
            catch (Exception ex)
            {

            }
            finally
            {
            }
        }

        private void InvalidateControl()
        {
            if (_uiContext != null)
            {
                _uiContext.Post(_ => _targetControl?.Invalidate(), null);
            }
            else
            {
                _targetControl?.Invoke(new Action(() => _targetControl.Invalidate()));
            }
        }

        public void Dispose()
        {
            if (_targetControl != null)
            {
                _targetControl.Paint -= OnPaint;
            }

            try
            {
                _currentFrame?.Dispose();
                _currentFrame = null;
                _decoder?.Dispose();
            }
            catch (Exception) { }

            OnRenderError = null;
        }
    }
}