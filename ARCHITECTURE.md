# BeatBind Architecture

BeatBind uses Clean Architecture with layered separation. Dependencies flow inward: Presentation → Application → Infrastructure → Core.

## Project Structure

```
src/
├── BeatBind/                   # Entry point, DI configuration
├── BeatBind.Core/              # Domain entities and interfaces
├── BeatBind.Application/       # Business logic and commands
├── BeatBind.Infrastructure/    # Spotify API, file I/O
├── BeatBind.Presentation/      # Windows Forms UI
└── BeatBind.Tests/             # Unit and integration tests
```

## Core Layers

### Core ([src/BeatBind.Core/](src/BeatBind.Core/))

Domain models and contracts. No external dependencies.

**Entities**: `Track`, `Hotkey`, `PlaybackState`, `Device`, `ApplicationConfiguration`  
**Interfaces**: `ISpotifyService`, `IConfigurationService`, `IHotkeyService`, `IAuthenticationService`

### Application ([src/BeatBind.Application/](src/BeatBind.Application/))

Orchestrates business logic using MediatR and FluentValidation.

**Commands**: Authentication, configuration management, credential updates  
**Services**: `MusicControlService`, `HotkeyManagementService`  
**Behaviors**: Validation and logging pipelines

### Infrastructure ([src/BeatBind.Infrastructure/](src/BeatBind.Infrastructure/))

Implements Core interfaces for external systems.

**Services**: `SpotifyService`, `SpotifyAuthenticationService`, `JsonConfigurationService`, `WindowsHotkeyService`

### Presentation ([src/BeatBind.Presentation/](src/BeatBind.Presentation/))

Windows Forms UI components.

**Forms**: `MainForm`, `HotkeyConfigurationDialog`, `HotkeyEntry`

## How It Works

1. **Startup**: [Program.cs](src/BeatBind/Program.cs) configures DI container and launches `MainForm`
2. **User Action**: UI sends commands via MediatR
3. **Processing**: Command handlers coordinate services to execute business logic
4. **External Calls**: Infrastructure services interact with Spotify API or file system
5. **Response**: Results flow back through the layers to update the UI

## Making Changes

**Adding a feature**: Create command in Application layer, implement handler, update UI  
**New Spotify endpoint**: Add method to `ISpotifyService`, implement in `SpotifyService`  
**UI change**: Modify forms in Presentation layer  
**Configuration option**: Update `ApplicationConfiguration` entity, modify `JsonConfigurationService`

## Testing

Tests are organized by layer in [BeatBind.Tests/](src/BeatBind.Tests/). Mock interfaces from Core to test Application and Presentation logic in isolation.
