# Changelog

All notable changes to this project are documented in this file.

## [2.0.0] - 2026-11-03

### Breaking

- Target framework is **`net10.0` only** (drops .NET 8/9 support).
- NuGet package version **2.0.0** — consumers must upgrade the SDK/runtime before referencing this package.
- `AddPipelink` registers `Pipelink` as **transient** (was singleton) so handlers and behaviors resolve from the caller's DI scope. Inject it where you use it (controller, endpoint, scoped service) instead of resolving it once from `app.Services`; a root-resolved instance cannot reach scoped dependencies. The dispatch cache is a per-container singleton, so `Send` speed is unchanged.

### Fixed

- **`Send` now executes the `IPipelineBehavior` pipeline** before the request handler (registration order = outermost-first). Previously behaviors were registered in DI but bypassed.
- **Pipeline behaviors** are resolved from DI on every `Send`, so transient/scoped behavior lifetimes are honored (an interim build cached behavior instances).
- **`Send` fast path:** one cached generic wrapper per request type replaces compiled expression trees; with no behaviors registered the handler is called directly (~37 ns / 208 B vs MediatR 12.5 ~62–74 ns / 272 B on Apple M3).
- Handlers with **scoped dependencies** (for example a `DbContext`) no longer fail with "Cannot resolve ... from root provider" when `Send` is called inside a request scope.
- **`CachingBehavior`** uses a `ConcurrentDictionary`; the previous `Dictionary` could be corrupted by concurrent requests ("Operations that change non-concurrent collections must have exclusive access").
- Sample API `/metrics/visualize` no longer returns 500 when no metrics are recorded.
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
- Added **`Pipelink.IntegrationTests`**: the sample API over HTTP (`WebApplicationFactory`), the Worker and Console samples on a real generic host and as separate processes, scoped dependencies, and concurrent `Send`.

### Notes / follow-ups

- `Send` no longer wraps the call in an extra `async` method, so DI resolution errors (for example a missing handler) are thrown synchronously from `Send` instead of through the returned task. `await mediator.Send(...)` callers see no difference.
- Transitive **`Microsoft.OpenApi` 2.0.0** advisory (NU1903) may still appear until upstream packages ship patched versions — track and bump when available.
- **`IStreamPipelineBehavior`** is still not wired into `SendStream` (future enhancement).
- **`CachingBehavior`** uses an in-process static dictionary — not suitable for multi-instance deployments without review.

## [1.0.9] and earlier

See git history on the `production` branch prior to the `chore/net10-migration` branch.
