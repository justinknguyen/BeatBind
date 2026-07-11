# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BeatBind is a Windows desktop app (WinForms, .NET 8.0) that controls Spotify playback via global hotkeys, using Spotify's Web API (requires Premium). It's a complete C# rewrite of an older Python tool (see `README_LEGACY.md`); the maintainer has scaled back active development, so expect community contributions to drive most future changes.

**This is a Windows-only codebase** (`net8.0-windows`, WinForms, Win32 global hotkey hooks). It cannot be built or run on macOS/Linux — CI (`.github/workflows/ci.yml`) runs on `windows-latest`. If you're working from a non-Windows machine, you can still read/edit/reason about the code, but you cannot `dotnet build`/`dotnet run` it locally.

## Commands

All commands run from the repo root unless noted.

```bash
# Restore + build (whole solution)
dotnet restore src/BeatBind.sln
dotnet build src/BeatBind.sln --configuration Release

# Run the app
cd src/BeatBind && dotnet run

# Run all tests
dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj

# Run a single test (by fully-qualified name or filter)
dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj --filter "FullyQualifiedName~HotkeyServiceTests"
dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj --filter "DisplayName~PlayPauseAsync_WhenPlaying_ShouldPause"

# Publish a self-contained single-file build
dotnet publish src/BeatBind/BeatBind.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

CI (`.github/workflows/ci.yml`) runs on pushes/PRs to `dev`: `dotnet restore src/BeatBind.sln` → `dotnet build --configuration Release` → `dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj --configuration Release --no-build`. Match that sequence locally before opening a PR.

Branch model: `main` is the stable/release branch, `dev` is the active development branch (PRs target `dev`), `beta` auto-publishes a beta GitHub release on every push via `build-and-release.yml`.

## Architecture

Clean Architecture, five projects under `src/`, dependencies flow inward only: **Presentation → Application → Infrastructure → Core** (Core has no dependencies on the other layers; Infrastructure and Presentation both depend on Core/Application, never on each other directly).

- **`BeatBind.Core`** — domain entities (`Track`, `Hotkey`, `PlaybackState`, `Device`, `ApplicationConfiguration`) and interfaces (`ISpotifyService`, `IConfigurationService`, `IHotkeyService`, `IAuthenticationService`, `IGithubReleaseService`, `IStartupService`). No external dependencies — this is the contract layer everything else implements against.
- **`BeatBind.Application`** — `*ApplicationService` classes (`AuthenticationApplicationService`, `ConfigurationApplicationService`, `MusicControlApplicationService`, `HotkeyApplicationService`) orchestrate business logic by calling Core interfaces (not concrete Infrastructure types). MediatR is used only for cross-cutting pipeline behaviors (`Behaviors/LoggingBehavior.cs`, `Behaviors/ValidationBehavior.cs` with FluentValidation) — not a full CQRS command/handler setup.
- **`BeatBind.Infrastructure`** — concrete implementations of the Core interfaces: `SpotifyService` (Web API + OAuth), `AuthenticationService`, `ConfigurationService` (reads/writes `%APPDATA%\BeatBind\config.json`), `HotkeyService` (Win32 global hotkey hook), `GithubReleaseService` (update checks), `StartupService`.
- **`BeatBind.Presentation`** — WinForms UI using MaterialSkin.2. `MainForm` is a tab-based shell; `Panels/` (`AuthenticationPanel`, `HotkeysPanel`, `SettingsPanel`) extend `BasePanelControl`; `Components/` holds dialogs like `HotkeyEditorDialog`; `Helpers/` (`ControlFactory`, `CardFactory`, `MessageBoxHelper`, `ThemeHelper`) build/style controls; `Themes/Theme.cs` centralizes colors/styling.
- **`BeatBind.Tests`** — xUnit + Moq + FluentAssertions, mirrors the layer structure (`Core/`, `Application/`, `Infrastructure/`). Tests mock Core interfaces to exercise Application/Infrastructure logic in isolation.

**DI/startup wiring**: `src/BeatBind/Program.cs` builds a generic `IHost`, registers all services (Core interfaces → Infrastructure implementations) and MediatR/FluentValidation/Serilog, then launches `MainForm` via a small `Startup : IHostedService`.

**Typical change patterns**:
- New Spotify API capability → add a method to `ISpotifyService` (Core), implement it in `SpotifyService` (Infrastructure), call it from the relevant `*ApplicationService` (Application), wire up UI in the corresponding Panel (Presentation).
- New config option → extend `ApplicationConfiguration` (Core entity), handle read/write in `ConfigurationService` (Infrastructure), expose in `SettingsPanel`.
- New hotkey action → extend `Hotkey`/hotkey handling in Core/Infrastructure `HotkeyService`, surface it in `HotkeysPanel` and `HotkeyEditorDialog`.

Config is stored at `%APPDATA%\BeatBind\config.json`; logs at `%APPDATA%\BeatBind\` (kept for 48 hours).

## Style Conventions

Enforced via `.editorconfig` (analyzers run as warnings on build — don't introduce new warnings):
- File-scoped rules aren't mandated repo-wide (existing code uses block-scoped namespaces) — match the surrounding file's style rather than converting wholesale.
- `var` only when the type is apparent from the right-hand side (`csharp_style_var_when_type_is_apparent`); otherwise use explicit types.
- Predefined type keywords (`int`, `string`) over BCL names (`Int32`, `String`).
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`) — respect existing nullability annotations, especially on event handlers (`object? sender`) and events (`EventHandler?`).
- `.github/agents/WinFormsExpert.agent.md` has detailed WinForms Designer rules (e.g. what's forbidden inside `InitializeComponent`/`*.Designer.cs` — no lambdas, ternaries, control flow, or `nameof()`). Read it before touching any `.Designer.cs` file or generated UI code.
