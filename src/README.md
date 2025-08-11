# BeatBind - Clean Architecture

This is the reorganized BeatBind project following Clean Architecture principles.

## Project Structure

```
src/
├── BeatBind/                     # Main application entry point
│   ├── Program.cs               # Application startup and DI container
│   └── BeatBind.csproj
├── BeatBind.Domain/             # Domain layer (Core business logic)
│   ├── Entities/                # Domain entities
│   │   ├── Track.cs
│   │   ├── PlaybackState.cs
│   │   ├── Device.cs
│   │   ├── Hotkey.cs
│   │   ├── ApplicationConfiguration.cs
│   │   └── AuthenticationResult.cs
│   ├── Interfaces/              # Domain interfaces
│   │   ├── ISpotifyService.cs
│   │   ├── IConfigurationService.cs
│   │   ├── IHotkeyService.cs
│   │   └── IAuthenticationService.cs
│   └── BeatBind.Domain.csproj
├── BeatBind.Application/        # Application layer (Use cases and services)
│   ├── Services/                # Application services
│   │   ├── MusicControlService.cs
│   │   └── HotkeyManagementService.cs
│   ├── UseCases/                # Use case implementations
│   │   ├── AuthenticateUserUseCase.cs
│   │   └── SaveConfigurationUseCase.cs
│   └── BeatBind.Application.csproj
├── BeatBind.Infrastructure/     # Infrastructure layer (External concerns)
│   ├── Spotify/                 # Spotify API integration
│   │   ├── SpotifyService.cs
│   │   └── SpotifyAuthenticationService.cs
│   ├── Configuration/           # Configuration management
│   │   └── JsonConfigurationService.cs
│   ├── Hotkeys/                 # System hotkey integration
│   │   └── WindowsHotkeyService.cs
│   └── BeatBind.Infrastructure.csproj
├── BeatBind.Presentation/       # Presentation layer (UI)
│   ├── Forms/                   # Windows Forms
│   │   ├── MainForm.cs
│   │   ├── HotkeyEntry.cs
│   │   └── HotkeyConfigurationDialog.cs
│   └── BeatBind.Presentation.csproj
└── BeatBind.sln                # Solution file
```

## Architecture Layers

### 1. Domain Layer (Core)
- **Purpose**: Contains the core business logic and entities
- **Dependencies**: None (independent)
- **Contains**: 
  - Entities (Track, Hotkey, etc.)
  - Interfaces that define contracts
  - Domain-specific logic

### 2. Application Layer
- **Purpose**: Orchestrates domain objects and implements use cases
- **Dependencies**: Domain layer only
- **Contains**:
  - Application services that coordinate domain objects
  - Use cases that implement specific application scenarios
  - Business workflows

### 3. Infrastructure Layer
- **Purpose**: Implements interfaces from domain layer
- **Dependencies**: Domain layer
- **Contains**:
  - External API integrations (Spotify)
  - Data persistence (Configuration)
  - System services (Hotkeys)

### 4. Presentation Layer
- **Purpose**: User interface and user interaction
- **Dependencies**: Domain and Application layers
- **Contains**:
  - Windows Forms
  - UI logic and controls
  - User input handling

## Key Benefits of This Architecture

1. **Separation of Concerns**: Each layer has a single responsibility
2. **Dependency Inversion**: Higher layers don't depend on lower layers
3. **Testability**: Easy to unit test each layer independently
4. **Maintainability**: Changes in one layer don't affect others
5. **Flexibility**: Easy to swap implementations (e.g., different UI frameworks)

## Building and Running

To build the new structure:

```bash
cd src
dotnet build BeatBind.sln
```

To run the application:

```bash
cd src/BeatBind
dotnet run
```

## Migration Notes

- All original functionality has been preserved
- Dependency injection is now used throughout the application
- Configuration is now strongly typed
- Better error handling and logging
- More testable architecture

## Next Steps

1. Add unit tests for each layer
2. Consider adding a database layer for configuration
3. Implement additional music service providers
4. Add more sophisticated error handling
5. Consider implementing CQRS pattern for complex operations
