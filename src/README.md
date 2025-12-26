# BeatBind Source Code

Clean Architecture with layered separation. See [../ARCHITECTURE.md](../ARCHITECTURE.md) for details.

## Project Structure

```
src/
├── BeatBind/                   # Entry point, DI configuration
├── BeatBind.Core/              # Domain entities and interfaces
├── BeatBind.Application/       # Business logic, commands, MediatR
├── BeatBind.Infrastructure/    # Spotify API, file I/O, Windows APIs
├── BeatBind.Presentation/      # Windows Forms UI
└── BeatBind.Tests/             # Unit and integration tests
```

## Quick Start

### Build

```bash
cd src
dotnet build
```

### Run

```bash
cd src/BeatBind
dotnet run
```

### Test

```bash
cd src
dotnet test
```

## Technologies

- **.NET 8.0** - Target framework
- **Windows Forms** - Desktop UI
- **MediatR** - CQRS/Mediator pattern
- **FluentValidation** - Input validation
- **MaterialSkin.2** - Material Design theming
- **xUnit, Moq, FluentAssertions** - Testing

## Publishing

Self-contained (includes .NET runtime):

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Framework-dependent (requires .NET 8.0):

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `src/BeatBind/bin/Release/net8.0-windows/win-x64/publish/`
