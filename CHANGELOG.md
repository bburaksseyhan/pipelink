# Changelog

All notable changes to this project are documented in this file.

## [2.0.0] - 2026-11-03

### Breaking

- Target framework is **`net10.0` only** (drops .NET 8/9 support).
- NuGet package version **2.0.0** — consumers must upgrade the SDK/runtime before referencing this package.

### Fixed

- **`Send` now executes the `IPipelineBehavior` pipeline** before the request handler (registration order = outermost-first). Previously behaviors were registered in DI but bypassed.
- **Stream handlers** are resolved from DI on each call (no stale singleton handler in compiled delegates).
- **`Publish`** uses typed `INotificationHandler<T>` dispatch instead of reflection-only invocation.
- Removed duplicate **`StreamUserQueryHandler`** in the sample API.
- Removed redundant **NuGet + ProjectReference** on `PipelinkTest.Api` (project reference only).
- Updated **`RepositoryUrl` / `PackageProjectUrl`** to `bburaksseyhan/pipelink`.

### Changed

- Replaced `Microsoft.AspNetCore.App` **PackageReference** with **FrameworkReference**.
- Removed unused **FluentValidation** package dependency from the core library (validation remains extensible via behaviors).
- Demo API uses built-in **`AddOpenApi` / `MapOpenApi`** instead of Swashbuckle (Swashbuckle 8.x was incompatible with .NET 10 at migration time).
- Added **`global.json`** pinning SDK 10.0.102.
- Added **`Pipelink.Tests`** (xUnit) covering Send, Publish, SendStream, pipeline ordering, and DI registration.

### Notes / follow-ups

- Re-benchmark against MediatR on .NET 10; README numbers are from the 1.x era.
- Transitive **`Microsoft.OpenApi` 2.0.0** advisory (NU1903) may still appear until upstream packages ship patched versions — track and bump when available.
- **`IStreamPipelineBehavior`** is still not wired into `SendStream` (future enhancement).
- **`CachingBehavior`** uses an in-process static dictionary — not suitable for multi-instance deployments without review.

## [1.0.9] and earlier

See git history on the `production` branch prior to the `chore/net10-migration` branch.
