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
**Services**: Internal business logic services (future expansion)

### Application ([src/BeatBind.Application/](src/BeatBind.Application/))

Orchestrates business logic using application services.

**Application Services**: `AuthenticationApplicationService`, `ConfigurationApplicationService`, `MusicControlApplicationService`, `HotkeyApplicationService`  
**Behaviors**: Validation and logging pipelines (MediatR)

### Infrastructure ([src/BeatBind.Infrastructure/](src/BeatBind.Infrastructure/))

Implements Core interfaces for external systems.

**Services**: `SpotifyService`, `AuthenticationService`, `ConfigurationService`, `HotkeyService`, `GithubReleaseService`

### Presentation ([src/BeatBind.Presentation/](src/BeatBind.Presentation/))

Windows Forms UI components built with MaterialSkin.

**Main Form**: `MainForm` - Tab-based interface with MaterialSkinManager integration  
**Panels**: `AuthenticationPanel`, `HotkeysPanel`, `SettingsPanel` - Tab content panels extending `BasePanelControl`  
**Components**: `HotkeyEditorDialog`, `HotkeyListItem` - Reusable UI components  
**Helpers**: `ControlFactory`, `CardFactory`, `MessageBoxHelper`, `ThemeHelper` - UI creation utilities  
**Themes**: `Theme` - Centralized color and styling definitions

## How It Works

1. **Startup**: [Program.cs](src/BeatBind/Program.cs) configures DI container and launches `MainForm`
2. **User Action**: UI interacts with Application Services
3. **Processing**: Application Services coordinate Core and Infrastructure services to execute business logic
4. **External Calls**: Infrastructure services interact with Spotify API or file system via Core interfaces
5. **Response**: Results flow back through the layers to update the UI

## Making Changes

**Adding a feature**: Create or extend Application Service, implement business logic, update UI  
**New Spotify endpoint**: Add method to `ISpotifyService`, implement in `SpotifyService`  
**UI change**: Modify forms in Presentation layer  
**Configuration option**: Update `ApplicationConfiguration` entity, modify `ConfigurationService`

## Testing

Tests are organized by layer in [BeatBind.Tests/](src/BeatBind.Tests/). Mock interfaces from Core to test Application and Presentation logic in isolation.
