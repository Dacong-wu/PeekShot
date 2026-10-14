# PeekShot

[![Build](https://github.com/Dacong-wu/PeekShot/actions/workflows/build.yml/badge.svg)](https://github.com/Dacong-wu/PeekShot/actions/workflows/build.yml)

中文 | [English](README.en.md)

有人碰了你的电脑？先给他拍张照，再锁屏。

离开电脑后，PeekShot 在后台监听键盘和鼠标：**首次检测到任何输入 → 摄像头自动拍一张照片 → 立即锁屏**。
本人返回时按自定义热键即可正常退出，不拍照、不锁屏。

由 C# + Windows 原生 Media Foundation / WIC 实现，**单个 exe，零外部依赖**（不需要 ffmpeg、不需要 .NET SDK 安装）。

## 特性

- 📸 **现场快照**：谁碰了你的键盘，就拍下谁，照片按时间戳命名
- 🔒 **自动锁屏**：拍照后立即锁定电脑，不给继续操作的机会
- ⌨️ **热键退出**：本人返回按热键（如 **Ctrl+Alt+Q**）安静退出，什么都不发生
- 📦 **单文件**：一个 exe 搞定，无安装、无依赖，拷到哪都能用
- 🏠 **仅存本地**：照片保存在本机 `图片\PeekShot\`，不上传任何数据

## 构建

### 本地构建

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

使用 Windows 自带的 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` 编译（C# 5），产物为 `PeekShot.exe`。

### CI 自动构建

仓库配置了 GitHub Actions（`.github/workflows/build.yml`）：

- 每次 push / PR 自动构建，产物在 Actions 的 Artifacts 里下载
- 推送 `v*` 标签（如 `git tag v1.0.0 && git push --tags`）会自动创建 Release 并附上 `PeekShot.exe`

## 使用

1. 双击 `PeekShot.exe`，阅读弹窗说明后点"是"。
2. 程序在后台监听键盘和鼠标（可关闭窗口，进程继续运行）。
3. 首次检测到任意键鼠输入时：摄像头拍一张照片（约 1-2 秒）→ 自动锁屏。
4. 本人返回：按退出热键（默认 **Ctrl+Alt+Q**），程序直接退出，什么都不发生。

照片保存在 `图片\PeekShot\`，仅存本地，不上传。

## 配置（PeekShot.ini，与 exe 同目录，改后重启生效）

```ini
[PeekShot]
CameraName=Integrated Camera   ; 摄像头名称，留空 = 第一个设备
Hotkey=Ctrl+Alt+Q              ; 退出热键，必须含修饰键
CaptureTimeoutSeconds=5        ; 拍照看门狗（秒）
SkipFrames=1                   ; 丢弃前 N 帧防黑图
```

- 热键写法：`Ctrl+Alt+F9`、`Ctrl+Shift+Space`、`Win+F12` 等；支持 A-Z、0-9、F1-F24、Space/Enter/Esc/Tab/Delete/Insert/Home/End/PageUp/PageDown/Pause 及 `0xNN` 原始虚拟键码。留空 = 禁用热键。
- 必须至少带一个修饰键；若被其他程序占用会弹窗提示，此时热键仍通过键盘钩子生效（但按键会传给前台应用）。

## 故障行为

| 场景 | 行为 |
|---|---|
| 摄像头被禁用 / 系统隐私开关关闭 / 拍照失败 / 超时 | 跳过拍照，仍然锁屏 |
| 配置的摄像头名不存在 | 回退到第一个设备 |
| 钩子被安全软件拦截 | 弹窗提示；完全失败则直接锁屏 |
| 日志 | `图片\PeekShot\PeekShot_error.log`（仅文字，含每次枚举到的摄像头列表） |

## 注意

- **Windows 11 隐私开关**：设置 → 隐私和安全性 → 相机 → "让桌面应用访问你的相机"必须打开，否则枚举不到设备（表现为不拍照但仍锁屏，日志记 0 设备）。
- **杀软/SmartScreen**：全局键盘钩子 + 摄像头访问的未签名 exe 是行为检测的典型特征，被拦截时请加白名单。
- 拍照期间（约 1-2 秒）输入不被监控；`SkipFrames` 可调整首帧偏暗问题。
- 离开前确认弹窗点了"是"且进程在运行（任务管理器有 PeekShot.exe）。

## 测试

不锁屏、不装钩子的拍照自检：

```powershell
.\PeekShot.exe --test-capture
```

结果弹窗显示 `Saved` 并在 `图片\PeekShot\` 生成 `PeekShot_test_*.jpg` 即管线正常。

## 隐私与合规

PeekShot 的设计初衷是保护**你自己**的电脑。请仅在你有权使用摄像头、且符合相关人员隐私要求的情况下启用；照片仅保存在本机，程序不联网、不上传。

## 源码结构

```
src\
  Program.cs        入口、流程编排、日志
  ConsentDialog.cs  同意弹窗（自定义 WinForms 界面）
  Config.cs         INI 解析、热键解析
  InputMonitor.cs   低级键鼠钩子、消息泵、热键、结果竞争
  CaptureEngine.cs  Media Foundation 采集一帧 + WIC 编码 JPEG
  NativeMethods.cs  P/Invoke 与 COM interop（vtable 顺序已对照本地 SDK 头文件核对）
```
