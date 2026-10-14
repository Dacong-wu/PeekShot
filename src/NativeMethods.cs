using System;
using System.Runtime.InteropServices;

namespace PeekShot
{
    /// <summary>
    /// 全部 Win32 P/Invoke 与 COM interop 声明。
    /// COM 接口的 vtable 顺序已逐一核对本地 SDK 头文件
    /// （Mfobjects.idl / Mfidl.idl / mfreadwrite.idl / wincodec.idl，10.0.26100.0），
    /// 不得调整声明顺序。未调用的方法可按规则截断，但被调用方法之前的
    /// 所有槽位必须按序完整声明。
    /// </summary>
    internal static class NativeMethods
    {
        // ==================== Win32 常量 ====================

        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const int WM_QUIT = 0x0012;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WM_HOTKEY = 0x0312;

        public const uint MOD_ALT = 0x1;
        public const uint MOD_CONTROL = 0x2;
        public const uint MOD_SHIFT = 0x4;
        public const uint MOD_WIN = 0x8;

        // KBDLLHOOKSTRUCT.flags
        public const int LLKHF_EXTENDED = 0x01;
        public const int LLKHF_UP = 0x80;

        // 虚拟键码
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_LSHIFT = 0xA0;
        public const int VK_RSHIFT = 0xA1;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_LMENU = 0xA4;
        public const int VK_RMENU = 0xA5;

        public const uint MB_ICONWARNING = 0x30;
        public const uint MB_ICONERROR = 0x10;
        public const uint MB_SETFOREGROUND = 0x10000;
        public const uint MB_TOPMOST = 0x40000;

        // ==================== Media Foundation 常量 ====================

        // MF_VERSION = MF_SDK_VERSION(0x0002) << 16 | MF_API_VERSION(0x0070)
        public const uint MF_VERSION = 0x00020070;
        // MFSTARTUP_LITE == MFSTARTUP_NOSOCKET（已核对 mfapi.h）
        public const uint MFSTARTUP_LITE = 0x1;

        // mfidl.h
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE =
            new Guid("c60ac5fe-252a-478f-a0ef-bc8fa5f7cad3");
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID =
            new Guid("8ac3587a-4ae7-42d8-99e0-0a6013eef90f");
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK =
            new Guid("58f0aad8-22bf-4f8a-bb3d-d2c4978c6e2f");
        // 注意：是 mfidl.h 的 MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME（60d0e559...），
        // 不是 functiondiscoverykeys 的 PKEY_Device_FriendlyName。
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME =
            new Guid("60d0e559-52f8-4fa2-bbce-acdb34a8ec01");

        // mfapi.h
        public static readonly Guid MF_MT_MAJOR_TYPE =
            new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        public static readonly Guid MF_MT_SUBTYPE =
            new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        public static readonly Guid MF_MT_FRAME_SIZE =
            new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
        public static readonly Guid MF_MT_DEFAULT_STRIDE =
            new Guid("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");
        public static readonly Guid MFMediaType_Video =
            new Guid("73646976-0000-0010-8000-00aa00389b71");
        public static readonly Guid MFVideoFormat_RGB32 =
            new Guid("00000016-0000-0010-8000-00aa00389b71"); // D3DFMT_X8R8G8B8
        public static readonly Guid MFVideoFormat_RGB24 =
            new Guid("00000014-0000-0010-8000-00aa00389b71"); // D3DFMT_R8G8B8
        public static readonly Guid MFVideoFormat_NV12 =
            new Guid("3231564e-0000-0010-8000-00aa00389b71"); // FCC('NV12')
        public static readonly Guid MFVideoFormat_MJPG =
            new Guid("47504a4d-0000-0010-8000-00aa00389b71"); // FCC('MJPG')

        // mfreadwrite.h
        public const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
        public static readonly Guid MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING =
            new Guid("fb394f3d-ccf1-42ee-bbb3-f9b845d5681d");

        // mferror.h
        public const int MF_E_ATTRIBUTENOTFOUND = unchecked((int)0xC00D36E6);

        public static readonly Guid IID_IMFMediaSource =
            new Guid("279a808d-aec7-40c8-9c6b-a6b492c78a66");

        // ==================== WIC 常量 ====================

        // CLSID_WICImagingFactory == CLSID_WICImagingFactory1（wincodec.idl）
        public static readonly Guid CLSID_WICImagingFactory =
            new Guid("cacaf262-9370-4615-a13b-9f5539da4c0a");

        // 本机注册表中 IWICImagingFactory 注册的 IID（Windows 11 26200 实测）。
        public static readonly Guid IID_WICImagingFactory_Current =
            new Guid("ec5ec8a9-c395-4314-9c77-54d7a935ff70");

        public static readonly Guid GUID_ContainerFormatJpeg =
            new Guid("19e4a5aa-5662-4fc5-a0c0-1758028e1057");
        public static readonly Guid GUID_WICPixelFormat32bppBGRA =
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc90f");
        public static readonly Guid GUID_WICPixelFormat24bppBGR =
            new Guid("6fddc324-4e03-4bfe-b185-3d77768dc90c");

        public const uint CLSCTX_INPROC_SERVER = 0x1;
        public const int COINIT_MULTITHREADED = 0x0;
        public const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);

        // IWICStream::InitializeFromFilename 使用 CreateFile 的 GENERIC_* 访问标志
        public const uint GENERIC_WRITE = 0x40000000;
        public const int WICBitmapEncoderNoCache = 2;
        public const int WICBitmapNoCache = 0;

        // ==================== 结构与委托 ====================

        internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WICRect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        // ==================== P/Invoke ====================

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll")]
        internal static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetMessage(out MSG msg, IntPtr hwnd, uint min, uint max);

        [DllImport("user32.dll")]
        internal static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        internal static extern IntPtr DispatchMessage(ref MSG msg);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool PostThreadMessage(uint threadId, uint message, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool PeekMessage(out MSG msg, IntPtr hwnd, uint min, uint max, uint remove);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        internal static extern bool LockWorkStation();

        // 可从任意线程调用（WinForms 的 MessageBox 只保证 STA 场景）
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr MessageBoxW(IntPtr hwnd, string text, string caption, uint type);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("ole32.dll")]
        internal static extern int CoInitializeEx(IntPtr reserved, uint coInit);

        [DllImport("ole32.dll")]
        internal static extern void CoUninitialize();

        [DllImport("ole32.dll", PreserveSig = true)]
        internal static extern int CoCreateInstance(
            ref Guid clsid, IntPtr unkOuter, uint clsContext, ref Guid iid, out IntPtr obj);

        [DllImport("mfplat.dll")]
        internal static extern int MFStartup(uint version, uint dwFlags);

        [DllImport("mfplat.dll")]
        internal static extern int MFShutdown();

        [DllImport("mfplat.dll")]
        internal static extern int MFCreateAttributes(out IMFAttributes attributes, uint cInitialCapacity);

        [DllImport("mfplat.dll")]
        internal static extern int MFCreateMediaType(out IMFMediaType mediaType);

        [DllImport("mf.dll")]
        internal static extern int MFEnumDeviceSources(
            IMFAttributes attributes, out IntPtr activates, out int count);

        [DllImport("mfreadwrite.dll")]
        internal static extern int MFCreateSourceReaderFromMediaSource(
            IMFMediaSource source, IMFAttributes attributes, out IMFSourceReader reader);

        // ==================== COM 接口 ====================
        // 约定：全部 PreserveSig 显式返回 HRESULT；未调用的方法用最简参数占位，
        // 仅用于维持 vtable 槽位顺序。

        // ---- IMFAttributes（Mfobjects.idl，30 个方法，已核对）----
        [ComImport, Guid("2CD2D921-C447-44A7-A13C-4ADABFC247E3")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFAttributes
        {
            [PreserveSig] int GetItem(ref Guid guidKey, IntPtr pValue);
            [PreserveSig] int GetItemType(ref Guid guidKey, IntPtr pType);
            [PreserveSig] int CompareItem(ref Guid guidKey, IntPtr value, IntPtr result);
            [PreserveSig] int Compare(IMFAttributes theOthers, int matchType, IntPtr result);
            [PreserveSig] int GetUINT32(ref Guid guidKey, out uint unValue);
            [PreserveSig] int GetUINT64(ref Guid guidKey, out ulong unValue);
            [PreserveSig] int GetDouble(ref Guid guidKey, IntPtr pdValue);
            [PreserveSig] int GetGUID(ref Guid guidKey, out Guid guidValue);
            [PreserveSig] int GetStringLength(ref Guid guidKey, IntPtr pcchLength);
            [PreserveSig] int GetString(ref Guid guidKey, IntPtr pwszValue, int cchBufSize, IntPtr pcchLength);
            // 返回 CoTaskMemAlloc 的缓冲区，用后须 Marshal.FreeCoTaskMem
            [PreserveSig] int GetAllocatedString(ref Guid guidKey, out IntPtr pwszValue, out int pcchLength);
            [PreserveSig] int GetBlobSize(ref Guid guidKey, IntPtr pcbBlobSize);
            [PreserveSig] int GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, IntPtr pcbBlobSize);
            [PreserveSig] int GetAllocatedBlob(ref Guid guidKey, IntPtr ppBuf, IntPtr pcbBlobSize);
            [PreserveSig] int GetUnknown(ref Guid guidKey, ref Guid riid, IntPtr ppv);
            [PreserveSig] int SetItem(ref Guid guidKey, IntPtr value);
            [PreserveSig] int DeleteItem(ref Guid guidKey);
            [PreserveSig] int DeleteAllItems();
            [PreserveSig] int SetUINT32(ref Guid guidKey, uint unValue);
            [PreserveSig] int SetUINT64(ref Guid guidKey, ulong unValue);
            [PreserveSig] int SetDouble(ref Guid guidKey, double dValue);
            [PreserveSig] int SetGUID(ref Guid guidKey, ref Guid guidValue);
            [PreserveSig] int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);
            [PreserveSig] int SetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize);
            [PreserveSig] int SetUnknown(ref Guid guidKey, IntPtr pUnknown);
            [PreserveSig] int LockStore();
            [PreserveSig] int UnlockStore();
            [PreserveSig] int GetCount(IntPtr pcItems);
            [PreserveSig] int GetItemByIndex(uint unIndex, IntPtr pguidKey, IntPtr pValue);
            [PreserveSig] int CopyAllItems(IMFAttributes pDest);
        }

        // ---- IMFActivate（Mfobjects.idl：IMFAttributes + 3 个方法，已核对）----
        [ComImport, Guid("7FEE9E9A-4A89-47A6-899C-B6A53A70FB67")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFActivate
        {
            // IMFAttributes 的 30 个方法
            [PreserveSig] int GetItem(ref Guid guidKey, IntPtr pValue);
            [PreserveSig] int GetItemType(ref Guid guidKey, IntPtr pType);
            [PreserveSig] int CompareItem(ref Guid guidKey, IntPtr value, IntPtr result);
            [PreserveSig] int Compare(IMFAttributes theOthers, int matchType, IntPtr result);
            [PreserveSig] int GetUINT32(ref Guid guidKey, out uint unValue);
            [PreserveSig] int GetUINT64(ref Guid guidKey, out ulong unValue);
            [PreserveSig] int GetDouble(ref Guid guidKey, IntPtr pdValue);
            [PreserveSig] int GetGUID(ref Guid guidKey, out Guid guidValue);
            [PreserveSig] int GetStringLength(ref Guid guidKey, IntPtr pcchLength);
            [PreserveSig] int GetString(ref Guid guidKey, IntPtr pwszValue, int cchBufSize, IntPtr pcchLength);
            [PreserveSig] int GetAllocatedString(ref Guid guidKey, out IntPtr pwszValue, out int pcchLength);
            [PreserveSig] int GetBlobSize(ref Guid guidKey, IntPtr pcbBlobSize);
            [PreserveSig] int GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, IntPtr pcbBlobSize);
            [PreserveSig] int GetAllocatedBlob(ref Guid guidKey, IntPtr ppBuf, IntPtr pcbBlobSize);
            [PreserveSig] int GetUnknown(ref Guid guidKey, ref Guid riid, IntPtr ppv);
            [PreserveSig] int SetItem(ref Guid guidKey, IntPtr value);
            [PreserveSig] int DeleteItem(ref Guid guidKey);
            [PreserveSig] int DeleteAllItems();
            [PreserveSig] int SetUINT32(ref Guid guidKey, uint unValue);
            [PreserveSig] int SetUINT64(ref Guid guidKey, ulong unValue);
            [PreserveSig] int SetDouble(ref Guid guidKey, double dValue);
            [PreserveSig] int SetGUID(ref Guid guidKey, ref Guid guidValue);
            [PreserveSig] int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);
            [PreserveSig] int SetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize);
            [PreserveSig] int SetUnknown(ref Guid guidKey, IntPtr pUnknown);
            [PreserveSig] int LockStore();
            [PreserveSig] int UnlockStore();
            [PreserveSig] int GetCount(IntPtr pcItems);
            [PreserveSig] int GetItemByIndex(uint unIndex, IntPtr pguidKey, IntPtr pValue);
            [PreserveSig] int CopyAllItems(IMFAttributes pDest);
            // IMFActivate 自身
            [PreserveSig] int ActivateObject(ref Guid riid, out IntPtr ppvObject);
        }

        // ---- IMFMediaSource（仅作为对象引用传递，不调用其任何方法；
        //      声明 canonical 顺序仅作文档用途）----
        [ComImport, Guid("279A808D-AEC7-40C8-9C6B-A6B492C78A66")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFMediaSource
        {
            // IMFMediaEventGenerator
            [PreserveSig] int GetEvent(int dwFlags, IntPtr ppEvent);
            [PreserveSig] int BeginGetEvent(IntPtr pCallback, IntPtr punkState);
            [PreserveSig] int EndGetEvent(IntPtr pResult, IntPtr ppEvent);
            [PreserveSig] int QueueEvent(ref Guid mediaEventType, ref Guid extendedType, int hrResult, IntPtr value);
            // IMFMediaSource
            [PreserveSig] int GetCharacteristics(IntPtr pCharacteristics);
            [PreserveSig] int CreatePresentationDescriptor(IntPtr ppPD);
            [PreserveSig] int Start(IntPtr pPD, IntPtr pguidTimeFormat, IntPtr pvarStartPos);
            [PreserveSig] int Stop();
            [PreserveSig] int Pause();
            [PreserveSig] int Shutdown();
        }

        // ---- IMFMediaType（IMFAttributes，无新增方法；用到 CopyAllItems 故声明满 30 个）----
        [ComImport, Guid("44AE0FA8-EA31-4109-8D2E-4CAE4997C555")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFMediaType
        {
            [PreserveSig] int GetItem(ref Guid guidKey, IntPtr pValue);
            [PreserveSig] int GetItemType(ref Guid guidKey, IntPtr pType);
            [PreserveSig] int CompareItem(ref Guid guidKey, IntPtr value, IntPtr result);
            [PreserveSig] int Compare(IMFAttributes theOthers, int matchType, IntPtr result);
            [PreserveSig] int GetUINT32(ref Guid guidKey, out uint unValue);
            [PreserveSig] int GetUINT64(ref Guid guidKey, out ulong unValue);
            [PreserveSig] int GetDouble(ref Guid guidKey, IntPtr pdValue);
            [PreserveSig] int GetGUID(ref Guid guidKey, out Guid guidValue);
            [PreserveSig] int GetStringLength(ref Guid guidKey, IntPtr pcchLength);
            [PreserveSig] int GetString(ref Guid guidKey, IntPtr pwszValue, int cchBufSize, IntPtr pcchLength);
            [PreserveSig] int GetAllocatedString(ref Guid guidKey, out IntPtr pwszValue, out int pcchLength);
            [PreserveSig] int GetBlobSize(ref Guid guidKey, IntPtr pcbBlobSize);
            [PreserveSig] int GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, IntPtr pcbBlobSize);
            [PreserveSig] int GetAllocatedBlob(ref Guid guidKey, IntPtr ppBuf, IntPtr pcbBlobSize);
            [PreserveSig] int GetUnknown(ref Guid guidKey, ref Guid riid, IntPtr ppv);
            [PreserveSig] int SetItem(ref Guid guidKey, IntPtr value);
            [PreserveSig] int DeleteItem(ref Guid guidKey);
            [PreserveSig] int DeleteAllItems();
            [PreserveSig] int SetUINT32(ref Guid guidKey, uint unValue);
            [PreserveSig] int SetUINT64(ref Guid guidKey, ulong unValue);
            [PreserveSig] int SetDouble(ref Guid guidKey, double dValue);
            [PreserveSig] int SetGUID(ref Guid guidKey, ref Guid guidValue);
            [PreserveSig] int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);
            [PreserveSig] int SetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize);
            [PreserveSig] int SetUnknown(ref Guid guidKey, IntPtr pUnknown);
            [PreserveSig] int LockStore();
            [PreserveSig] int UnlockStore();
            [PreserveSig] int GetCount(IntPtr pcItems);
            [PreserveSig] int GetItemByIndex(uint unIndex, IntPtr pguidKey, IntPtr pValue);
            [PreserveSig] int CopyAllItems(IMFAttributes pDest);
        }

        // ---- IMFMediaBuffer（Mfobjects.idl 实际顺序：Lock, Unlock, GetCurrentLength,
        //      SetCurrentLength, GetMaxLength；本程序只调用 Lock/Unlock，故截断到 Unlock）----
        [ComImport, Guid("045FA593-8799-42B8-BC8D-8968C6453507")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFMediaBuffer
        {
            [PreserveSig] int Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);
            [PreserveSig] int Unlock();
        }

        // ---- IMFSample（Mfobjects.idl：IMFAttributes 30 个 + 9 个，已核对；
        //      只调用到 ConvertToContiguousBuffer）----
        [ComImport, Guid("C40A00F2-B93A-4D80-AE8C-5A1C634F58E4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFSample
        {
            // IMFAttributes 的 30 个方法
            [PreserveSig] int GetItem(ref Guid guidKey, IntPtr pValue);
            [PreserveSig] int GetItemType(ref Guid guidKey, IntPtr pType);
            [PreserveSig] int CompareItem(ref Guid guidKey, IntPtr value, IntPtr result);
            [PreserveSig] int Compare(IMFAttributes theOthers, int matchType, IntPtr result);
            [PreserveSig] int GetUINT32(ref Guid guidKey, out uint unValue);
            [PreserveSig] int GetUINT64(ref Guid guidKey, out ulong unValue);
            [PreserveSig] int GetDouble(ref Guid guidKey, IntPtr pdValue);
            [PreserveSig] int GetGUID(ref Guid guidKey, out Guid guidValue);
            [PreserveSig] int GetStringLength(ref Guid guidKey, IntPtr pcchLength);
            [PreserveSig] int GetString(ref Guid guidKey, IntPtr pwszValue, int cchBufSize, IntPtr pcchLength);
            [PreserveSig] int GetAllocatedString(ref Guid guidKey, out IntPtr pwszValue, out int pcchLength);
            [PreserveSig] int GetBlobSize(ref Guid guidKey, IntPtr pcbBlobSize);
            [PreserveSig] int GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, IntPtr pcbBlobSize);
            [PreserveSig] int GetAllocatedBlob(ref Guid guidKey, IntPtr ppBuf, IntPtr pcbBlobSize);
            [PreserveSig] int GetUnknown(ref Guid guidKey, ref Guid riid, IntPtr ppv);
            [PreserveSig] int SetItem(ref Guid guidKey, IntPtr value);
            [PreserveSig] int DeleteItem(ref Guid guidKey);
            [PreserveSig] int DeleteAllItems();
            [PreserveSig] int SetUINT32(ref Guid guidKey, uint unValue);
            [PreserveSig] int SetUINT64(ref Guid guidKey, ulong unValue);
            [PreserveSig] int SetDouble(ref Guid guidKey, double dValue);
            [PreserveSig] int SetGUID(ref Guid guidKey, ref Guid guidValue);
            [PreserveSig] int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);
            [PreserveSig] int SetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize);
            [PreserveSig] int SetUnknown(ref Guid guidKey, IntPtr pUnknown);
            [PreserveSig] int LockStore();
            [PreserveSig] int UnlockStore();
            [PreserveSig] int GetCount(IntPtr pcItems);
            [PreserveSig] int GetItemByIndex(uint unIndex, IntPtr pguidKey, IntPtr pValue);
            [PreserveSig] int CopyAllItems(IMFAttributes pDest);
            // IMFSample 自身
            [PreserveSig] int GetSampleFlags(IntPtr pdwSampleFlags);
            [PreserveSig] int SetSampleFlags(int dwSampleFlags);
            [PreserveSig] int GetSampleTime(IntPtr phnsSampleTime);
            [PreserveSig] int SetSampleTime(long hnsSampleTime);
            [PreserveSig] int GetSampleDuration(IntPtr phnsSampleDuration);
            [PreserveSig] int SetSampleDuration(long hnsSampleDuration);
            [PreserveSig] int GetBufferCount(IntPtr pdwBufferCount);
            [PreserveSig] int GetBufferByIndex(uint dwIndex, IntPtr ppBuffer);
            [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);
        }

        // ---- IMFSourceReader（mfreadwrite.idl，ReadSample 位于槽位 7，已核对）----
        [ComImport, Guid("70AE66F2-C809-4E4F-8915-BDCB406B7993")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMFSourceReader
        {
            [PreserveSig] int GetStreamSelection(uint dwStreamIndex, IntPtr pbSelected);
            [PreserveSig] int SetStreamSelection(uint dwStreamIndex, int fSelected);
            [PreserveSig] int GetNativeMediaType(uint dwStreamIndex, uint dwMediaTypeIndex, out IMFMediaType ppMediaType);
            [PreserveSig] int GetCurrentMediaType(uint dwStreamIndex, out IMFMediaType ppMediaType);
            [PreserveSig] int SetCurrentMediaType(uint dwStreamIndex, IntPtr pdwReserved, IMFMediaType pMediaType);
            [PreserveSig] int SetCurrentPosition(ref Guid guidMajorType, IMFAttributes pvarPosition);
            [PreserveSig] int ReadSample(
                uint dwStreamIndex, uint dwControlFlags,
                out uint pdwActualStreamIndex, out uint pdwStreamFlags,
                out long pllTimestamp, out IMFSample ppSample);
            [PreserveSig] int Flush(uint dwStreamIndex);
            [PreserveSig] int GetServiceForStream(uint dwStreamIndex, ref Guid guidService, ref Guid riid, IntPtr ppvObject);
            [PreserveSig] int GetPresentationAttribute(uint dwStreamIndex, ref Guid guidKey, IntPtr pValue);
        }

        // ---- IWICImagingFactory（wincodec.idl 10.0.26100.0，CreateBitmapFromMemory 在槽位 18，已核对；
        //      本机注册表登记的 IID 为 ...54d7a935ff70）----
        [ComImport, Guid("EC5EC8A9-C395-4314-9C77-54D7A935FF70")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICImagingFactory
        {
            [PreserveSig] int CreateDecoderFromFilename(
                [MarshalAs(UnmanagedType.LPWStr)] string wzFilename, IntPtr pguidVendor, uint dwDesiredAccess, int metadataOptions, IntPtr ppIDecoder);
            [PreserveSig] int CreateDecoderFromStream(IntPtr pIStream, IntPtr pguidVendor, int metadataOptions, IntPtr ppIDecoder);
            [PreserveSig] int CreateDecoderFromFileHandle(UIntPtr hFile, IntPtr pguidVendor, int metadataOptions, IntPtr ppIDecoder);
            [PreserveSig] int CreateComponentInfo(ref Guid clsidComponent, IntPtr ppIInfo);
            [PreserveSig] int CreateDecoder(ref Guid guidContainerFormat, IntPtr pguidVendor, int metadataOptions, IntPtr ppIDecoder);
            [PreserveSig] int CreateEncoder(ref Guid guidContainerFormat, IntPtr pguidVendor, out IWICBitmapEncoder ppIEncoder);
            [PreserveSig] int CreatePalette(IntPtr ppIPalette);
            [PreserveSig] int CreateFormatConverter(IntPtr ppIFormatConverter);
            [PreserveSig] int CreateBitmapScaler(IntPtr ppIScaler);
            [PreserveSig] int CreateBitmapClipper(IntPtr ppIClipper);
            [PreserveSig] int CreateBitmapFlipRotator(IntPtr ppIFlipRotator);
            [PreserveSig] int CreateStream(out IWICStream ppIWICStream);
            [PreserveSig] int CreateColorContext(IntPtr ppIColorContext);
            [PreserveSig] int CreateColorTransformer(IntPtr ppIColorTransformer);
            [PreserveSig] int CreateBitmap(uint uiWidth, uint uiHeight, ref Guid pixelFormat, uint cacheOption, IntPtr ppIBitmap);
            [PreserveSig] int CreateBitmapFromSource(IWICBitmapSource piBitmapSource, uint cacheOption, IntPtr ppIBitmap);
            [PreserveSig] int CreateBitmapFromSourceRect(IWICBitmapSource piBitmapSource, uint x, uint y, uint width, uint height, IntPtr ppIBitmap);
            [PreserveSig] int CreateBitmapFromMemory(
                uint uiWidth, uint uiHeight, ref Guid pixelFormat,
                uint cbStride, uint cbBufferSize,
                [MarshalAs(UnmanagedType.LPArray)] byte[] pbBuffer,
                out IWICBitmapSource ppIBitmap);
        }

        // 旧版 Windows 注册的 IWICImagingFactory IID（老 SDK 头文件中的值）。
        // 继承自同一 vtable 声明，仅 Guid 不同，用于兼容旧系统。
        [ComImport, Guid("EC5EC8A9-C395-4314-9C77-358607A36104")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICImagingFactoryLegacy : IWICImagingFactory
        {
        }

        // ---- IWICStream（wincodec.idl：IStream 11 个方法 + 自身 4 个；
        //      只调用 InitializeFromFilename，位于槽位 13）----
        [ComImport, Guid("135FF860-22B7-4DDF-B0F6-218F4F299A43")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICStream
        {
            // IStream（ComTypes.IStream 的扁平化顺序：Read, Write, Seek, SetSize,
            // CopyTo, Commit, Revert, LockRegion, UnlockRegion, Stat, Clone）
            [PreserveSig] int Read(IntPtr pv, int cb, IntPtr pcbRead);
            [PreserveSig] int Write(IntPtr pv, int cb, IntPtr pcbWritten);
            [PreserveSig] int Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition);
            [PreserveSig] int SetSize(long libNewSize);
            [PreserveSig] int CopyTo(IntPtr pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten);
            [PreserveSig] int Commit(int grfCommitFlags);
            [PreserveSig] int Revert();
            [PreserveSig] int LockRegion(long libOffset, long cb, int dwLockType);
            [PreserveSig] int UnlockRegion(long libOffset, long cb, int dwLockType);
            [PreserveSig] int Stat(IntPtr pstatstg, int grfStatFlag);
            [PreserveSig] int Clone(IntPtr ppstm);
            // IWICStream 自身
            [PreserveSig] int InitializeFromIStream(IntPtr pIStream);
            [PreserveSig] int InitializeFromFilename(
                [MarshalAs(UnmanagedType.LPWStr)] string wzFileName, uint dwDesiredAccess);
        }

        // ---- IWICBitmapSource（wincodec.idl 前 5 个方法，已核对）----
        [ComImport, Guid("00000120-A8F2-4877-BA0A-FD2B6645FB94")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICBitmapSource
        {
            [PreserveSig] int GetSize(IntPtr puiWidth, IntPtr puiHeight);
            [PreserveSig] int GetPixelFormat(IntPtr pPixelFormat);
            [PreserveSig] int GetResolution(IntPtr pdpiX, IntPtr pdpiY);
            [PreserveSig] int CopyPalette(IntPtr pIPalette);
            [PreserveSig] int CopyPixels(IntPtr prc, int cbStride, int cbBufferSize, IntPtr pbBuffer);
        }

        // ---- IWICBitmapEncoder（wincodec.idl 前 9 个方法，已核对）----
        [ComImport, Guid("00000103-A8F2-4877-BA0A-FD2B6645FB94")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICBitmapEncoder
        {
            [PreserveSig] int Initialize(System.Runtime.InteropServices.ComTypes.IStream pIStream, int cacheOption);
            [PreserveSig] int GetContainerFormat(out Guid pguidContainerFormat);
            [PreserveSig] int GetEncoderInfo(IntPtr ppIEncoderInfo);
            [PreserveSig] int SetColorContexts(uint cCount, IntPtr ppIColorContexts);
            [PreserveSig] int SetPalette(IntPtr pIPalette);
            [PreserveSig] int SetThumbnail(IntPtr pIThumbnail);
            [PreserveSig] int SetPreview(IntPtr pIPreview);
            [PreserveSig] int CreateNewFrame(out IWICBitmapFrameEncode ppIFrameEncode, out IntPtr ppIEncoderOptions);
            [PreserveSig] int Commit();
        }

        // ---- IWICBitmapFrameEncode（wincodec.idl 前 10 个方法，已核对）----
        [ComImport, Guid("00000105-A8F2-4877-BA0A-FD2B6645FB94")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IWICBitmapFrameEncode
        {
            [PreserveSig] int Initialize(IntPtr pIEncoderOptions);
            [PreserveSig] int SetSize(uint uiWidth, uint uiHeight);
            [PreserveSig] int SetResolution(double dpiX, double dpiY);
            [PreserveSig] int SetPixelFormat(ref Guid pPixelFormat);
            [PreserveSig] int SetColorContexts(uint cCount, IntPtr ppIColorContexts);
            [PreserveSig] int SetPalette(IntPtr pIPalette);
            [PreserveSig] int SetThumbnail(IntPtr pIThumbnail);
            [PreserveSig] int WritePixels(uint lcLines, IntPtr prc, uint cbStride, IntPtr pbPixels);
            [PreserveSig] int WriteSource(IWICBitmapSource pIBitmapSource, ref WICRect prc);
            [PreserveSig] int Commit();
        }

        // ==================== 辅助 ====================

        /// <summary>安全释放 COM RCW，吞掉异常（清理路径不允许抛出）。</summary>
        internal static void ReleaseComObject(object comObject)
        {
            if (comObject == null) return;
            try { Marshal.FinalReleaseComObject(comObject); }
            catch (ArgumentException) { }
        }
    }
}
