# Contributing to BeatBind

Thanks for helping keep BeatBind alive! The maintainer has scaled back active development, so community contributions drive most changes. This guide gets you from clone to merged PR quickly.

## Prerequisites

- **Windows 10/11** — this is a `net8.0-windows` WinForms app with Win32 keyboard hooks. It cannot be built or run on macOS/Linux (you can still read and edit code there; CI builds on `windows-latest`).
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A [Spotify Developer app](https://developer.spotify.com/dashboard) (Client ID/Secret) to test against the real API — Spotify **Premium** is required for playback commands.

## Build, run, test

All commands from the repo root:

```bash
dotnet restore src/BeatBind.sln
dotnet build src/BeatBind.sln --configuration Release

# Run the app
cd src/BeatBind && dotnet run

# Run all tests
dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj

# Run a single test class or test
dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj --filter "FullyQualifiedName~HotkeyServiceTests"
dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj --filter "DisplayName~PlayPauseAsync_WhenPlaying_ShouldPause"
```

CI (`.github/workflows/ci.yml`) runs restore → build (Release) → test on every push/PR to `dev`. Run the same sequence locally before opening a PR.

## Branch model

| Branch | Purpose |
|--------|---------|
| `main` | Stable releases |
| `dev`  | Active development — **target your PRs here** |
| `beta` | Every push auto-publishes a beta GitHub release (`build-and-release.yml`) |

## Where things live

Clean Architecture, five projects under `src/`, dependencies flow inward only (Presentation → Application → Infrastructure → Core):

- **`BeatBind.Core`** — entities (`Track`, `Hotkey`, `PlaybackState`, …) and interfaces (`ISpotifyService`, `IHotkeyService`, …). No external dependencies.
- **`BeatBind.Application`** — `*ApplicationService` classes orchestrating business logic against Core interfaces. This layer owns the playback-state cache that makes hotkeys fast.
- **`BeatBind.Infrastructure`** — implementations: `SpotifyService` (Web API + token refresh), `AuthenticationService` (OAuth flow), `ConfigurationService` (`%APPDATA%\BeatBind\config.json`), `HotkeyService` (Win32 low-level keyboard hook), `GithubReleaseService`, `StartupService`.
- **`BeatBind.Presentation`** — WinForms UI (MaterialSkin.2): `MainForm` shell, `Panels/`, `Components/`, `Helpers/`, `Themes/`.
- **`BeatBind.Tests`** — xUnit + Moq + FluentAssertions, mirrors the layer structure.

See [ARCHITECTURE.md](ARCHITECTURE.md) for diagrams and [CLAUDE.md](CLAUDE.md) for a condensed map (also used by AI coding assistants).

### Recipes for common changes

- **New Spotify API capability** → add a method to `ISpotifyService` (Core) → implement in `SpotifyService` (Infrastructure) using the shared `SendRequestAsync` helper (it handles auth headers, HTTP/2, and 401 refresh-retry) → call it from the relevant `*ApplicationService` → wire up UI in a Panel.
- **New hotkey action** → **append** to the end of the `HotkeyAction` enum (Core) → handle it in `HotkeyApplicationService.ExecuteHotkeyAction` → it appears in `HotkeyEditorDialog` automatically. Appending is not a style preference: the enum is serialized to `config.json` as an integer ordinal, so inserting a member mid-list silently remaps every existing user's saved hotkeys to different actions. If it reads playback state, go through `ExecuteWithPlaybackStateAsync` in `MusicControlApplicationService` so it benefits from the cache and lock.
- **New config option** → extend `ApplicationConfiguration` (Core) → it round-trips through `ConfigurationService` automatically → expose it in `SettingsPanel`.

### Things that bite

- **Spotify's JSON is full of nullable fields** (`item`, `progress_ms`, `device.volume_percent` can all be `null` — during ads, private sessions, casting). Parse with the `GetInt32OrNull`/`GetStringOrEmpty`-style helpers in `SpotifyService`; never `GetProperty(...).GetInt32()` directly.
- **`MusicControlApplicationService` serializes commands behind `_playbackLock`** and caches playback state for 2 seconds with optimistic updates. Any new command that reads-then-writes playback state must go through the existing helpers, or you'll reintroduce races.
- **`.Designer.cs` / `InitializeComponent` rules**: no lambdas, ternaries, control flow, or `nameof()` inside generated UI code — see `.github/agents/WinFormsExpert.agent.md`.
- **Style is enforced by `.editorconfig` as build warnings** — don't introduce new warnings. Highlights: `var` only when the type is apparent from the right-hand side; `_camelCase` for private fields (including `static readonly`); predefined type keywords (`int`, not `Int32`); match the surrounding file's namespace style.

## Debugging user issues

- Config: `%APPDATA%\BeatBind\config.json`
- Logs: `%APPDATA%\BeatBind\log-*.txt` (rolling, kept 48 hours). Every Spotify API request is logged (`PUT https://api.spotify.com/v1/me/player/pause`), which is usually enough to diagnose "my hotkey did nothing" reports — ask users for this file.

## Releasing a new version

1. Bump `CURRENT_VERSION` in `src/BeatBind.Presentation/MainForm.cs` (the update checker compares this against the latest GitHub release tag).
2. Update the version badge at the top of `README.md`.
3. Merge `dev` → `main`, then create a GitHub release with tag `vX.Y.Z` and attach the published build:
   ```bash
   dotnet publish src/BeatBind/BeatBind.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```

Version numbers follow [SemVer](https://semver.org/): patch for fixes, minor for new features, major for breaking changes (e.g. config format).

## Pull request checklist

- [ ] Targets `dev`
- [ ] `dotnet build` succeeds with **no new warnings**
- [ ] `dotnet test` passes; new behavior has tests
- [ ] User-facing changes described in the PR body
