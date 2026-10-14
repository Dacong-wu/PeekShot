using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PeekShot
{
    internal enum Outcome
    {
        None = 0,
        Triggered = 1,  // 检测到键鼠输入 → 拍照 + 锁屏
        Cancelled = 2,  // 退出热键 → 直接退出，不拍照不锁屏
        LockOnly = 3    // 监控本身失败 → 不拍照，仅锁屏
    }

    /// <summary>
    /// 全局键鼠监控：低级键盘/鼠标钩子 + 退出热键。
    /// 第一个到达的信号（输入触发 / 热键取消）胜出，由
    /// Interlocked.CompareExchange 保证恰好一个赢家。
    /// </summary>
    internal class InputMonitor
    {
        private const int HotkeyId = 1;

        private readonly Config _config;
        private readonly NativeMethods.HookProc _keyboardProc;
        private readonly NativeMethods.HookProc _mouseProc;

        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;
        private uint _threadId;

        // 结果竞争：只有从 None 的转移生效
        private int _outcome = (int)Outcome.None;

        // 修饰键按下状态（从 KBDLLHOOKSTRUCT 跟踪，不依赖 GetAsyncKeyState）
        private bool _lCtrl, _rCtrl, _lAlt, _rAlt, _lShift, _rShift, _lWin, _rWin;

        public InputMonitor(Config config)
        {
            _config = config;
            _keyboardProc = OnKeyboard;
            _mouseProc = OnMouse;
        }

        public Outcome FinalOutcome
        {
            get { return (Outcome)_outcome; }
        }

        public bool MonitoringFailed { get; private set; }

        public void Run()
        {
            NativeMethods.MSG msg;
            _threadId = NativeMethods.GetCurrentThreadId();
            // 预先创建消息队列，保证钩子回调里的 PostThreadMessage 可送达
            NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);

            // 先注册热键并处理冲突（此时尚未安装钩子，弹窗期间的输入不会触发）
            RegisterHotkeyOrWarn();

            IntPtr module = NativeMethods.GetModuleHandle(null);
            try
            {
                _keyboardHook = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
                if (_keyboardHook == IntPtr.Zero)
                {
                    Log.Write("键盘钩子安装失败，错误码 " + Marshal.GetLastWin32Error());
                }

                _mouseHook = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);
                if (_mouseHook == IntPtr.Zero)
                {
                    Log.Write("鼠标钩子安装失败，错误码 " + Marshal.GetLastWin32Error());
                }

                if (_keyboardHook == IntPtr.Zero && _mouseHook == IntPtr.Zero)
                {
                    // 有意偏离 PS 版的静默退出：同意之后最坏的结果是不设防，
                    // 因此提示后仍然走锁屏流程。
                    MonitoringFailed = true;
                    NativeMethods.MessageBoxW(
                        IntPtr.Zero,
                        "键盘和鼠标钩子均安装失败（可能被安全软件拦截）。\r\n" +
                        "程序将直接锁定电脑，不会监控输入，也不会拍照。",
                        "PeekShot",
                        NativeMethods.MB_ICONERROR | NativeMethods.MB_SETFOREGROUND | NativeMethods.MB_TOPMOST);
                    return;
                }

                if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
                {
                    NativeMethods.MessageBoxW(
                        IntPtr.Zero,
                        "部分输入钩子安装失败，监控能力不完整（可能被安全软件拦截）。\r\n" +
                        "程序将继续以现有能力监控。",
                        "PeekShot",
                        NativeMethods.MB_ICONWARNING | NativeMethods.MB_SETFOREGROUND | NativeMethods.MB_TOPMOST);
                }

                // 消息泵：WM_QUIT（返回 0）或 WM_HOTKEY 之外的错误退出
                while (true)
                {
                    int result = NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0);
                    if (result == 0) break;
                    if (result == -1)
                    {
                        MonitoringFailed = true;
                        Log.Write("消息循环发生错误，监控提前结束");
                        break;
                    }

                    if (msg.message == NativeMethods.WM_HOTKEY)
                    {
                        // RegisterHotKey 路径（钩子路径的冗余备份）
                        ClaimAndQuit(Outcome.Cancelled);
                        continue;
                    }

                    NativeMethods.TranslateMessage(ref msg);
                    NativeMethods.DispatchMessage(ref msg);
                }
            }
            finally
            {
                if (_keyboardHook != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_keyboardHook);
                    _keyboardHook = IntPtr.Zero;
                }
                if (_mouseHook != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_mouseHook);
                    _mouseHook = IntPtr.Zero;
                }
                NativeMethods.UnregisterHotKey(IntPtr.Zero, HotkeyId);
            }
        }

        private void RegisterHotkeyOrWarn()
        {
            if (_config.HotkeyVk == 0) return; // 配置为空 = 禁用热键

            if (!NativeMethods.RegisterHotKey(
                    IntPtr.Zero, HotkeyId, _config.HotkeyMods, _config.HotkeyVk))
            {
                Log.Write("全局热键注册失败：" + _config.HotkeyText);
                NativeMethods.MessageBoxW(
                    IntPtr.Zero,
                    "无法注册全局退出热键 " + _config.HotkeyText +
                    "（可能被其他程序占用）。\r\n\r\n" +
                    "程序将继续监控；键盘钩子路径的热键取消仍然有效。\r\n" +
                    "若热键无效，请用任务管理器结束 PeekShot.exe。",
                    "PeekShot",
                    NativeMethods.MB_ICONWARNING | NativeMethods.MB_SETFOREGROUND | NativeMethods.MB_TOPMOST);
            }
        }

        // ==================== 结果竞争 ====================

        private bool Claim(Outcome outcome)
        {
            return Interlocked.CompareExchange(
                ref _outcome, (int)outcome, (int)Outcome.None) == (int)Outcome.None;
        }

        private void ClaimAndQuit(Outcome outcome)
        {
            if (!Claim(outcome)) return;
            NativeMethods.PostThreadMessage(
                _threadId, NativeMethods.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
        }

        // ==================== 键盘钩子 ====================

        private IntPtr OnKeyboard(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                int message = wParam.ToInt32();
                if (message == NativeMethods.WM_KEYDOWN ||
                    message == NativeMethods.WM_SYSKEYDOWN ||
                    message == NativeMethods.WM_KEYUP ||
                    message == NativeMethods.WM_SYSKEYUP)
                {
                    // 直接读 KBDLLHOOKSTRUCT 字段，避免 PtrToStructure 分配
                    int vk = Marshal.ReadInt32(lParam, 0);
                    int flags = Marshal.ReadInt32(lParam, 8);
                    bool isKeyUp = (flags & NativeMethods.LLKHF_UP) != 0;

                    UpdateModifierState(vk, isKeyUp);

                    if (message == NativeMethods.WM_KEYDOWN ||
                        message == NativeMethods.WM_SYSKEYDOWN)
                    {
                        if (!IsModifierVk(vk))
                        {
                            if (_config.HotkeyVk != 0 &&
                                vk == (int)_config.HotkeyVk &&
                                AreHotkeyModsDown())
                            {
                                ClaimAndQuit(Outcome.Cancelled);
                            }
                            else
                            {
                                ClaimAndQuit(Outcome.Triggered);
                            }
                        }
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
        }

        // ==================== 鼠标钩子 ====================

        private IntPtr OnMouse(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                switch (wParam.ToInt32())
                {
                    case NativeMethods.WM_MOUSEMOVE:
                    case NativeMethods.WM_LBUTTONDOWN:
                    case NativeMethods.WM_RBUTTONDOWN:
                    case NativeMethods.WM_MBUTTONDOWN:
                    case NativeMethods.WM_XBUTTONDOWN:
                    case NativeMethods.WM_MOUSEWHEEL:
                    case NativeMethods.WM_MOUSEHWHEEL:
                        ClaimAndQuit(Outcome.Triggered);
                        break;
                }
            }

            return NativeMethods.CallNextHookEx(_mouseHook, code, wParam, lParam);
        }

        // ==================== 修饰键状态 ====================

        private static bool IsModifierVk(int vk)
        {
            switch (vk)
            {
                case NativeMethods.VK_SHIFT:
                case NativeMethods.VK_CONTROL:
                case NativeMethods.VK_MENU:
                case NativeMethods.VK_LWIN:
                case NativeMethods.VK_RWIN:
                case NativeMethods.VK_LSHIFT:
                case NativeMethods.VK_RSHIFT:
                case NativeMethods.VK_LCONTROL:
                case NativeMethods.VK_RCONTROL:
                case NativeMethods.VK_LMENU:
                case NativeMethods.VK_RMENU:
                    return true;
                default:
                    return false;
            }
        }

        private void UpdateModifierState(int vk, bool isKeyUp)
        {
            switch (vk)
            {
                case NativeMethods.VK_LSHIFT: _lShift = !isKeyUp; break;
                case NativeMethods.VK_RSHIFT: _rShift = !isKeyUp; break;
                case NativeMethods.VK_SHIFT: _lShift = _rShift = !isKeyUp; break;
                case NativeMethods.VK_LCONTROL: _lCtrl = !isKeyUp; break;
                case NativeMethods.VK_RCONTROL: _rCtrl = !isKeyUp; break;
                case NativeMethods.VK_CONTROL: _lCtrl = _rCtrl = !isKeyUp; break;
                case NativeMethods.VK_LMENU: _lAlt = !isKeyUp; break;
                case NativeMethods.VK_RMENU: _rAlt = !isKeyUp; break;
                case NativeMethods.VK_MENU: _lAlt = _rAlt = !isKeyUp; break;
                case NativeMethods.VK_LWIN: _lWin = !isKeyUp; break;
                case NativeMethods.VK_RWIN: _rWin = !isKeyUp; break;
            }
        }

        private bool AreHotkeyModsDown()
        {
            uint mods = _config.HotkeyMods;
            if ((mods & NativeMethods.MOD_CONTROL) != 0 && !(_lCtrl || _rCtrl)) return false;
            if ((mods & NativeMethods.MOD_ALT) != 0 && !(_lAlt || _rAlt)) return false;
            if ((mods & NativeMethods.MOD_SHIFT) != 0 && !(_lShift || _rShift)) return false;
            if ((mods & NativeMethods.MOD_WIN) != 0 && !(_lWin || _rWin)) return false;
            return true;
        }
    }
}
