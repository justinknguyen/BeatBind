<!-- Thanks for contributing! See CONTRIBUTING.md for the full guide. -->

## What does this PR do?



## Checklist

- [ ] Targets the `dev` branch
- [ ] `dotnet build src/BeatBind.sln --configuration Release` succeeds with no new warnings
- [ ] `dotnet test src/BeatBind.Tests/BeatBind.Tests.csproj` passes
- [ ] New behavior is covered by tests
- [ ] No architecture-layer violations (dependencies flow Presentation → Application → Infrastructure → Core only)
