# BeatBind - Clean Architecture

This is the reorganized BeatBind project following Clean Architecture principles.

## Project Structure

```
src/
├── BeatBind/                            # Main application entry point (Composition Root)
│   ├── Program.cs                      # Application startup, DI configuration, and Startup service
│   └── BeatBind.csproj
├── BeatBind.Core/                      # Core layer (Domain business logic)
│   ├── Common/
│   │   └── Result.cs                   # Result pattern for error handling
│   ├── Entities/                       # Domain entities
│   │   ├── Track.cs
│   │   ├── PlaybackState.cs
│   │   ├── Device.cs
│   │   ├── Hotkey.cs
│   │   ├── ApplicationConfiguration.cs
│   │   └── AuthenticationResult.cs
│   ├── Interfaces/                     # Domain interfaces
│   │   ├── ISpotifyService.cs
│   │   ├── IConfigurationService.cs
│   │   ├── IHotkeyService.cs
│   │   └── IAuthenticationService.cs
│   └── BeatBind.Core.csproj
├── BeatBind.Application/               # Application layer (Use cases/CQRS)
│   ├── Abstractions/
│   │   └── Messaging.cs                # ICommand and IQuery base interfaces
│   ├── Behaviors/                      # MediatR pipeline behaviors
│   │   ├── LoggingBehavior.cs
│   │   └── ValidationBehavior.cs
│   ├── Commands/                       # CQRS commands and handlers
│   │   ├── AuthenticateUser.cs
│   │   ├── SaveConfiguration.cs
│   │   └── UpdateClientCredentials.cs
│   ├── Services/                       # Application services
│   │   ├── MusicControlService.cs
│   │   └── HotkeyManagementService.cs
│   └── BeatBind.Application.csproj
├── BeatBind.Infrastructure/            # Infrastructure layer (External integrations)
│   ├── Services/                       # Service implementations (flat structure)
│   │   ├── JsonConfigurationService.cs
│   │   ├── WindowsHotkeyService.cs
│   │   ├── SpotifyService.cs
│   │   └── SpotifyAuthenticationService.cs
│   └── BeatBind.Infrastructure.csproj
├── BeatBind.Presentation/              # Presentation layer (UI)
│   ├── Forms/                          # Windows Forms
│   │   ├── MainForm.cs
│   │   ├── HotkeyEntry.cs
│   │   └── HotkeyConfigurationDialog.cs
│   ├── Themes/
│   │   └── Theme.cs                    # Dark theme colors
│   └── BeatBind.Presentation.csproj
├── BeatBind.Tests/                     # Unit tests
│   ├── Core/                           # Tests for Core layer
│   ├── Application/                    # Tests for Application layer
│   └── BeatBind.Tests.csproj
└── BeatBind.sln                        # Solution file
```

## Architecture Layers

### 1. Core Layer (BeatBind.Core)

- **Purpose**: Contains the core business logic and entities
- **Dependencies**: None (innermost layer)
- **Contains**:
  - Domain entities (Track, Hotkey, PlaybackState, Device, etc.)
  - Domain interfaces (contracts for services)
  - Result pattern for error handling without exceptions
  - No external dependencies

### 2. Application Layer (BeatBind.Application)

- **Purpose**: Orchestrates business logic using CQRS pattern
- **Dependencies**: Core layer only
- **Contains**:
  - Commands and their handlers (AuthenticateUser, SaveConfiguration, UpdateClientCredentials)
  - MediatR pipeline behaviors (Logging, Validation)
  - FluentValidation validators for commands
  - Application services (MusicControlService, HotkeyManagementService)

### 3. Infrastructure Layer (BeatBind.Infrastructure)

- **Purpose**: Implements Core interfaces with external integrations
- **Dependencies**: Core layer
- **Contains**:
  - Spotify Web API integration (SpotifyService, SpotifyAuthenticationService)
  - JSON configuration persistence (JsonConfigurationService)
  - Windows global hotkeys via P/Invoke (WindowsHotkeyService)

### 4. Presentation Layer (BeatBind.Presentation)

- **Purpose**: Windows Forms UI
- **Dependencies**: Core and Application layers
- **Contains**:
  - MainForm (system tray, settings UI)
  - HotkeyEntry (hotkey input control)
  - HotkeyConfigurationDialog (hotkey configuration)
  - MaterialSkin theming
    , well-defined responsibility

2. **Dependency Rule**: Dependencies point inward (outer layers depend on inner, never the reverse)
3. **Testability**: Core and Application layers have no external dependencies, easy to unit test
4. **Maintainability**: Changes in outer layers (UI, Infrastructure) don't affect inner layers
5. **Flexibility**: Easy to swap implementations (different data stores, UI frameworks, APIs)
6. **CQRS Pattern**: Clear separation between commands (write) and queries (read) operations
7. **Result Pattern**: Type-safe error handling without throwing exceptions

- Program.cs with DI container configuration
- Startup service for MainForm initialization
- Application.Run() entry point

## Key Benefits of This Architecture

1. **Separation of Concerns**: Each layer has a single responsibility
2. **Dependency Inversion**: Higher layers don't depend on lower layers
3. **Testability**: Easy to unit test each layer independently
4. **Maintainability**: Changes in one layer don't affect others
5. **Flexibility**: Easy to swap implementations (e.g., different UI frameworks)

### Build the solution

```bash
cd src
dotnet build
```

### Run the application

```bash
cd src/BeatBind
dotnet run
```

Or use the VS Code task: **Run BeatBind**

### Run tests

```bash
cd src
dotnet test
```

## Technologies Used

- **.NET 8.0**: Target framework
- **Windows Forms**: Desktop UI framework
- **MediatR 12.4.1**: CQRS and mediator pattern implementation
- **FluentValidation 12.1.1**: Command input validation
- **MaterialSkin.2 2.3.1**: Material Design theming
- **Microsoft.Extensions.Hosting**: Dependency injection and service lifetime management
- **xUnit 2.5.3**: Unit testing framework
- **Moq 4.20.72**: Mocking framework for tests
- **FluentAssertions 8.8.0**: Fluent assertion library

## Publishing

### Self-contained (includes .NET runtime)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Framework-dependent (requires .NET 8.0 installed)

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output will be in: `src/BeatBind/bin/Release/net8.0-windows/win-x64/publish/` 4. Add more sophisticated error handling 5. Consider implementing CQRS pattern for complex operations
