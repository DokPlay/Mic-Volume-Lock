<p align="center">
  <a href="README_RU.md">Русская версия</a>
</p>

<p align="center">
  <img src="docs/readme-hero.svg" alt="Mic Volume Lock animated banner" width="100%" />
</p>

# Mic Volume Lock

**Mic Volume Lock** is a free, open-source Windows utility that keeps your selected microphone input level exactly where you want it.

If Windows, Discord, Zoom, a game, a driver utility, or another audio component keeps lowering your microphone volume, Mic Volume Lock watches the selected microphone endpoint and restores the target level automatically.

The project is licensed under the **MIT License** and is built for people who just want their microphone to stop fighting them.

<p align="center">
  <img src="docs/workflow.svg" alt="Mic Volume Lock workflow animation" width="100%" />
</p>

## Why this exists

Some Windows 10/11 systems randomly reduce microphone input volume after driver updates, communication-app changes, audio enhancements, or vendor audio software updates. For many users, the microphone slider looks fine for a while, then suddenly drops again.

Mic Volume Lock solves the practical part of the problem:

- choose the microphone;
- choose the target volume;
- enable protection;
- leave the app running in the tray.

## Main features

- **Microphone volume lock** through Windows Core Audio endpoint volume.
- **Device selection** with support for the default communications microphone.
- **Tray mode** so the app can run quietly in the background.
- **Startup option** for launching with Windows.
- **Light and dark themes** with an in-app theme switcher.
- **Profiles** for quick presets such as Gaming, Work, Streaming, and Quiet room.
- **Global hotkeys** for toggling protection and changing volume quickly.
- **Process exclusions and troubleshooting hints** for Discord, Zoom, Steam/games, Windows settings, NVIDIA Broadcast / RTX Voice, and AMD Noise Suppression.
- **Support logs** that can be copied or exported when troubleshooting.
- **Multilingual interface** with English, Russian, Spanish, Portuguese Brazil, German, French, Italian, Polish, Turkish, Chinese Simplified, Japanese, Korean, Dutch, Indonesian, Vietnamese, Czech, Arabic, Hindi, and Romanian.
- **Classic Windows installer** built with Inno Setup.

## Download

Stable builds should be downloaded from GitHub Releases:

[Download Mic Volume Lock](https://github.com/DokPlay/Mic-Volume-Lock/releases)

Release files such as `MicVolumeLockSetup.exe` and portable builds are attached to Releases. They are intentionally not committed into the repository.

## Quick start

1. Download `MicVolumeLockSetup.exe` from the Releases page.
2. Run the installer.
3. Choose the installer language.
4. Launch Mic Volume Lock.
5. Open the **Microphone** tab.
6. Select your microphone.
7. Set the target volume, for example `85%` or `100%`.
8. Enable **Lock volume**.
9. Keep the app in the tray.

If the volume changes while protection is enabled, Mic Volume Lock restores it automatically.

## Recommended first setup

For the most stable result:

1. Enable **Run Mic Volume Lock at Windows startup**.
2. Enable **Follow default communications microphone** if you often reconnect headsets or USB microphones.
3. Keep notifications enabled at first, then disable them if you prefer silence.
4. Check the Exclusions tab if the volume keeps changing often.

## Exclusions: what may change microphone volume

Mic Volume Lock cannot always identify the exact process that changed the microphone level because Windows does not expose that information reliably for every audio path. The Exclusions tab focuses on practical suspects and offers a temporary exception: restore can be paused while the selected process is running.

- **Discord**: Voice & Video -> Automatic Gain Control.
- **Zoom**: Audio -> Automatically adjust microphone volume.
- **Steam / games**: voice input or microphone gain settings.
- **Windows**: communications settings and exclusive-mode behavior.
- **NVIDIA Broadcast / RTX Voice**: microphone effects and noise removal.
- **AMD Software: Adrenalin Edition**: AMD Noise Suppression and AMD Streaming Audio Device.

## What Mic Volume Lock does not do

Mic Volume Lock does **not** record microphone audio.

It does not store, analyze, upload, or transmit your voice. The app only reads and writes the Windows endpoint volume for the selected capture device.

Some vendor drivers, APOs, hardware AGC implementations, and exclusive audio paths can still affect real microphone gain outside the normal Windows endpoint volume slider. Mic Volume Lock handles the endpoint volume layer and provides exclusions/troubleshooting hints for the rest.

## Build from source

Requirements:

- Windows 10 or Windows 11.
- .NET 8 SDK.
- Inno Setup 6, only if you want to build the installer.

Build the app:

```powershell
cd "C:\Mic Volume Lock"
dotnet build -c Release
```

Create a standalone executable:

```powershell
.\build-release.ps1
```

Output:

```text
publish\MicVolumeLock.exe
```

Create the installer:

```powershell
.\build-installer.ps1
```

Output:

```text
dist\MicVolumeLockSetup.exe
```

If Inno Setup 6 is missing, install it first:

```powershell
winget install --id JRSoftware.InnoSetup -e
```

## Repository hygiene

The repository is meant to store source code, scripts, assets, documentation, and installer definitions.

The following should not be committed:

- compiled `.exe` files;
- `publish/` output;
- `dist/` installers;
- `bin/` and `obj/` folders;
- logs and local settings;
- signing keys, certificates, `.pfx`, `.pem`, `.key`, `.jks`, and other secrets.

Release binaries belong on the GitHub Releases page, not in the source tree.

## Project status

Mic Volume Lock is an early public utility. Feedback, bug reports, logs, and suggestions are welcome.

If the app helped you, the best way to support the project right now is to star the repository and share it with someone who has the same microphone-volume problem.

## License

MIT License. See [LICENSE](LICENSE).
