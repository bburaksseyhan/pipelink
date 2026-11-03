# Pipelink sample hosts (API, Worker, Console)

Reference implementations for **Pipelink 2.x** on **.NET 10**. All samples use `AddPipelink` + assembly scanning.

## Projects

| Project | Host type | Purpose |
|---------|-----------|---------|
| `PipelinkTest.Api` | ASP.NET Core Minimal API | HTTP endpoints calling `Send` / `Publish` / `SendStream`, optional metrics middleware |
| `PipelinkTest.Worker` | `BackgroundService` | Periodic `Send` via `ProcessTickCommand` |
| `PipelinkTest.Console` | Generic host + one-shot exit | CLI `GreetQuery` demo |

## Registration pattern

```csharp
builder.Services.AddPipelink(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<YourHandlerType>());
```

Register handlers in the **same assembly** (or add `RegisterServicesFromAssembly` for others).

## Run locally

```bash
dotnet build pipelink.sln -c Release
dotnet test pipelink.sln -c Release

# API (Swagger/OpenAPI in Development)
dotnet run --project PipelinkTest.Api

# Console — optional name argument
dotnet run --project PipelinkTest.Console -- World

# Worker — one tick then exit
dotnet run --project PipelinkTest.Worker -- --run-once
```

### Worker configuration

`appsettings.json`:

```json
"PipelinkWorker": {
  "RunOnce": false,
  "IntervalSeconds": 5
}
```

`appsettings.Development.json` sets `RunOnce: true` for local/CI smoke runs.

## Tests

`Pipelink.Tests/SampleHostTests.cs`:

- Console / Worker handler wiring via DI
- API integration via `WebApplicationFactory<ApiApplicationEntry>` (`GET /user/42`)

## API marker for tests

`PipelinkTest.Api/ApiApplicationEntry.cs` is an empty assembly marker for `WebApplicationFactory` (avoids `Program` type clashes with other sample hosts).
