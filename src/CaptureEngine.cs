using System;
using System.IO;
using System.Runtime.InteropServices;

// COM 接口声明在 NativeMethods 类内，用别名引入到本文件的顶级作用域
using IMFActivate = PeekShot.NativeMethods.IMFActivate;
using IMFAttributes = PeekShot.NativeMethods.IMFAttributes;
using IMFMediaBuffer = PeekShot.NativeMethods.IMFMediaBuffer;
using IMFMediaSource = PeekShot.NativeMethods.IMFMediaSource;
using IMFMediaType = PeekShot.NativeMethods.IMFMediaType;
using IMFSample = PeekShot.NativeMethods.IMFSample;
using IMFSourceReader = PeekShot.NativeMethods.IMFSourceReader;
using IWICBitmapEncoder = PeekShot.NativeMethods.IWICBitmapEncoder;
using IWICBitmapFrameEncode = PeekShot.NativeMethods.IWICBitmapFrameEncode;
using IWICBitmapSource = PeekShot.NativeMethods.IWICBitmapSource;
using IWICImagingFactory = PeekShot.NativeMethods.IWICImagingFactory;
using IWICImagingFactoryLegacy = PeekShot.NativeMethods.IWICImagingFactoryLegacy;
using IWICStream = PeekShot.NativeMethods.IWICStream;
using WICRect = PeekShot.NativeMethods.WICRect;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace PeekShot
{
    internal enum CaptureResult
    {
        NotRun = 0,
        Saved = 1,
        NoCamera = 2,
        DeviceOpenFailed = 3,
        CaptureFailed = 4,
        EncodeFailed = 5
    }

    /// <summary>
    /// 通过 Media Foundation（Source Reader）从摄像头采集一帧并用 WIC 编码为 JPEG。
    /// 在独立后台线程上运行（Execute），由主线程 Join 看门狗限时。
    /// </summary>
    internal class CaptureJob
    {
        private readonly Config _config;
        private readonly string _photoPath;

        public CaptureResult Result = CaptureResult.NotRun;
        public string Detail = "";

        public CaptureJob(Config config, string photoPath)
        {
            _config = config;
            _photoPath = photoPath;
        }

        public void Execute()
        {
            int coInitHr = NativeMethods.CoInitializeEx(
                IntPtr.Zero, (uint)NativeMethods.COINIT_MULTITHREADED);
            bool coInitSucceeded = (coInitHr == 0 || coInitHr == 1); // S_OK / S_FALSE
            bool mfStarted = false;

            try
            {
                if (coInitHr != 0 && coInitHr != 1 && coInitHr != NativeMethods.RPC_E_CHANGED_MODE)
                {
                    SetResult(CaptureResult.CaptureFailed,
                        "CoInitializeEx 失败：0x" + coInitHr.ToString("X8"));
                    return;
                }

                int hr = NativeMethods.MFStartup(NativeMethods.MF_VERSION, NativeMethods.MFSTARTUP_LITE);
                if (hr < 0)
                {
                    SetResult(CaptureResult.CaptureFailed,
                        "MFStartup 失败：0x" + hr.ToString("X8"));
                    return;
                }
                mfStarted = true;

                RunPipeline();
            }
            catch (Exception ex)
            {
                if (Result != CaptureResult.Saved)
                {
                    SetResult(CaptureResult.CaptureFailed, "拍照过程异常：" + ex.Message);
                }
            }
            finally
            {
                if (mfStarted) NativeMethods.MFShutdown();
                if (coInitSucceeded) NativeMethods.CoUninitialize();
            }
        }

        private void SetResult(CaptureResult result, string detail)
        {
            if (Result == CaptureResult.Saved) return;
            Result = result;
            Detail = detail;
            Log.Write("拍照[" + result + "] " + detail);
        }

        private void RunPipeline()
        {
            IMFAttributes enumAttrs = null;
            IMFSourceReader reader = null;
            IMFMediaSource mediaSource = null;
            IMFAttributes readerAttrs = null;

            try
            {
                // ---------- 枚举摄像头 ----------
                int hr = NativeMethods.MFCreateAttributes(out enumAttrs, 2);
                if (hr < 0)
                {
                    SetResult(CaptureResult.CaptureFailed, "MFCreateAttributes 失败：0x" + hr.ToString("X8"));
                    return;
                }

                Guid sourceType = NativeMethods.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE;
                Guid vidcapGuid = NativeMethods.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID;
                hr = enumAttrs.SetGUID(ref sourceType, ref vidcapGuid);
                if (hr < 0)
                {
                    SetResult(CaptureResult.CaptureFailed, "设置设备源类型失败：0x" + hr.ToString("X8"));
                    return;
                }

                IntPtr activateArray;
                int deviceCount;
                hr = NativeMethods.MFEnumDeviceSources(enumAttrs, out activateArray, out deviceCount);
                if (hr < 0)
                {
                    SetResult(CaptureResult.NoCamera, "MFEnumDeviceSources 失败：0x" + hr.ToString("X8"));
                    return;
                }
                if (deviceCount == 0 || activateArray == IntPtr.Zero)
                {
                    SetResult(CaptureResult.NoCamera,
                        "未枚举到任何摄像头设备（常见原因：系统设置中已关闭\"让桌面应用访问相机\"，或无摄像头）");
                    return;
                }

                // ---------- 选择设备 ----------
                IMFActivate chosen = null;
                string chosenName = null;
                string deviceList = "";

                try
                {
                    for (int i = 0; i < deviceCount; i++)
                    {
                        IntPtr raw = Marshal.ReadIntPtr(activateArray, i * IntPtr.Size);
                        if (raw == IntPtr.Zero) continue;
                        object rcw = Marshal.GetObjectForIUnknown(raw);
                        Marshal.Release(raw); // 数组持有的引用转移完毕，保留 RCW 自己的
                        IMFActivate activate = (IMFActivate)rcw;

                        string name = ReadStringAttribute(activate, NativeMethods.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME);
                        deviceList += (deviceList.Length == 0 ? "" : "; ") + name;

                        bool matches =
                            _config.CameraName.Length > 0 &&
                            string.Equals(name.Trim(), _config.CameraName.Trim(), StringComparison.OrdinalIgnoreCase);

                        if (chosen == null || matches)
                        {
                            if (chosen != null) NativeMethods.ReleaseComObject(chosen);
                            chosen = activate;
                            chosenName = name;
                            if (matches) break;
                            continue; // 首个设备作后备，继续找精确匹配
                        }
                        NativeMethods.ReleaseComObject(activate);
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem(activateArray);
                }

                if (chosen == null)
                {
                    SetResult(CaptureResult.NoCamera, "设备数组为空");
                    return;
                }

                if (_config.CameraName.Length > 0 &&
                    !string.Equals(chosenName.Trim(), _config.CameraName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Log.Write("配置的摄像头\"" + _config.CameraName + "\"未找到，回退到第一个设备");
                }
                Log.Write("摄像头列表[" + deviceCount + "]: " + deviceList + "；选用: " + chosenName);

                // ---------- 激活设备 ----------
                IntPtr pSource;
                Guid iidSource = NativeMethods.IID_IMFMediaSource;
                hr = chosen.ActivateObject(ref iidSource, out pSource);
                if (hr < 0)
                {
                    SetResult(CaptureResult.DeviceOpenFailed,
                        "ActivateObject 失败：0x" + hr.ToString("X8"));
                    return;
                }
                mediaSource = (IMFMediaSource)Marshal.GetObjectForIUnknown(pSource);
                Marshal.Release(pSource); // RCW 已持有自己的引用

                // ---------- 创建 Source Reader ----------
                hr = NativeMethods.MFCreateAttributes(out readerAttrs, 1);
                if (hr >= 0)
                {
                    Guid processingKey = NativeMethods.MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING;
                    readerAttrs.SetUINT32(ref processingKey, 1);
                    hr = NativeMethods.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttrs, out reader);
                }
                if (hr < 0 || reader == null)
                {
                    SetResult(CaptureResult.DeviceOpenFailed,
                        "MFCreateSourceReaderFromMediaSource 失败：0x" + hr.ToString("X8"));
                    return;
                }

                uint stream = NativeMethods.MF_SOURCE_READER_FIRST_VIDEO_STREAM;
                hr = reader.SetStreamSelection(stream, 1);
                if (hr < 0)
                {
                    SetResult(CaptureResult.DeviceOpenFailed,
                        "SetStreamSelection 失败：0x" + hr.ToString("X8"));
                    return;
                }

                // ---------- 采集一帧 ----------
                byte[] rawFrame;
                int width;
                int height;
                int stride;
                Guid subtype;
                if (!ReadFrame(reader, stream, out rawFrame, out width, out height, out stride, out subtype))
                {
                    return; // 结果已设置
                }

                // ---------- 归一化为紧凑 top-down BGRA ----------
                byte[] bgra = ToTopDownBgra(rawFrame, width, height, stride, subtype);
                if (bgra == null)
                {
                    SetResult(CaptureResult.CaptureFailed,
                        "不支持的输出子类型：" + subtype.ToString("B"));
                    return;
                }

                // ---------- WIC 编码 JPEG ----------
                string encodeError;
                if (!SaveJpeg(_photoPath, bgra, width, height, out encodeError))
                {
                    SetResult(CaptureResult.EncodeFailed, encodeError);
                    return;
                }

                FileInfo info = new FileInfo(_photoPath);
                if (!info.Exists || info.Length == 0)
                {
                    SetResult(CaptureResult.EncodeFailed, "JPEG 文件为空或未生成");
                    return;
                }

                Result = CaptureResult.Saved;
                Detail = "已保存 " + width + "x" + height + "（" + info.Length + " 字节）→ " + _photoPath;
                Log.Write("拍照成功: " + Detail);
            }
            finally
            {
                NativeMethods.ReleaseComObject(reader);
                NativeMethods.ReleaseComObject(readerAttrs);
                NativeMethods.ReleaseComObject(mediaSource);
                NativeMethods.ReleaseComObject(enumAttrs);
            }
        }

        // ==================== 帧读取与格式协商 ====================

        private bool ReadFrame(IMFSourceReader reader, uint stream,
            out byte[] rawFrame, out int width, out int height, out int stride, out Guid subtype)
        {
            rawFrame = null;
            width = 0;
            height = 0;
            stride = 0;
            subtype = Guid.Empty;

            // 1) 请求 RGB32；失败则退回 partial type（仅声明主类型），由 Source Reader 自选
            IMFMediaType nativeType;
            int hr = reader.GetNativeMediaType(stream, 0, out nativeType);
            if (hr < 0)
            {
                SetResult(CaptureResult.DeviceOpenFailed, "GetNativeMediaType 失败：0x" + hr.ToString("X8"));
                return false;
            }

            IMFMediaType rgb32Type = null;
            IMFMediaType partialType = null;
            IMFMediaType currentType = null;
            try
            {
                hr = NativeMethods.MFCreateMediaType(out rgb32Type);
                if (hr >= 0)
                {
                    hr = rgb32Type.CopyAllItems((IMFAttributes)nativeType);
                    if (hr >= 0)
                    {
                        Guid majorKey = NativeMethods.MF_MT_MAJOR_TYPE;
                        Guid majorVideo = NativeMethods.MFMediaType_Video;
                        rgb32Type.SetGUID(ref majorKey, ref majorVideo);
                        Guid subKey = NativeMethods.MF_MT_SUBTYPE;
                        Guid rgb32 = NativeMethods.MFVideoFormat_RGB32;
                        rgb32Type.SetGUID(ref subKey, ref rgb32);
                        hr = reader.SetCurrentMediaType(stream, IntPtr.Zero, rgb32Type);
                    }
                }

                if (hr < 0)
                {
                    // partial type 回退：Source Reader 自动选择可转换的类型
                    hr = NativeMethods.MFCreateMediaType(out partialType);
                    if (hr >= 0)
                    {
                        Guid majorKey = NativeMethods.MF_MT_MAJOR_TYPE;
                        Guid majorVideo = NativeMethods.MFMediaType_Video;
                        partialType.SetGUID(ref majorKey, ref majorVideo);
                        hr = reader.SetCurrentMediaType(stream, IntPtr.Zero, partialType);
                    }
                    if (hr < 0)
                    {
                        SetResult(CaptureResult.CaptureFailed,
                            "无法协商输出格式（RGB32 与 partial type 均失败）");
                        return false;
                    }
                    Log.Write("RGB32 协商失败，partial type 回退生效");
                }

                hr = reader.GetCurrentMediaType(stream, out currentType);
                if (hr < 0)
                {
                    SetResult(CaptureResult.CaptureFailed, "GetCurrentMediaType 失败：0x" + hr.ToString("X8"));
                    return false;
                }

                Guid subKey2 = NativeMethods.MF_MT_SUBTYPE;
                if (currentType.GetGUID(ref subKey2, out subtype) < 0)
                {
                    SetResult(CaptureResult.CaptureFailed, "读取输出子类型失败");
                    return false;
                }

                ulong frameSize;
                Guid sizeKey = NativeMethods.MF_MT_FRAME_SIZE;
                if (currentType.GetUINT64(ref sizeKey, out frameSize) < 0)
                {
                    SetResult(CaptureResult.CaptureFailed, "读取帧尺寸失败");
                    return false;
                }
                width = (int)(frameSize >> 32);
                height = (int)(frameSize & 0xFFFFFFFFUL);
                if (width <= 0 || height <= 0)
                {
                    SetResult(CaptureResult.CaptureFailed,
                        "帧尺寸非法: " + width + "x" + height);
                    return false;
                }

                // 步长：优先 MF_MT_DEFAULT_STRIDE；缺失时按子类型推算
                Guid strideKey = NativeMethods.MF_MT_DEFAULT_STRIDE;
                uint strideValue;
                if (currentType.GetUINT32(ref strideKey, out strideValue) >= 0)
                {
                    stride = unchecked((int)strideValue);
                }
                else if (subtype == NativeMethods.MFVideoFormat_RGB32)
                {
                    stride = width * 4;
                }
                else if (subtype == NativeMethods.MFVideoFormat_RGB24)
                {
                    stride = (width * 3 + 3) & ~3;
                }
                else
                {
                    stride = width; // NV12
                }

                Log.Write("输出格式: " + subtype.ToString("B") + " " + width + "x" + height +
                          " stride=" + stride);
            }
            finally
            {
                NativeMethods.ReleaseComObject(currentType);
                NativeMethods.ReleaseComObject(partialType);
                NativeMethods.ReleaseComObject(rgb32Type);
                NativeMethods.ReleaseComObject(nativeType);
            }

            // 2) 读取帧：null sample（格式切换等中间事件）跳过，最多尝试 10 次
            int samplesTaken = 0;
            int target = _config.SkipFrames + 1;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                IMFSample sample = null;
                try
                {
                    uint actualIndex;
                    uint streamFlags;
                    long timestamp;
                    hr = reader.ReadSample(stream, 0,
                        out actualIndex, out streamFlags, out timestamp, out sample);
                    if (hr < 0)
                    {
                        SetResult(CaptureResult.CaptureFailed,
                            "ReadSample 失败：0x" + hr.ToString("X8"));
                        return false;
                    }
                    if (sample == null) continue; // 中间事件，继续读

                    samplesTaken++;
                    if (samplesTaken < target) continue; // 丢弃预热帧

                    // 取出像素数据
                    IMFMediaBuffer buffer;
                    hr = sample.ConvertToContiguousBuffer(out buffer);
                    if (hr < 0)
                    {
                        SetResult(CaptureResult.CaptureFailed,
                            "ConvertToContiguousBuffer 失败：0x" + hr.ToString("X8"));
                        return false;
                    }
                    try
                    {
                        IntPtr pData;
                        int maxLength;
                        int currentLength;
                        hr = buffer.Lock(out pData, out maxLength, out currentLength);
                        if (hr < 0)
                        {
                            SetResult(CaptureResult.CaptureFailed,
                                "IMFMediaBuffer::Lock 失败：0x" + hr.ToString("X8"));
                            return false;
                        }
                        try
                        {
                            rawFrame = new byte[currentLength];
                            Marshal.Copy(pData, rawFrame, 0, currentLength);
                        }
                        finally
                        {
                            buffer.Unlock();
                        }
                    }
                    finally
                    {
                        NativeMethods.ReleaseComObject(buffer);
                    }

                    if (rawFrame.Length <= 0)
                    {
                        SetResult(CaptureResult.CaptureFailed, "帧数据为空");
                        return false;
                    }
                    return true;
                }
                finally
                {
                    NativeMethods.ReleaseComObject(sample);
                }
            }

            SetResult(CaptureResult.CaptureFailed,
                "尝试 10 次仍未取得有效帧（共读到 " + samplesTaken + " 个样本）");
            return false;
        }

        // ==================== 像素格式归一化 ====================

        /// <summary>
        /// 把采集到的帧转换为紧凑 top-down BGRA（每像素 4 字节），
        /// 处理正负步长（底朝上存储）与 RGB24/NV12 压缩格式。
        /// </summary>
        private static byte[] ToTopDownBgra(byte[] raw, int width, int height, int stride, Guid subtype)
        {
            if (subtype == NativeMethods.MFVideoFormat_RGB32)
            {
                if (stride == width * 4 && raw.Length >= width * height * 4)
                {
                    return raw; // 已是紧凑 top-down BGRA
                }
                return CopyRows(raw, width, height, stride, 4);
            }

            if (subtype == NativeMethods.MFVideoFormat_RGB24)
            {
                return Rgb24ToBgra(raw, width, height, stride);
            }

            if (subtype == NativeMethods.MFVideoFormat_NV12)
            {
                return Nv12ToBgra(raw, width, height, Math.Abs(stride));
            }

            return null;
        }

        private static byte[] CopyRows(byte[] raw, int width, int height, int stride, int bytesPerPixel)
        {
            int rowBytes = width * bytesPerPixel;
            if (rowBytes > Math.Abs(stride)) return null;
            byte[] output = new byte[rowBytes * height];
            bool flip = stride < 0;
            int sourceStride = Math.Abs(stride);

            for (int y = 0; y < height; y++)
            {
                int sourceRow = (flip ? height - 1 - y : y) * sourceStride;
                if (sourceRow + rowBytes > raw.Length) return null;
                Buffer.BlockCopy(raw, sourceRow, output, y * rowBytes, rowBytes);
            }
            return output;
        }

        private static byte[] Rgb24ToBgra(byte[] raw, int width, int height, int stride)
        {
            int sourceStride = Math.Abs(stride);
            bool flip = stride < 0;
            byte[] output = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                int sourceRow = (flip ? height - 1 - y : y) * sourceStride;
                int outputRow = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int s = sourceRow + x * 3;
                    int d = outputRow + x * 4;
                    if (s + 2 >= raw.Length) return null;
                    output[d] = raw[s];         // B
                    output[d + 1] = raw[s + 1]; // G
                    output[d + 2] = raw[s + 2]; // R
                    output[d + 3] = 255;        // A
                }
            }
            return output;
        }

        // BT.601 有限范围 YUV → RGB
        private static byte[] Nv12ToBgra(byte[] raw, int width, int height, int stride)
        {
            byte[] output = new byte[width * height * 4];
            long yPlaneSize = (long)stride * height;
            if (raw.Length < yPlaneSize + (long)stride * (height / 2)) return null;

            for (int y = 0; y < height; y++)
            {
                int yRow = y * stride;
                int uvRow = (int)yPlaneSize + (y / 2) * stride;
                int outputRow = y * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int luma = raw[yRow + x] - 16;
                    int uvIndex = uvRow + (x & ~1);
                    int u = raw[uvIndex] - 128;
                    int v = raw[uvIndex + 1] - 128;

                    int r = (298 * luma + 409 * v + 128) >> 8;
                    int g = (298 * luma - 100 * u - 208 * v + 128) >> 8;
                    int b = (298 * luma + 516 * u + 128) >> 8;

                    if (r < 0) r = 0; else if (r > 255) r = 255;
                    if (g < 0) g = 0; else if (g > 255) g = 255;
                    if (b < 0) b = 0; else if (b > 255) b = 255;

                    int d = outputRow + x * 4;
                    output[d] = (byte)b;
                    output[d + 1] = (byte)g;
                    output[d + 2] = (byte)r;
                    output[d + 3] = 255;
                }
            }
            return output;
        }

        // ==================== WIC JPEG 编码 ====================

        private static bool SaveJpeg(string path, byte[] bgra, int width, int height, out string error)
        {
            error = null;
            IWICImagingFactory factory = null;
            IWICStream stream = null;
            IWICBitmapEncoder encoder = null;
            IWICBitmapFrameEncode frame = null;
            IWICBitmapSource bitmap = null;
            IntPtr encoderOptions = IntPtr.Zero;

            try
            {
                factory = CreateWicFactory();
                if (factory == null)
                {
                    error = "无法创建 WIC Imaging Factory";
                    return false;
                }

                // 从紧凑 BGRA 内存创建 WIC 位图（编码时 WIC 自动转 24bppBGR）
                Guid bgraFormat = NativeMethods.GUID_WICPixelFormat32bppBGRA;
                int hr = factory.CreateBitmapFromMemory(
                    (uint)width, (uint)height, ref bgraFormat,
                    (uint)(width * 4), (uint)bgra.Length, bgra, out bitmap);
                if (hr < 0 || bitmap == null)
                {
                    error = "CreateBitmapFromMemory 失败：0x" + hr.ToString("X8");
                    return false;
                }

                hr = factory.CreateStream(out stream);
                if (hr < 0 || stream == null)
                {
                    error = "CreateStream 失败：0x" + hr.ToString("X8");
                    return false;
                }

                hr = stream.InitializeFromFilename(path, NativeMethods.GENERIC_WRITE);
                if (hr < 0)
                {
                    error = "打开输出文件失败（" + path + "）：0x" + hr.ToString("X8");
                    return false;
                }

                Guid jpegFormat = NativeMethods.GUID_ContainerFormatJpeg;
                hr = factory.CreateEncoder(ref jpegFormat, IntPtr.Zero, out encoder);
                if (hr < 0 || encoder == null)
                {
                    error = "CreateEncoder(JPEG) 失败：0x" + hr.ToString("X8");
                    return false;
                }

                hr = encoder.Initialize(
                    (ComTypes.IStream)stream, NativeMethods.WICBitmapEncoderNoCache);
                if (hr < 0)
                {
                    error = "Encoder.Initialize 失败：0x" + hr.ToString("X8");
                    return false;
                }

                hr = encoder.CreateNewFrame(out frame, out encoderOptions);
                if (hr < 0 || frame == null)
                {
                    error = "CreateNewFrame 失败：0x" + hr.ToString("X8");
                    return false;
                }
                // JPEG 编码器属性使用默认质量，不查询 IPropertyBag2
                if (encoderOptions != IntPtr.Zero) Marshal.Release(encoderOptions);

                hr = frame.Initialize(IntPtr.Zero);
                if (hr >= 0) hr = frame.SetSize((uint)width, (uint)height);
                Guid pixelFormat = NativeMethods.GUID_WICPixelFormat24bppBGR;
                if (hr >= 0) hr = frame.SetPixelFormat(ref pixelFormat);

                WICRect rect = new WICRect();
                rect.X = 0;
                rect.Y = 0;
                rect.Width = width;
                rect.Height = height;
                if (hr >= 0) hr = frame.WriteSource(bitmap, ref rect);
                if (hr >= 0) hr = frame.Commit();
                if (hr >= 0) hr = encoder.Commit();
                if (hr < 0)
                {
                    error = "JPEG 编码/写盘失败：0x" + hr.ToString("X8");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "JPEG 编码异常：" + ex.Message;
                return false;
            }
            finally
            {
                NativeMethods.ReleaseComObject(bitmap);
                NativeMethods.ReleaseComObject(frame);
                NativeMethods.ReleaseComObject(encoder);
                NativeMethods.ReleaseComObject(stream);
                NativeMethods.ReleaseComObject(factory);
            }
        }

        /// <summary>
        /// 创建 WIC 工厂：优先本机注册的新 IID，失败则回退旧 IID（旧版 Windows）。
        /// 注意 bitmap 的创建在管道主流程完成，这里只暴露工厂。
        /// </summary>
        private static IWICImagingFactory CreateWicFactory()
        {
            IntPtr pointer;
            Guid clsid = NativeMethods.CLSID_WICImagingFactory;

            Guid currentIid = NativeMethods.IID_WICImagingFactory_Current;
            int hr = NativeMethods.CoCreateInstance(
                ref clsid, IntPtr.Zero, NativeMethods.CLSCTX_INPROC_SERVER, ref currentIid, out pointer);
            if (hr >= 0)
            {
                IWICImagingFactory factory = (IWICImagingFactory)Marshal.GetObjectForIUnknown(pointer);
                Marshal.Release(pointer);
                return factory;
            }
            Log.Write("WIC 工厂（新 IID）创建失败：0x" + hr.ToString("X8") + "，尝试旧 IID");

            // 旧系统回退：通过旧 IID 拿到对象后，RCW 已按旧 IID 缓存接口，
            // 以 IWICImagingFactoryLegacy 类型调用（vtable 相同）。
            Guid legacyIid = new Guid("EC5EC8A9-C395-4314-9C77-358607A36104");
            hr = NativeMethods.CoCreateInstance(
                ref clsid, IntPtr.Zero, NativeMethods.CLSCTX_INPROC_SERVER, ref legacyIid, out pointer);
            if (hr >= 0)
            {
                IWICImagingFactoryLegacy legacy = (IWICImagingFactoryLegacy)Marshal.GetObjectForIUnknown(pointer);
                Marshal.Release(pointer);
                return legacy;
            }
            Log.Write("WIC 工厂（旧 IID）创建同样失败：0x" + hr.ToString("X8"));
            return null;
        }

        // ==================== 设备属性辅助 ====================

        private static string ReadStringAttribute(IMFActivate activate, Guid key)
        {
            IntPtr valuePointer;
            int length;
            int hr = activate.GetAllocatedString(ref key, out valuePointer, out length);
            if (hr < 0 || valuePointer == IntPtr.Zero) return "(未知设备)";
            try
            {
                string value = Marshal.PtrToStringUni(valuePointer);
                return value == null ? "(未知设备)" : value;
            }
            finally
            {
                Marshal.FreeCoTaskMem(valuePointer);
            }
        }
    }
}
