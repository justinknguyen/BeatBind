# BeatBind Clean Architecture Documentation

## 📐 Architecture Overview

BeatBind follows the Clean Architecture pattern by Robert C. Martin, organized into distinct layers with clear dependencies flowing inward toward the core domain.

## 🏗️ Layer Structure

### 1. Core Layer (`src/BeatBind.Core/`)

**Purpose**: Core business logic and entities
**Dependencies**: None (innermost layer)

#### Entities

- `Track`: Represents a Spotify track with metadata
- `Hotkey`: Defines a key combination and associated action
- `PlaybackState`: Current playback status and position
- `Device`: Spotify device information
- `ApplicationConfiguration`: App settings and preferences
- `AuthenticationResult`: OAuth authentication data

#### Interfaces

- `ISpotifyService`: Spotify API operations contract
- `IConfigurationService`: Settings management contract
- `IHotkeyService`: Global hotkey registration contract
- `IAuthenticationService`: OAuth authentication contract

### 2. Application Layer (`src/BeatBind.Application/`)

**Purpose**: Business logic orchestration and use cases
**Dependencies**: Domain only

#### Abstractions

- `ICommand` / `IQuery`: Interfaces for CQRS pattern built on MediatR

#### Commands

- `AuthenticateUserCommand` / `AuthenticateUserCommandHandler`: Handles user authentication flow
- `SaveConfigurationCommand` / `SaveConfigurationCommandHandler`: Persists user configuration
- `UpdateClientCredentialsCommand` / `UpdateClientCredentialsCommandHandler`: Updates API credentials
- `UpdateClientCredentialsCommandValidator`: FluentValidation validator for client credentials

#### Behaviors (Pipeline)

- `ValidationBehavior`: Validates commands using FluentValidation
- `LoggingBehavior`: Logs request execution and results

#### Services

- `MusicControlService`: Orchestrates music playback operations
- `HotkeyManagementService`: Manages hotkey registration and configuration

### 3. Infrastructure Layer (`src/BeatBind.Infrastructure/`)

**Purpose**: External integrations and technical implementations
**Dependencies**: Core interfaces

#### Services

- `SpotifyService`: Implements `ISpotifyService` using Spotify Web API
- `SpotifyAuthenticationService`: Implements `IAuthenticationService` with OAuth
- `JsonConfigurationService`: Implements `IConfigurationService` with file persistence
- `WindowsHotkeyService`: Implements `IHotkeyService` using Windows APIs

### 4. Presentation Layer (`src/BeatBind.Presentation/`)

**Purpose**: User interface and interaction
**Dependencies**: Domain and Application layers

#### Forms

- `MainForm`: Primary application window
- `HotkeyEntry`: Individual hotkey configuration control
- `HotkeyConfigurationDialog`: Hotkey setup interface

### 5. Main Application (`src/BeatBind/`)

**Purpose**: Composition root and dependency injection
**Dependencies**: All layers for DI configuration

#### Entry Point

- `Program.cs`: Application startup and DI container configuration using Microsoft.Extensions.Hosting

#### Hosting

- `Startup` (in Program.cs): IHostedService that initializes the MainForm at startup

## 🔄 Dependency Flow

```
┌─────────────────┐
│   Main App      │ ← Entry point, DI setup
│   (Program.cs)  │
└─────────┬───────┘
          │
          ▼
┌─────────────────┐
│  Presentation   │ ← Windows Forms UI
│     Layer       │
└─────────┬───────┘
          │
          ▼
┌─────────────────┐
│  Application    │ ← Business logic, use cases
│     Layer       │
└─────────┬───────┘
          │
          ▼
┌─────────────────┐
│ Infrastructure  │ ← External APIs, file I/O
│     Layer       │
└─────────┬───────┘
          │
          ▼
┌─────────────────┐
│      Core       │ ← Core entities, interfaces
│     Layer       │   (No dependencies)
└─────────────────┘
```

## 🎯 Key Principles Applied

### 1. Dependency Inversion

- High-level modules don't depend on low-level modules
- Both depend on abstractions (interfaces)
- Core defines contracts, Infrastructure implements them

### 2. Single Responsibility

- Each layer has one reason to change
- Classes have focused, single purposes
- Clear separation of concerns

### 3. Open/Closed Principle

- Easy to extend functionality without modifying existing code
- New implementations can be added via interfaces
- Behavioral changes through composition, not modification

### 4. Interface Segregation

- Interfaces are specific to client needs
- No forced implementation of unused methods
- Focused contracts for different concerns

## 🔧 Dependency Injection Setup

The main application configures all dependencies in `Program.cs`:

```csharp
// Infrastructure Layer
services.AddSingleton<IConfigurationService, JsonConfigurationService>();
services.AddHttpClient<ISpotifyService, SpotifyService>();
services.AddHttpClient<IAuthenticationService, SpotifyAuthenticationService>();

// Application Layer
services.AddTransient<MusicControlService>();
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ICommand).Assembly));
services.AddValidatorsFromAssembly(typeof(ICommand).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Presentation Layer
services.AddSingleton<MainForm>();
services.AddSingleton<IHotkeyService, WindowsHotkeyService>(/* factory for MainForm dependency */);
services.AddSingleton<HotkeyManagementService>();
services.AddSingleton<IHostedService, Startup>();
```

## 🧪 Testing Strategy

### Unit Testing

- **Core**: Test entities and value objects in isolation
- **Application**: Mock core interfaces, test business logic
- **Infrastructure**: Test implementations against real external systems
- **Presentation**: Test UI logic with mocked services

### Integration Testing

- Test layer interactions
- Verify dependency injection configuration
- End-to-end scenarios

### Test Project Structure

```
src/BeatBind.Tests/
├── Core/
│   ├── Entities/
│   │   └── EntityTests.cs
│   └── ResultTests.cs
├── Application/
│   ├── Behaviors/
│   │   ├── LoggingBehaviorTests.cs
│   │   └── ValidationBehaviorTests.cs
│   ├── Commands/
│   │   ├── AuthenticateUserCommandHandlerTests.cs
│   │   ├── SaveConfigurationCommandHandlerTests.cs
│   │   └── UpdateClientCredentialsCommandHandlerTests.cs
│   └── Validators/
│       └── UpdateClientCredentialsCommandValidatorTests.cs
└── BeatBind.Tests.csproj
```

## 📦 Benefits Achieved

### Maintainability

- Clear boundaries make code easier to understand
- Changes are localized to specific layers
- Reduced coupling between components

### Testability

- Easy to mock dependencies
- Isolated testing of business logic
- Clear test boundaries

### Flexibility

- Swap implementations without changing business logic
- Support multiple UI frameworks
- Easy to add new external integrations

### Scalability

- Clear patterns for adding new features
- Consistent architecture across the application
- Easy onboarding for new developers

## 🔄 Migration Benefits

### Before (Monolithic)

- All logic mixed in UI classes
- Hard to test business logic
- Tight coupling to Windows Forms
- Difficult to change implementations

### After (Clean Architecture)

- Clear separation of concerns
- Business logic independent of UI
- Easy to test all layers
- Flexible implementation swapping
- Better code organization

## 🚀 Future Enhancements

### Implemented Enhancements

1. **CQRS Pattern**: Separated read/write operations using MediatR
2. **Mediator Pattern**: Decoupled request/response handling
3. **Configuration Validation**: Implemented using FluentValidation
4. **Logging Strategy**: Added LoggingBehavior pipeline
5. **Error Handling**: Implemented Result pattern

### Possible Additions

1. **Event Sourcing**: Track all state changes
2. **Repository Pattern**: Add data persistence layer (if database is added)
3. **Background Services**: Long-running tasks

### Additional Testing

- Performance testing
- Security testing
- User acceptance testing
- Automated integration tests

## 📚 Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Microsoft Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
