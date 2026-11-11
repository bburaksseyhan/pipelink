using Orient.Pipelink.Behaviors;
using Orient.Pipelink.Extensions;
using Orient.Pipelink.SampleApi.Endpoints;
using Orient.Pipelink.SampleApi.Features.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// A scoped dependency consumed by handlers, to demonstrate DI inside handlers
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();

// Orient.Pipelink: scan this assembly for handlers, opt in to the logging behavior
builder.Services.AddPipelink(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapUserEndpoints();

await app.RunAsync();
