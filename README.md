# BeatBind C# - Spotify Global Hotkeys

A C# Windows Forms application for controlling Spotify using global hotkeys. This is a complete refactor of the original Python BeatBind application.

## Features

- **Global Hotkeys**: Control Spotify from anywhere on your system
- **Enhanced Hotkey Configuration**: 
  - Visual key combination detection
  - Add/remove hotkeys dynamically
  - Scrollable hotkey list
  - Dropdown selection for actions
- **System Tray Integration**: Minimize to tray for background operation
- **OAuth Authentication**: Secure authentication with Spotify Web API
- **Volume Control**: Adjust Spotify volume independently
- **Track Management**: Save and remove tracks from your library
- **Seeking**: Jump forward/backward in tracks

## Prerequisites

- **.NET 8.0 SDK** or later - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows** operating system (Windows 10/11)
- **Spotify Premium** account (required for playback control)
- **Spotify Developer App** (for API credentials)

## Quick Start

### 1. Install .NET SDK
Download and install .NET 8.0 SDK from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0)

### 2. Build the Application
```bash
# Option 1: Use the provided batch file (Windows)
build.bat

# Option 2: Manual build
dotnet restore
dotnet build --configuration Release
```

### 3. Spotify Developer Setup

1. Go to [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
2. Create a new app
3. Note your **Client ID** and **Client Secret**
4. Add `http://localhost:8080/callback` to your app's redirect URIs

### 4. Run the Application

```bash
# Run in development mode
dotnet run

# Or run the built executable
./bin/Release/net8.0-windows/BeatBind.exe
```

## Configuration

1. **First Launch**: Enter your Spotify Client ID and Client Secret
2. **Authentication**: Click "Authenticate with Spotify" to authorize the application
3. **Hotkey Configuration**: 
   - **Add Hotkeys**: Click "Add Hotkey" button to add new hotkey combinations
   - **Configure Keys**: Click in any hotkey field and press your desired key combination
   - **Auto-Detection**: The app automatically detects Ctrl, Alt, Shift, and Win modifiers
   - **Remove Hotkeys**: Click the red "×" button to remove unwanted hotkeys
   - **Scrollable List**: The hotkey list is scrollable for managing many combinations
4. **Save Settings**: Click "Save Configuration" to persist your settings

## Default Hotkeys

| Action | Default Hotkey |
|--------|----------------|
| Play/Pause | `Ctrl+Alt+Space` |
| Next Track | `Ctrl+Alt+Right` |
| Previous Track | `Ctrl+Alt+Left` |
| Volume Up | `Ctrl+Alt+Up` |
| Volume Down | `Ctrl+Alt+Down` |
| Mute/Unmute | `Ctrl+Alt+M` |
| Seek Forward | `Ctrl+Alt+F` |
| Seek Backward | `Ctrl+Alt+B` |
| Save Track | `Ctrl+Alt+S` |
| Remove Track | `Ctrl+Alt+R` |

## Building for Distribution

```bash
# Build release version
dotnet build --configuration Release

# Publish as single-file executable
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output ./publish
```

## Project Structure

```
BeatBind2/
├── Program.cs                 # Application entry point
├── MainForm.cs               # Main UI form
├── SpotifyBackend.cs         # Spotify Web API integration
├── SpotifyOAuthHandler.cs    # OAuth authentication flow
├── ConfigurationManager.cs   # Settings management
├── GlobalHotkeyManager.cs    # Windows global hotkey registration
├── BeatBind.csproj          # Project file
├── BeatBind.sln             # Solution file
└── Resources/               # Application resources
```

## Dependencies

- **Microsoft.Extensions.Logging** - Logging framework
- **Microsoft.Extensions.Configuration** - Configuration management
- **Newtonsoft.Json** - JSON serialization
- **System.Net.Http** - HTTP client for API calls

## Configuration File

Settings are stored in: `%APPDATA%\BeatBind\config.json`

```json
{
  "ClientId": "your-spotify-client-id",
  "ClientSecret": "your-spotify-client-secret",
  "RedirectUri": "http://localhost:8080/callback",
  "Hotkeys": {
    "PlayPause": "Ctrl+Alt+Space",
    "NextTrack": "Ctrl+Alt+Right",
    "PreviousTrack": "Ctrl+Alt+Left",
    "VolumeUp": "Ctrl+Alt+Up",
    "VolumeDown": "Ctrl+Alt+Down",
    "Mute": "Ctrl+Alt+M",
    "SeekForward": "Ctrl+Alt+F",
    "SeekBackward": "Ctrl+Alt+B",
    "SaveTrack": "Ctrl+Alt+S",
    "RemoveTrack": "Ctrl+Alt+R"
  },
  "StartWithWindows": false,
  "MinimizeToTray": true,
  "VolumeStep": 5,
  "SeekStep": 10000
}
```

## Troubleshooting

### Common Issues

1. **Hotkeys not working**: Make sure no other application is using the same key combinations
2. **Authentication fails**: Verify your Client ID, Client Secret, and redirect URI
3. **API errors**: Ensure you have Spotify Premium and an active internet connection

### Logging

The application logs to the console when run in development mode. Check the logs for detailed error information.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Original Python BeatBind application
- Spotify Web API
- .NET Community
