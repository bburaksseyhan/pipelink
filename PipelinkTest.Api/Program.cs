using Pipelink.Extensions;
using PipelinkTest.Api.Commands;
using PipelinkTest.Api.CommandHandlers;
using PipelinkTest.Api.Dtos;
using PipelinkTest.Api.Notifications;
using PipelinkTest.Api.Queries;
using PipelinkTest.Api.QueryHandlers;
using PipelinkTest.Api.Stream;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Pipelink with automatic handler and behavior registration
builder.Services.AddPipelink(cfg => 
{
    // Register handlers from the current assembly
    cfg.RegisterServicesFromAssemblyContaining<Program>();
});

var app = builder.Build();

var pipelink = app.Services.GetRequiredService<Pipelink.Implementation.Pipelink>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/user/{id}", async (int id) =>
{
    var user = await pipelink.Send(new GetUserQuery(id));
    return Results.Ok(user);
}).WithName("GetUserById")
.WithOpenApi();

app.MapPost("/login", async (LoginUserCommand command) => 
{
    var loginUser = await pipelink.Send(command);
    return Results.Ok(loginUser);
}).WithName("LoginUser")
.WithOpenApi();

app.MapPost("/notify-user", async (UserCreatedNotification notification) =>
{
    await pipelink.Publish(notification);
    return Results.Ok("Notification sent");
}).WithName("NotifyUser")
.WithOpenApi();

app.MapGet("/stream-users", async (int count = 10) =>
{
    var users = pipelink.SendStream(new StreamUserQuery(count));
    return Results.Ok(users);
}).WithName("StreamUsers")
.WithOpenApi();

await app.RunAsync();
