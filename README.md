# BeatBind - Spotify Global Hotkeys (Clean Architecture)

A C# Windows Forms application that provides global hotkey controls for Spotify using Clean Architecture principles.

## 🏗️ Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
src/
├── BeatBind/                     # 🚀 Main Application Entry Point
├── BeatBind.Domain/             # 🎯 Core Business Logic & Entities  
├── BeatBind.Application/        # ⚙️ Use Cases & Application Services
├── BeatBind.Infrastructure/     # 🔌 External Integrations & Services
└── BeatBind.Presentation/       # 🖥️ User Interface & Forms
```

### Layer Dependencies
- **Domain**: No dependencies (pure business logic)
- **Application**: Depends only on Domain
- **Infrastructure**: Depends on Domain 
- **Presentation**: Depends on Domain & Application
- **Main App**: Orchestrates all layers with Dependency Injection

## ✨ Features

- **Global Hotkeys**: Control Spotify from anywhere on your system
- **OAuth Integration**: Secure authentication with Spotify Web API
- **System Tray**: Minimize to tray for background operation
- **Configurable Hotkeys**: Customize key combinations for different actions
- **Clean Architecture**: Maintainable, testable, and scalable codebase

### Supported Actions
- Play/Pause
- Next/Previous Track  
- Volume Up/Down
- Mute/Unmute
- Save/Remove Track
- Toggle Shuffle/Repeat

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Spotify Premium Account (required for Web API control)

### Setup
1. **Clone the repository**
   ```bash
   git clone https://github.com/justinknguyen/BeatBind.git
   cd BeatBind
   ```

2. **Build the application**
   ```bash
   # Option 1: Use the build script
   build.bat
   
   # Option 2: Manual build
   cd src
   dotnet build BeatBind.sln --configuration Release
   ```

3. **Configure Spotify API**
   - Create a Spotify App at [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
   - Add `http://127.0.0.1:8888/callback` as a redirect URI
   - Note your Client ID and Client Secret

4. **Run the application**
   ```bash
   cd src/BeatBind
   dotnet run
   ```

5. **First-time setup**
   - Enter your Spotify Client ID and Client Secret
   - Click "Authenticate with Spotify" 
   - Configure your preferred hotkeys
   - Save configuration

## 🏗️ Architecture Benefits

### ✅ **Separation of Concerns**
Each layer has a single, well-defined responsibility

### ✅ **Dependency Inversion** 
Core business logic doesn't depend on external frameworks

### ✅ **Testability**
Easy to unit test each layer independently

### ✅ **Maintainability** 
Changes in one layer don't affect others

### ✅ **Flexibility**
Easy to swap implementations (different UI frameworks, APIs, etc.)

## 🧪 Testing

The Clean Architecture makes testing straightforward:

```bash
# Run all tests (when test projects are added)
dotnet test

# Test specific layers
dotnet test BeatBind.Domain.Tests
dotnet test BeatBind.Application.Tests
dotnet test BeatBind.Infrastructure.Tests
```

## 🛠️ Development

### Project Structure
```
src/
├── BeatBind/                     # Program.cs, DI setup, entry point
├── BeatBind.Domain/             
│   ├── Entities/                # Core entities (Track, Hotkey, etc.)
│   └── Interfaces/              # Service contracts
├── BeatBind.Application/        
│   ├── Services/                # Business logic services
│   └── UseCases/                # Application use cases
├── BeatBind.Infrastructure/     
│   ├── Spotify/                 # Spotify Web API integration
│   ├── Configuration/           # Settings management
│   └── Hotkeys/                 # Windows hotkey registration
└── BeatBind.Presentation/       
    └── Forms/                   # Windows Forms UI
```

### Adding New Features
1. **Define entities** in `Domain/Entities`
2. **Create interfaces** in `Domain/Interfaces`
3. **Implement business logic** in `Application/Services`
4. **Add external integrations** in `Infrastructure`
5. **Create UI components** in `Presentation`
6. **Wire up dependencies** in main `Program.cs`

## 📋 Default Hotkeys

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

## 📁 Configuration

Settings are stored in: `%APPDATA%\BeatBind\config.json`

```json
{
  "ClientId": "your-spotify-client-id",
  "ClientSecret": "your-spotify-client-secret",
  "RedirectUri": "http://127.0.0.1:8888/callback",
  "Hotkeys": {
    "PlayPause": "Ctrl+Alt+Space",
    "NextTrack": "Ctrl+Alt+Right",
    // ... other hotkeys
  },
  "StartWithWindows": false,
  "MinimizeToTray": true,
  "VolumeStep": 5,
  "SeekStep": 10000
}
```

## 🔧 Troubleshooting

### Common Issues

**"Authentication failed"**
- Verify Client ID and Client Secret are correct
- Ensure redirect URI is exactly `http://127.0.0.1:8888/callback`
- Check that your Spotify app has the correct scopes

**"Global hotkeys not working"**  
- Run as Administrator if necessary
- Check that hotkey combinations aren't already in use
- Verify Windows allows the application to register global hotkeys

**"No active device found"**
- Open Spotify and start playing music
- Ensure you have a Spotify Premium account
- Check that Spotify is not in private session mode

## 📝 Migration Notes

This project was migrated from a monolithic structure to Clean Architecture:

- **Original files** → Refactored into layered structure
- **New structure** → `src/` (Clean Architecture)
- **All functionality preserved** ✅
- **Dependencies properly inverted** ✅
- **Dependency injection implemented** ✅

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow Clean Architecture principles
4. Add tests for new functionality
5. Commit changes (`git commit -m 'Add amazing feature'`)
6. Push to branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with .NET 8.0 and Windows Forms
- Uses Spotify Web API for music control
- Implements Clean Architecture by Robert C. Martin
- Original Python version inspiration
