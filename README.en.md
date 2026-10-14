# PeekShot

[中文](README.md) | English

Someone touched your computer? Take their photo first, then lock the screen.

While you're away, PeekShot monitors your keyboard and mouse in the background: **the first sign of input → the webcam automatically takes a photo → the screen locks immediately**.
When you come back, press your custom hotkey to exit cleanly — no photo, no lock.

Implemented in C# with native Windows Media Foundation / WIC: **a single exe with zero external dependencies** (no ffmpeg, no .NET SDK installation).

## Features

- 📸 **Snapshot on trigger**: whoever touches your keyboard ends up in a timestamped photo
- 🔒 **Auto lock**: the screen locks right after the shot — no chance to keep poking around
- ⌨️ **Hotkey exit**: come back, press the hotkey (e.g. **Ctrl+Alt+Q**), and the app exits silently
- 📦 **Single file**: one portable exe — no installer, no dependencies
- 🏠 **Local only**: photos stay in `Pictures\PeekShot\` on your machine; nothing is uploaded

## Build

### Local build

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Compiles with the in-box `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5). Output: `PeekShot.exe`.

### CI build

A GitHub Actions workflow (`.github/workflows/build.yml`) is included:

- Every push / PR builds automatically; download the exe from the run's Artifacts
- Pushing a `v*` tag (e.g. `git tag v1.0.0 && git push --tags`) creates a Release with `PeekShot.exe` attached

## Usage

1. Double-click `PeekShot.exe` and click "Yes" on the consent dialog.
2. The app monitors keyboard and mouse in the background (you can close the window; the process keeps running).
3. On the first input it detects: the camera takes a photo (about 1-2 seconds) → the screen locks.
4. When you return: press the exit hotkey (default **Ctrl+Alt+Q**) and the app exits — nothing happens.

Photos are saved to `Pictures\PeekShot\`, local only, never uploaded.

## Configuration (PeekShot.ini, next to the exe; restart to apply)

```ini
[PeekShot]
CameraName=Integrated Camera   ; camera name; empty = first device found
Hotkey=Ctrl+Alt+Q              ; exit hotkey, must include a modifier
CaptureTimeoutSeconds=5        ; capture watchdog (seconds)
SkipFrames=1                   ; drop first N frames to avoid dark shots
```

- Hotkey syntax: `Ctrl+Alt+F9`, `Ctrl+Shift+Space`, `Win+F12`, etc.; supports A-Z, 0-9, F1-F24, Space/Enter/Esc/Tab/Delete/Insert/Home/End/PageUp/PageDown/Pause and raw virtual-key codes via `0xNN`. Leave empty to disable the hotkey.
- At least one modifier is required; if the hotkey is taken by another app you'll get a warning dialog — the hotkey still works via the keyboard hook (but the keystroke also reaches the foreground app).

## Failure behavior

| Scenario | Behavior |
|---|---|
| Camera disabled / OS privacy toggle off / capture failed / timeout | Skip the photo, still lock the screen |
| Configured camera name not found | Fall back to the first device |
| Hook blocked by security software | Warning dialog; if the hook fails entirely, lock directly |
| Log | `Pictures\PeekShot\PeekShot_error.log` (text only, includes the camera list found on each start) |

## Notes

- **Windows 11 privacy toggle**: Settings → Privacy & security → Camera → "Let desktop apps access your camera" must be on, otherwise no device is found (no photo, but the screen still locks; the log shows 0 devices).
- **Antivirus / SmartScreen**: an unsigned exe with a global keyboard hook + camera access is a classic behavioral-detection signature; whitelist it if blocked.
- Input is not monitored during the capture window (about 1-2 seconds); adjust `SkipFrames` if first frames come out dark.
- Before leaving, make sure you clicked "Yes" and the process is running (PeekShot.exe visible in Task Manager).

## Testing

Capture pipeline self-check — no locking, no hooks installed:

```powershell
.\PeekShot.exe --test-capture
```

If the dialog shows `Saved` and a `PeekShot_test_*.jpg` appears in `Pictures\PeekShot\`, the pipeline works.

## Privacy & compliance

PeekShot is designed to protect **your own** computer. Only enable it where you have the right to use the camera and in a way that respects everyone's privacy; photos stay on the local machine — the app never goes online or uploads anything.

## Source layout

```
src\
  Program.cs        entry point, orchestration, logging
  ConsentDialog.cs  consent dialog (custom WinForms UI)
  Config.cs         INI parsing, hotkey parsing
  InputMonitor.cs   low-level keyboard/mouse hooks, message pump, hotkey, outcome race
  CaptureEngine.cs  Media Foundation frame capture + WIC JPEG encoding
  NativeMethods.cs  P/Invoke & COM interop (vtable order verified against local SDK headers)
```
