using System;
using System.Collections.Generic;
using System.IO;

namespace PeekShot
{
    /// <summary>
    /// 程序配置，来自 exe 旁的 PeekShot.ini。
    /// 任何缺失或非法的配置项都单独回退到默认值，绝不阻断启动。
    /// </summary>
    internal class Config
    {
        // 摄像头名称（空字符串 = 使用枚举到的第一个设备）
        public string CameraName = "";
        // 热键原始文本（仅用于展示）
        public string HotkeyText = "Ctrl+Alt+Q";
        // 热键修饰键（RegisterHotKey 的 MOD_* 组合）
        public uint HotkeyMods = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
        // 热键虚拟键码（0 = 未配置有效热键）
        public uint HotkeyVk = 0x51; // 'Q'
        // 拍照看门狗上限（秒）
        public int CaptureTimeoutSeconds = 5;
        // 丢弃的前 N 帧（自动曝光未稳定时画面可能偏暗）
        public int SkipFrames = 1;

        public static Config Load(string path)
        {
            Config config = new Config();
            Dictionary<string, string> map = IniParser.Parse(path);

            string value;
            if (map.TryGetValue("CameraName", out value))
            {
                config.CameraName = value == null ? "" : value.Trim();
            }

            if (map.TryGetValue("Hotkey", out value))
            {
                uint mods;
                uint vk;
                string text = value == null ? "" : value.Trim();
                if (text.Length > 0 && HotkeyHelper.TryParse(text, out mods, out vk))
                {
                    config.HotkeyText = text;
                    config.HotkeyMods = mods;
                    config.HotkeyVk = vk;
                }
                else if (text.Length > 0)
                {
                    // 非法热键：回退默认并留痕（不弹窗）
                    Log.Write("配置警告：Hotkey=\"" + text + "\" 无法解析，回退为 Ctrl+Alt+Q");
                }
                else
                {
                    // 显式留空 = 禁用退出热键
                    config.HotkeyText = "";
                    config.HotkeyVk = 0;
                }
            }

            int number;
            if (map.TryGetValue("CaptureTimeoutSeconds", out value) &&
                int.TryParse(value.Trim(), out number) &&
                number >= 1 && number <= 60)
            {
                config.CaptureTimeoutSeconds = number;
            }

            if (map.TryGetValue("SkipFrames", out value) &&
                int.TryParse(value.Trim(), out number) &&
                number >= 0 && number <= 9)
            {
                config.SkipFrames = number;
            }

            return config;
        }
    }

    /// <summary>
    /// 极简 INI 解析：逐行 key=value，忽略空行、;/# 注释、[section] 头，
    /// key 大小写不敏感，同 key 后值覆盖前值。
    /// </summary>
    internal static class IniParser
    {
        public static Dictionary<string, string> Parse(string path)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(path)) return map;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line == null) continue;
                    line = line.Trim();
                    if (line.Length == 0) continue;
                    char first = line[0];
                    if (first == ';' || first == '#' || first == '[') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    map[key] = val;
                }
            }
            catch (Exception ex)
            {
                // 配置不可读时使用全部默认值
                Log.Write("配置警告：读取 " + path + " 失败：" + ex.Message);
            }
            return map;
        }
    }

    /// <summary>
    /// 解析 "Ctrl+Alt+Q" 形式的热键描述为 MOD_* 修饰位 + 虚拟键码。
    /// 要求：至少一个修饰键（裸键会全局劫持按键，禁止）；最后一个
    /// token 必须是具体按键而非修饰键。
    /// </summary>
    internal static class HotkeyHelper
    {
        public static bool TryParse(string text, out uint mods, out uint vk)
        {
            mods = 0;
            vk = 0;
            if (text == null) return false;

            string[] tokens = text.Split('+');
            bool sawKey = false;

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim().ToLowerInvariant();
                if (token.Length == 0) return false;

                uint modifier = ModifierFromName(token);
                if (modifier != 0)
                {
                    if (sawKey) return false; // 修饰键出现在按键之后
                    mods |= modifier;
                    continue;
                }

                // 具体按键只允许出现在最后一个 token
                if (i != tokens.Length - 1) return false;
                if (!TryKeyFromName(token, out vk)) return false;
                sawKey = true;
            }

            if (!sawKey || mods == 0) return false;
            return vk >= 0x20 && vk <= 0xFE;
        }

        private static uint ModifierFromName(string token)
        {
            switch (token)
            {
                case "ctrl":
                case "control":
                    return NativeMethods.MOD_CONTROL;
                case "alt":
                    return NativeMethods.MOD_ALT;
                case "shift":
                    return NativeMethods.MOD_SHIFT;
                case "win":
                case "windows":
                    return NativeMethods.MOD_WIN;
                default:
                    return 0;
            }
        }

        private static bool TryKeyFromName(string token, out uint vk)
        {
            vk = 0;

            // 单个字母 A-Z
            if (token.Length == 1 && token[0] >= 'a' && token[0] <= 'z')
            {
                vk = (uint)(0x41 + (token[0] - 'a'));
                return true;
            }
            // 单个数字 0-9
            if (token.Length == 1 && token[0] >= '0' && token[0] <= '9')
            {
                vk = (uint)(0x30 + (token[0] - '0'));
                return true;
            }
            // F1-F24
            if (token.Length >= 2 && token[0] == 'f')
            {
                int fNumber;
                if (int.TryParse(token.Substring(1), out fNumber) && fNumber >= 1 && fNumber <= 24)
                {
                    vk = (uint)(0x70 + fNumber - 1);
                    return true;
                }
            }
            // 命名键
            uint named;
            if (NamedKeys.TryGetValue(token, out named))
            {
                vk = named;
                return true;
            }
            // 原始虚拟键码：0x20-0xFE
            if (token.StartsWith("0x"))
            {
                int raw;
                if (int.TryParse(token.Substring(2), System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture, out raw) &&
                    raw >= 0x20 && raw <= 0xFE)
                {
                    vk = (uint)raw;
                    return true;
                }
            }
            return false;
        }

        private static readonly Dictionary<string, uint> NamedKeys = CreateNamedKeys();

        private static Dictionary<string, uint> CreateNamedKeys()
        {
            Dictionary<string, uint> map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            map["space"] = 0x20;
            map["enter"] = 0x0D;
            map["return"] = 0x0D;
            map["esc"] = 0x1B;
            map["escape"] = 0x1B;
            map["tab"] = 0x09;
            map["backspace"] = 0x08;
            map["back"] = 0x08;
            map["delete"] = 0x2E;
            map["del"] = 0x2E;
            map["insert"] = 0x2D;
            map["ins"] = 0x2D;
            map["home"] = 0x24;
            map["end"] = 0x23;
            map["pageup"] = 0x21;
            map["pgup"] = 0x21;
            map["pagedown"] = 0x22;
            map["pgdn"] = 0x22;
            map["pause"] = 0x13;
            return map;
        }
    }
}
