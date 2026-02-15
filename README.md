# BeatBind - Spotify Global Hotkeys

[![build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/justinknguyen/BeatBind)
[![version](https://img.shields.io/badge/version-2.0.2-blue)](https://github.com/justinknguyen/BeatBind/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/justinknguyen/BeatBind/issues)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

> **📢 Version 2.0.0 is a complete rewrite!** This version is built from the ground up in C# with a modern UI and improved performance. Looking for the old Python version? See the [legacy README](README_LEGACY.md). 
<br> <br>
Since I don't use Spotify anymore, new features will not be added, however, please report any bugs. Thank you!

Control Spotify from anywhere on Windows using global hotkeys. No more alt-tabbing while gaming or working—adjust volume, skip tracks, and manage playback without switching windows.

<p align="center">
    <img src="./images/view.png" width="50%" height="50%"> <br> <br>
    <a href="https://paypal.me/OrbitUT?country.x=CA&locale.x=en_US">
        <img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee">
    </a>
</p>

## Features

- 🎹 **Global Hotkeys** - Control Spotify from any application
- 🔊 **Volume Control** - Adjust Spotify volume independently from system volume
- ⏯️ **Playback Control** - Play, pause, skip, seek, shuffle, and repeat
- 💾 **Track Management** - Save and remove tracks from your library
- 🌙 **System Tray** - Runs quietly in the background
- 🔄 **Auto-Update Checker** - Get notified when new versions are available
- ⚙️ **Easy Setup** - Simple configuration wizard

## Requirements

- Windows 10/11 (64-bit)
- Spotify Premium account
- Internet connection

> **Note:** This app uses Spotify's Web API, which requires Premium. It sends commands to Spotify over the internet, so expect a slight delay based on your connection speed and Spotify's.
>
> Why use Spotify's Web API? Other solutions will send hotkey commands to your local Spotify app, which would allow Free and Premium users to have global hotkey control. However, with Spotify's frequent app updates, this approach eventually stops working. The goal of BeatBind is to be a long-running solution.

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
   - **App name:** e.g., BeatBind
   - **App description:** e.g., hotkeys
   - **Redirect URI:** `http://127.0.0.1:8888/callback` (change port if needed)
   - **Which API/SDKs are you planning to use?** Web API
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

1. Open BeatBind and go to the `Authentication` tab and paste your **Client ID** and **Client Secret**
2. Change the **Redirect Port** if needed
3. Click **Authenticate with Spotify** to setup the Spotify connection
4. Click **Save Configuration** to save your settings
5. After adding Hotkeys, click **Save Configuration** to enable them

## Usage

Once configured, BeatBind runs in the background. Use your hotkeys from any application:

- **Play/Pause** - Toggle playback
- **Next/Previous** - Skip tracks
- **Volume Up/Down** - Adjust Spotify volume
- **Seek Forward/Backward** - Jump within tracks
- **Save/Remove Track** - Manage your library
- **Shuffle** - Toggle shuffle mode
- **Repeat** - Toggle repeat mode

Access settings anytime by right-clicking the system tray icon.

### Configuration File

Your settings are saved to `%APPDATA%\BeatBind\config.json`. You can edit this file directly if needed, or use the in-app settings interface.

Logs are saved to `%APPDATA%\BeatBind\`. The application keeps logs for the past 48 hours to help with troubleshooting.

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
- ⭐ Star this repo if you find it useful!

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details.

## Acknowledgments

- Built with [Spotify Web API](https://developer.spotify.com/documentation/web-api)
- UI powered by [MaterialSkin](https://github.com/IgnaceMaes/MaterialSkin)
- Architecture inspired by [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
