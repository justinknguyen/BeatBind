# BeatBind

[![build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/justinknguyen/BeatBind)
[![version](https://img.shields.io/badge/version-2.0.0-blue)](https://github.com/justinknguyen/BeatBind/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/justinknguyen/BeatBind/issues)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Control Spotify from anywhere on Windows using global hotkeys. No more alt-tabbing while gaming or working—adjust volume, skip tracks, and manage playback without switching windows.

<p align="center">
    <img src="./images/view.png" width="50%" height="50%">
</p>

## Features

- 🎹 **Global Hotkeys** - Control Spotify from any application
- 🔊 **Volume Control** - Adjust Spotify volume independently from system volume
- ⏯️ **Playback Control** - Play, pause, skip, seek, and shuffle
- 💾 **Track Management** - Save and remove tracks from your library
- 🌙 **System Tray** - Runs quietly in the background
- ⚙️ **Easy Setup** - Simple configuration wizard

## Requirements

- Windows 10/11 (64-bit)
- Spotify Premium account
- Internet connection

> **Note:** This app uses Spotify's Web API, which requires Premium. It sends commands over the internet, so expect a slight delay based on your connection speed.

## Installation

### Option 1: Download Release (Recommended)

1. Download the latest version from [Releases](https://github.com/justinknguyen/BeatBind/releases)
2. Extract the ZIP file to any location (avoid `Program Files` to prevent permission issues)
3. Run `BeatBind.exe`
4. Follow the [Setup Guide](#setup-guide) below

### Option 2: Build From Source

```bash
git clone https://github.com/justinknguyen/BeatBind.git
cd BeatBind/src/BeatBind
dotnet publish -c Release
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for development details.

## Setup Guide

BeatBind needs Spotify API credentials to control your music. This takes about 5 minutes.

### Step 1: Create Spotify App

1. Go to [Spotify for Developers](https://developer.spotify.com/dashboard)
2. Log in with your Spotify account
3. Click **Create App**
4. Fill in the form:
   - **App name:** BeatBind (or any name)
   - **App description:** Personal hotkey control
   - **Redirect URI:** `http://127.0.0.1:8888/callback` (change port if needed)
   - Check the Terms of Service box
5. Click **Save**

<p align="center">
    <img src="./images/create-app.png" width="70%">
</p>

### Step 2: Get Your Credentials

1. Click **Settings** in your newly created app
2. Copy your **Client ID**
3. Click **View client secret** and copy your **Client Secret**

<p align="center">
    <img src="./images/id-and-secret.png" width="70%">
</p>

### Step 3: Configure BeatBind

1. Open BeatBind and paste your **Client ID** and **Client Secret**
2. Click **Get Devices** to see your available Spotify devices
3. Select the device you want to control from the dropdown
   - **Tip:** If you don't see your device, open Spotify on that device and play something, then click **Get Devices** again
4. Click **Save** to save your settings
5. Click **Start & Close** to minimize to system tray

### Step 4: Customize Hotkeys (Optional)

1. Right-click the BeatBind icon in your system tray
2. Click **Settings**
3. Configure your preferred hotkeys for each action
4. Click **Save**

**To disable a hotkey:** Uncheck all modifiers and press Backspace in the key field.

## Usage

Once configured, BeatBind runs in the background. Use your hotkeys from any application:

- **Play/Pause** - Toggle playback
- **Next/Previous** - Skip tracks
- **Volume Up/Down** - Adjust Spotify volume
- **Seek Forward/Backward** - Jump within tracks
- **Save/Remove Track** - Manage your library
- **Shuffle** - Toggle shuffle mode

Access settings anytime by right-clicking the system tray icon.

## Updating

1. Copy and save your `BeatBind/beatbind-config.json` file somewhere
1. Replace your `BeatBind` folder with the updated version
1. Paste inside the `BeatBind` folder your saved `beatbind-config.json` file

## Contributing

Contributions are welcome! This project follows Clean Architecture principles.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

See [ARCHITECTURE.md](ARCHITECTURE.md) for project structure and [src/README.md](src/README.md) for development setup.

## Support

- 🐛 [Report a bug](https://github.com/justinknguyen/BeatBind/issues/new)
- 💡 [Request a feature](https://github.com/justinknguyen/BeatBind/issues/new)
- 📖 [Read the docs](https://github.com/justinknguyen/BeatBind/wiki)
- ⭐ Star this repo if you find it useful!

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Spotify Web API](https://developer.spotify.com/documentation/web-api)
- UI powered by [MaterialSkin](https://github.com/IgnaceMaes/MaterialSkin)
- Architecture inspired by [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

<p align="center">Made with ♥ by <a href="https://github.com/justinknguyen">Justin Nguyen</a></p>
