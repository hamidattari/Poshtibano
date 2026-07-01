using FFmpeg.AutoGen;
using H264Sharp;
using Microsoft.Diagnostics.Tracing.Parsers.JScript;
using Poshtibano.Desk.Shared.Settings;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using ImageFormat = H264Sharp.ImageFormat;

namespace Poshtibano.Desk.Shared
{
    public interface IH264Encoder
    {
        public bool IsDisposed { get; }

        public void Initialize(int w, int h, int fps = 25, int bitrate = 400000, bool fullFrame = false);
        public byte[] Encode(Bitmap bmp);

        public void
            UpdateSettings(int? newBitrate = null, int? newFps = null, bool? fullFrame = false);

        public void Dispose();
    }

    public interface IH264Decoder
    {
        public bool IsDisposed { get; }

        public Bitmap Decode(byte[] h264Data);
        public void Dispose();
    }

    public unsafe class H264Encoder : IH264Encoder
    {
        private AVCodec* pCodec;
        private AVCodecContext* pCodecContext;
        private AVPacket* pPacket;
        private AVFrame* pFrame;
        private SwsContext* pSwsContext;

        private int _width;
        private int _height;
        private int _fps;
        private int _bitrate;

        public bool IsDisposed { get; private set; }

        public H264Encoder() { }

        public void Initialize(int w, int h, int fps = 25, int bitrate = 400000, bool fullFrame = false)
        {
            ffmpeg.av_log_set_level(-8);

            _width = w;
            _height = h;
            _fps = fps;
            _bitrate = bitrate;

            pCodec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
            if (pCodec == null) throw new InvalidOperationException("H.264 encoder not found.");

            pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            if (pCodecContext == null) throw new InvalidOperationException("Could not allocate AVCodecContext.");

            pCodecContext->width = _width;
            pCodecContext->height = _height;
            pCodecContext->time_base = new AVRational { num = 1, den = fps };
            pCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P; pCodecContext->bit_rate = bitrate;

            ffmpeg.av_opt_set(pCodecContext->priv_data, "preset", "ultrafast", 0); ffmpeg.av_opt_set(pCodecContext->priv_data, "tune", "zerolatency", 0); pCodecContext->gop_size = 10;
            if (ffmpeg.avcodec_open2(pCodecContext, pCodec, null) < 0)
                throw new InvalidOperationException("Could not open H.264 codec.");

            pPacket = ffmpeg.av_packet_alloc();
            pFrame = ffmpeg.av_frame_alloc();

            pFrame->format = (int)AVPixelFormat.AV_PIX_FMT_YUV420P;
            pFrame->width = _width;
            pFrame->height = _height;
            ffmpeg.av_frame_get_buffer(pFrame, 0);

            pSwsContext = ffmpeg.sws_getContext(
                _width, _height, AVPixelFormat.AV_PIX_FMT_BGRA, _width, _height, AVPixelFormat.AV_PIX_FMT_YUV420P, ffmpeg.SWS_FAST_BILINEAR, null, null, null);

            if (pSwsContext == null) throw new InvalidOperationException("Could not initialize the SwS context.");

        }

        public byte[] Encode(Bitmap bmp)
        {
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte_ptrArray4 inData = default;
            int_array4 inLinesize = default;
            inData[0] = (byte*)bmpData.Scan0;
            inLinesize[0] = bmpData.Stride;

            ffmpeg.sws_scale(pSwsContext, inData, inLinesize, 0, _height, pFrame->data, pFrame->linesize);

            bmp.UnlockBits(bmpData);

            int ret = ffmpeg.avcodec_send_frame(pCodecContext, pFrame);
            if (ret < 0) return null;
            ret = ffmpeg.avcodec_receive_packet(pCodecContext, pPacket);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
            {
                return null;
            }
            else if (ret < 0)
            {
                throw new InvalidOperationException("Error during encoding.");
            }

            byte[] encodedData = new byte[pPacket->size];
            Marshal.Copy((IntPtr)pPacket->data, encodedData, 0, pPacket->size);

            ffmpeg.av_packet_unref(pPacket);
            return encodedData;
        }

        public void Dispose()
        {
            IsDisposed = true;

            try
            {
                fixed (SwsContext** ppSwsContext = &pSwsContext)
                {
                    ffmpeg.sws_freeContext(*ppSwsContext);
                }
                fixed (AVFrame** ppFrame = &pFrame)
                {
                    ffmpeg.av_frame_free(ppFrame);
                }
                fixed (AVPacket** ppPacket = &pPacket)
                {
                    ffmpeg.av_packet_free(ppPacket);
                }
                fixed (AVCodecContext** ppCodecContext = &pCodecContext)
                {
                    ffmpeg.avcodec_free_context(ppCodecContext);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void UpdateSettings(int? newBitrate = null, int? newFps = null, bool? fullFrame = false)
        {
            if (pCodecContext == null) return;

            _bitrate = newBitrate ?? _bitrate;
            pCodecContext->bit_rate = _bitrate;

            pCodecContext->rc_max_rate = _bitrate;
            pCodecContext->rc_buffer_size = _bitrate;

            string bitrateStr = newBitrate.ToString();
            ffmpeg.av_opt_set(pCodecContext->priv_data, "b", bitrateStr, 0);

            _fps = newFps ?? _fps;
            pCodecContext->time_base = new AVRational { num = 1, den = _fps };
            pCodecContext->framerate = new AVRational { num = _fps, den = 1 };
        }
    }

    public unsafe class H264Decoder : IH264Decoder
    {
        private AVCodec* pCodec;
        private AVCodecContext* pCodecContext;
        private AVPacket* pPacket;
        private AVFrame* pFrame;
        private SwsContext* pSwsContext;

        public H264Decoder()
        {
            ffmpeg.av_log_set_level(-8);

            pCodec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (pCodec == null) throw new InvalidOperationException("H.264 decoder not found.");

            pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            if (pCodecContext == null) throw new InvalidOperationException("Could not allocate AVCodecContext.");

            if (ffmpeg.avcodec_open2(pCodecContext, pCodec, null) < 0)
                throw new InvalidOperationException("Could not open H.264 decoder.");

            pPacket = ffmpeg.av_packet_alloc();
            pFrame = ffmpeg.av_frame_alloc();
        }

        public bool IsDisposed { get; private set; }

        public Bitmap Decode(byte[] h264Data)
        {
            Bitmap resultBitmap = null;

            fixed (byte* pData = h264Data)
            {
                pPacket->data = pData;
                pPacket->size = h264Data.Length;

                int ret = ffmpeg.avcodec_send_packet(pCodecContext, pPacket);
                if (ret < 0) return null;

                ret = ffmpeg.avcodec_receive_frame(pCodecContext, pFrame);

                if (ret == 0)
                {
                    if (pSwsContext == null)
                    {
                        pSwsContext = ffmpeg.sws_getContext(
                            pFrame->width, pFrame->height, (AVPixelFormat)pFrame->format,
                            pFrame->width, pFrame->height, AVPixelFormat.AV_PIX_FMT_BGRA, ffmpeg.SWS_FAST_BILINEAR, null, null, null);

                        if (pSwsContext == null) throw new InvalidOperationException("Could not initialize SwS context for decoding.");
                    }

                    resultBitmap = new Bitmap(pFrame->width, pFrame->height, PixelFormat.Format32bppArgb);
                    BitmapData bmpData = resultBitmap.LockBits(
                        new Rectangle(0, 0, pFrame->width, pFrame->height),
                        ImageLockMode.WriteOnly,
                        PixelFormat.Format32bppArgb);

                    byte_ptrArray4 outData = default;
                    int_array4 outLinesize = default;
                    outData[0] = (byte*)bmpData.Scan0;
                    outLinesize[0] = bmpData.Stride;

                    ffmpeg.sws_scale(pSwsContext, pFrame->data, pFrame->linesize, 0, pFrame->height, outData, outLinesize);

                    resultBitmap.UnlockBits(bmpData);
                }
            }
            return resultBitmap;
        }

        public void Dispose()
        {
            IsDisposed = true;

            fixed (SwsContext** ppSwsContext = &pSwsContext)
            {
                ffmpeg.sws_freeContext(*ppSwsContext);
            }
            fixed (AVFrame** ppFrame = &pFrame)
            {
                ffmpeg.av_frame_free(ppFrame);
            }
            fixed (AVPacket** ppPacket = &pPacket)
            {
                ffmpeg.av_packet_free(ppPacket);
            }
            fixed (AVCodecContext** ppCodecContext = &pCodecContext)
            {
                ffmpeg.avcodec_free_context(ppCodecContext);
            }
        }
    }

    public class H264SharpEncoder : IH264Encoder
    {
        private H264Sharp.H264Encoder encoder;
        private int _width;
        private int _height;
        private int _fps;
        private int _bitrate;
        private object _encoderLocker = new object();

        private H264VideoRecorder _recorder;
        private readonly object _recorderLocker = new object();
        public bool IsRecording => _recorder?.IsRecording ?? false;
        public bool _recorderInitiated = false;
        private string _videoPath = string.Empty;
        public bool IsDisposed { get; private set; }

        public H264SharpEncoder()
        {

        }

        public void Initialize(int width, int height, int fps = 25, int bitrate = 400000, bool fullFrame = false)
        {
            _width = width;
            _height = height;
            _fps = fps;
            _bitrate = bitrate;

            Console.WriteLine($"encoder width: {_width}x{_height}");

            encoder = new H264Sharp.H264Encoder();
            var param = GetEncoderInitParams(width, height, fps, bitrate, fullFrame);

            encoder.Initialize(param);
        }

        private TagEncParamExt GetEncoderInitParams(int w, int h, int fps, int bitrate, bool fullFrame)
        {
            var param = encoder.GetDefaultParameters();
            bitrate *= 2;

            param.iUsageType = EUsageType.CAMERA_VIDEO_REAL_TIME;

            param.iPicWidth = w;
            param.iPicHeight = h;
            param.iTargetBitrate = bitrate;
            param.iRCMode = RC_MODES.RC_BITRATE_MODE;
            param.fMaxFrameRate = fps;

            param.iSpatialLayerNum = 1;
            param.sSpatialLayers[0].iVideoWidth = w;
            param.sSpatialLayers[0].iVideoHeight = h;
            param.sSpatialLayers[0].fFrameRate = fps;
            param.sSpatialLayers[0].iSpatialBitrate = bitrate;

            param.sSpatialLayers[0].iMaxSpatialBitrate = (int)(bitrate * 1.5);

            param.sSpatialLayers[0].uiProfileIdc = EProfileIdc.PRO_HIGH;
            param.sSpatialLayers[0].uiLevelIdc = ELevelIdc.LEVEL_UNKNOWN;
            param.sSpatialLayers[0].iDLayerQp = 24;

            param.sSpatialLayers[0].sSliceArgument.uiSliceMode = SliceModeEnum.SM_SINGLE_SLICE;

            param.iComplexityMode = ECOMPLEXITY_MODE.HIGH_COMPLEXITY;
            param.uiIntraPeriod = fullFrame ? 1 : (uint)fps;

            param.iNumRefFrame = 0;

            param.iMaxBitrate = (int)(bitrate * 1.5);
            param.iPaddingFlag = 0;
            param.iEntropyCodingModeFlag = 1;

            param.bEnableFrameSkip = true;

            param.iMultipleThreadIdc = 0;

            param.bEnableLongTermReference = false;
            param.iLTRRefNum = 0;

            param.bEnableDenoise = false;
            param.iLoopFilterDisableIdc = 0;

            param.bEnableBackgroundDetection = true;
            param.bEnableSceneChangeDetect = true;
            param.bEnableAdaptiveQuant = true;

            param.uiMaxNalSize = 0;

            return param;
        }

        private TagEncParamExt GetOptimizedParams(int w, int h, int fps, int bitrate)
        {
            var param = encoder.GetDefaultParameters();

            param.iUsageType = EUsageType.SCREEN_CONTENT_REAL_TIME;

            param.iPicWidth = w;
            param.iPicHeight = h;
            param.iTargetBitrate = bitrate;
            param.iMaxBitrate = (int)(bitrate * 1.5);

            param.iRCMode = RC_MODES.RC_BITRATE_MODE;
            param.fMaxFrameRate = fps;

            param.iSpatialLayerNum = 1;
            param.sSpatialLayers[0].iVideoWidth = w;
            param.sSpatialLayers[0].iVideoHeight = h;
            param.sSpatialLayers[0].fFrameRate = fps;
            param.sSpatialLayers[0].iSpatialBitrate = bitrate;
            param.sSpatialLayers[0].iMaxSpatialBitrate = (int)(bitrate * 1.5);

            param.sSpatialLayers[0].uiProfileIdc = EProfileIdc.PRO_HIGH;

            param.sSpatialLayers[0].sSliceArgument.uiSliceMode = SliceModeEnum.SM_SINGLE_SLICE;

            param.uiIntraPeriod = (uint)(fps);

            param.iComplexityMode = ECOMPLEXITY_MODE.HIGH_COMPLEXITY;
            param.bEnableFrameSkip = false;
            param.iMultipleThreadIdc = 0;

            param.bEnableDenoise = false;
            param.bEnableBackgroundDetection = true;
            param.bEnableAdaptiveQuant = true;
            param.bEnableLongTermReference = true;

            param.iMinQp = 18;
            param.iMaxQp = 36;

            return param;
        }

        public void UpdateSettings(int? newBitrate = null, int? newFps = null, bool? fullFrame = false)
        {
            lock (_encoderLocker)
            {
                if (encoder == null || IsDisposed) return;

                encoder.Dispose();
                encoder = new H264Sharp.H264Encoder();
                var param = GetEncoderInitParams(_width, _height, newFps ?? _fps, newBitrate ?? _bitrate, fullFrame ?? false);
                encoder.Initialize(param);

                //int isFullFrame;
                //if (fullFrame == true) isFullFrame = 1;
                //else isFullFrame = newFps.HasValue ? newFps.Value : _fps;

                //encoder.SetOption(ENCODER_OPTION.ENCODER_OPTION_FRAME_RATE, newFps ?? _fps);
                //encoder.SetOption(ENCODER_OPTION.ENCODER_OPTION_BITRATE, newBitrate ?? _bitrate);
                //encoder.SetOption(ENCODER_OPTION.ENCODER_OPTION_MAX_BITRATE, newBitrate.HasValue ? newBitrate.Value * 1.5 : _bitrate * 1.5);
                //encoder.SetOption(ENCODER_OPTION.ENCODER_OPTION_IDR_INTERVAL, isFullFrame);
            }
        }

        public void StartRecording(string outputPath, int width = 0, int height = 0)
        {
            lock (_recorderLocker)
            {
                _recorder?.Dispose();
                _recorder = new H264VideoRecorder();
                _recorder.StartRecording(outputPath, width, height);
                _videoPath = outputPath;
            }
        }

        public void StopRecording()
        {
            lock (_recorderLocker)
            {
                _recorder?.StopRecording();
                _recorder?.Dispose();
                //_recorder = null;
            }
        }

        public byte[] Encode(Bitmap bmp)
        {
            lock (_encoderLocker)
            {
                RgbImage rgbImage = BitmapToRgbImage(bmp);

                if (!encoder.Encode(rgbImage, out EncodedData[] encodedFrames))
                {
                    return null;
                }

                byte[] h264Data = encodedFrames.GetAllBytes();

                if (!_recorderInitiated && SettingsManager.Instance.RecordIncomingSessions)
                {
                    _recorderInitiated = true;
                    _videoPath = Path.Combine(
                        SettingsManager.Instance.RecordIncomingFolder,
                        $"{DateTime.Now:yyyy-MM-dd HH-mm-ss} from {SettingsManager.SessionName}.mkv");

                    StartRecording(_videoPath, _width, _height);
                }

                lock (_recorderLocker)
                {
                    _recorder?.WriteFrame(h264Data);
                }

                return h264Data;
            }
        }

        public (byte[] data, int size, FrameType type) EncodeWithInfo(Bitmap bmp)
        {
            RgbImage rgbImage = BitmapToRgbImage(bmp);

            if (!encoder.Encode(rgbImage, out EncodedData[] encodedFrames))
            {
                return (null, 0, FrameType.Invalid);
            }

            byte[] data = encodedFrames.GetAllBytes();
            int size = data.Length;
            FrameType frameType = encodedFrames[0].FrameType;

            return (data, size, frameType);
        }

        private RgbImage BitmapToRgbImage(Bitmap bmp)
        {
            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            );

            ImageFormat format = ImageFormat.Bgra;

            RgbImage rgbImage = new RgbImage(
                format,
                bmp.Width,
                bmp.Height,
                bmpData.Stride,
                bmpData.Scan0
            );

            bmp.UnlockBits(bmpData);
            return rgbImage;
        }

        public void Dispose()
        {
            IsDisposed = true;
            encoder?.Dispose();

            lock (_recorderLocker)
            {
                _recorder?.StopRecording();
                _recorder?.Dispose();
                _recorder = null;
            }
        }
    }

    public class H264SharpDecoder : IH264Decoder
    {
        private readonly H264Sharp.H264Decoder decoder;
        public bool IsDisposed { get; private set; }
        private readonly object _locker = new object();

        private H264VideoRecorder _recorder;
        private readonly object _recorderLocker = new object();
        public bool IsRecording => _recorder?.IsRecording ?? false;
        public bool _recorderInitiated = false;
        private string _videoPath = string.Empty;

        public H264SharpDecoder()
        {
            decoder = new H264Sharp.H264Decoder();
            decoder.Initialize();
        }

        public void StartRecording(string outputPath, int width = 0, int height = 0)
        {
            lock (_recorderLocker)
            {
                _recorder?.Dispose();
                _recorder = new H264VideoRecorder();
                _recorder.StartRecording(outputPath, width, height);
                _videoPath = outputPath;
            }
        }

        public void StopRecording()
        {
            lock (_recorderLocker)
            {
                _recorder?.StopRecording();
                _recorder?.Dispose();
                //_recorder = null;
            }
        }

        public Bitmap Decode(byte[] h264Data)
        {
            if (IsDisposed) return null;

            if (h264Data == null || h264Data.Length == 0)
                return null;

            lock (_recorderLocker)
            {
                _recorder?.WriteFrame(h264Data);
            }

            if (!decoder.Decode(h264Data, 0, h264Data.Length, noDelay: true, out DecodingState state, out YUVImagePointer yuvPtr))
            {
                return null;
            }

            int width = yuvPtr.Width;
            int height = yuvPtr.Height;

            if (!_recorderInitiated && SettingsManager.Instance.RecordOutcomingSessions)
            {
                _recorderInitiated = true;
                _videoPath = Path.Combine(
                    SettingsManager.Instance.RecordOutcomingFolder,
                    $"{DateTime.Now:yyyy-MM-dd HH-mm-ss} from {SettingsManager.SessionName}");

                StartRecording(_videoPath, width, height);
            }

            RgbImage rgbOutput = new RgbImage(ImageFormat.Rgb, width, height);

            try
            {
                lock (_locker)
                {
                    if (decoder.Decode(h264Data, 0, h264Data.Length, noDelay: true, out DecodingState state2, ref rgbOutput))
                        return RgbImageToBitmap(rgbOutput);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] Error in H264SharpDecoder.Decode() : {ex.Message}");
            }

            return null;
        }

        public bool TryDecode(byte[] h264Data, out RgbImage rgbImage)
        {
            rgbImage = null;

            if (h264Data == null || h264Data.Length == 0)
                return false;

            lock (_recorderLocker)
            {
                _recorder?.WriteFrame(h264Data);
            }

            // First, decode once to receive the size.
            if (!decoder.Decode(h264Data, 0, h264Data.Length, noDelay: true, out DecodingState state, out YUVImagePointer yuvPtr))
            {
                return false;
            }

            int width = yuvPtr.Width;
            int height = yuvPtr.Height;

            rgbImage = new RgbImage(ImageFormat.Rgb, width, height);

            return decoder.Decode(h264Data, 0, h264Data.Length, noDelay: true, out DecodingState state2, ref rgbImage);
        }

        private Bitmap RgbImageToBitmap(RgbImage img)
        {
            PixelFormat pixelFormat = PixelFormat.Format24bppRgb;

            switch (img.Format)
            {
                case ImageFormat.Rgb:
                case ImageFormat.Bgr:
                    pixelFormat = PixelFormat.Format24bppRgb;
                    break;
                case ImageFormat.Rgba:
                case ImageFormat.Bgra:
                    pixelFormat = PixelFormat.Format32bppArgb;
                    break;
            }

            Bitmap bmp = new Bitmap(
                img.Width,
                img.Height,
                img.Stride,
                pixelFormat,
                img.NativeBytes
            );

            return bmp;
        }

        public void Dispose()
        {
            lock (_locker)
            {
                IsDisposed = true;

                lock (_recorderLocker)
                {
                    _recorder?.StopRecording();
                    _recorder?.Dispose();
                    _recorder = null;
                }

                decoder?.Dispose();
            }
        }
    }
}

