#!/usr/bin/env bash
# Installs Orient.Pipelink from a local package folder into fresh Console, Worker and Web API projects and runs them.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
package_dir="$(cd "${1:-$root/artifacts/packages}" && pwd)"
version="${2:-$(dotnet msbuild "$root/src/Orient.Pipelink/Orient.Pipelink.csproj" -getProperty:Version)}"
port="${VERIFY_CONSUMER_PORT:-5187}"
work="$(mktemp -d)"
export NUGET_PACKAGES="$work/packages"

cat > "$work/nuget.config" << EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$package_dir" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="Orient.Pipelink" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

new_consumer() {
  local template="$1" name="$2"
  dotnet new "$template" -n "$name" -o "$work/$name" -f net10.0 --no-restore > /dev/null
  cp "$work/nuget.config" "$work/$name/nuget.config"
  dotnet add "$work/$name/$name.csproj" package Orient.Pipelink --version "$version" > /dev/null
}

# Console: Send, Publish, SendStream and an opt-in behavior.
new_consumer console ConsoleConsumer
dotnet add "$work/ConsoleConsumer/ConsoleConsumer.csproj" package Microsoft.Extensions.Hosting --version 10.0.0 > /dev/null
cat > "$work/ConsoleConsumer/Program.cs" << 'EOF'
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orient.Pipelink.Behaviors;
using Orient.Pipelink.Extensions;
using Orient.Pipelink.Handlers;
using Orient.Pipelink.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<List<string>>();
builder.Services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Ping>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
using var host = builder.Build();
using var scope = host.Services.CreateScope();
var pipelink = scope.ServiceProvider.GetRequiredService<IPipelink>();

var pong = await pipelink.Send(new Ping("orient"));
await pipelink.Publish(new Pinged(pong));
var streamed = new List<int>();
await foreach (var n in pipelink.SendStream(new Count(3))) streamed.Add(n);

var log = scope.ServiceProvider.GetRequiredService<List<string>>();
if (pong != "pong:orient" || log.Count != 1 || string.Join(',', streamed) != "0,1,2")
    throw new InvalidOperationException($"unexpected results: {pong} {log.Count} {string.Join(',', streamed)}");
Console.WriteLine($"console ok: {pong}, published={log.Count}, stream={string.Join(',', streamed)}");

public record Ping(string Name) : IRequest<string>;
public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult($"pong:{request.Name}");
}
public record Pinged(string Value) : INotification;
public class PingedHandler(List<string> log) : INotificationHandler<Pinged>
{
    public Task Handle(Pinged notification, CancellationToken cancellationToken) { log.Add(notification.Value); return Task.CompletedTask; }
}
public record Count(int To) : IStreamRequest<int>;
public class CountHandler : IStreamRequestHandler<Count, int>
{
    public async IAsyncEnumerable<int> Handle(Count request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < request.To; i++) { await Task.Yield(); yield return i; }
    }
}
EOF
dotnet run --project "$work/ConsoleConsumer/ConsoleConsumer.csproj" -c Release

# Worker: a BackgroundService that creates a scope per unit of work and sends through the pipelink.
new_consumer worker WorkerConsumer
cat > "$work/WorkerConsumer/Program.cs" << 'EOF'
using Orient.Pipelink.Extensions;
using Orient.Pipelink.Handlers;
using Orient.Pipelink.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddScoped<ScopeId>();
builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<Tick>());
builder.Services.AddHostedService<TickWorker>();
await builder.Build().RunAsync();

public class ScopeId { public Guid Value { get; } = Guid.NewGuid(); }
public record Tick(int Number) : IRequest<Guid>;
public class TickHandler(ScopeId scopeId) : IRequestHandler<Tick, Guid>
{
    public Task<Guid> Handle(Tick request, CancellationToken cancellationToken) => Task.FromResult(scopeId.Value);
}
public class TickWorker(IServiceScopeFactory scopes, IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ids = new HashSet<Guid>();
        for (var i = 0; i < 3; i++)
        {
            using var scope = scopes.CreateScope();
            ids.Add(await scope.ServiceProvider.GetRequiredService<IPipelink>().Send(new Tick(i), stoppingToken));
        }
        if (ids.Count != 3) throw new InvalidOperationException("scoped handler dependency leaked across scopes");
        Console.WriteLine($"worker ok: {ids.Count} ticks in {ids.Count} scopes");
        lifetime.StopApplication();
    }
}
EOF
dotnet run --project "$work/WorkerConsumer/WorkerConsumer.csproj" -c Release

# Web API: minimal endpoint with a scoped dependency, called over HTTP.
new_consumer web ApiConsumer
cat > "$work/ApiConsumer/Program.cs" << 'EOF'
using Orient.Pipelink.Extensions;
using Orient.Pipelink.Handlers;
using Orient.Pipelink.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<Greeter>();
builder.Services.AddPipelink(cfg => cfg.RegisterServicesFromAssemblyContaining<Greet>());
var app = builder.Build();
app.MapGet("/greet/{name}", (string name, IPipelink pipelink, CancellationToken ct) => pipelink.Send(new Greet(name), ct));
app.Run();

public class Greeter { public string Hello(string name) => $"Hello, {name}"; }
public record Greet(string Name) : IRequest<string>;
public class GreetHandler(Greeter greeter) : IRequestHandler<Greet, string>
{
    public Task<string> Handle(Greet request, CancellationToken cancellationToken) => Task.FromResult(greeter.Hello(request.Name));
}
EOF
dotnet build "$work/ApiConsumer/ApiConsumer.csproj" -c Release > /dev/null
dotnet "$work/ApiConsumer/bin/Release/net10.0/ApiConsumer.dll" --urls "http://127.0.0.1:$port" > "$work/api.log" 2>&1 &
api_pid=$!
trap 'kill "$api_pid" 2>/dev/null || true' EXIT
response=""
for _ in $(seq 1 40); do
  response="$(curl -fsS "http://127.0.0.1:$port/greet/orient" 2>/dev/null || true)"
  [[ -n "$response" ]] && break
  sleep 0.25
done
if [[ "$response" != "Hello, orient" ]]; then
  cat "$work/api.log" >&2
  echo "api consumer returned '$response'" >&2
  exit 1
fi
echo "api ok: $response"

echo "local consumer proof ok ($version)"
