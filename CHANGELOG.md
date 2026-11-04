# Changelog

## [2.0.0]

### Breaking

- Targets **`net10.0` only**. Projects on .NET 8 or 9 should stay on Pipelink 1.x.

### Changed

- `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions` 10.0.0.
- `Send` calls the handler directly when no pipeline behaviors are registered for the request, skipping the delegate allocation. On .NET 10 (Apple M3): 53.5 ns / 208 B, versus 74.6 ns / 272 B for 1.2.0's dispatch and 67.2 ns / 272 B for MediatR 12.5.0.
- Sample API uses the built-in `AddOpenApi` / `MapOpenApi` instead of Swashbuckle.

### Added

- `PipelinkTest.Worker` and `PipelinkTest.Console` samples.
- `Pipelink.IntegrationTests`: the sample API over HTTP, the Worker and Console on a real generic host and as separate processes, scoped handler dependencies, opt-in behaviors and concurrent `Send`.
- `global.json` pinning SDK 10.0.102; CI and publish workflows read it.
- The publish workflow fails when the pushed `v*` tag does not match `<Version>` in `Pipelink/Pipelink.csproj`.

## [1.2.0] and earlier

See the git history.
