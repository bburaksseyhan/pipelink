using Pipelink.Extensions;
using PipelinkTest.Api.Commands;
using PipelinkTest.Api.Dtos;
using PipelinkTest.Api.Notifications;
using PipelinkTest.Api.Queries;

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

app.MapGet("/user", async () =>
{
    var user = await pipelink.Send(new GetUserQuery(1));

    return user;
}).WithName("GetUserById")
.WithOpenApi();

app.MapGet("/login", async () => 
    {
        var loginUser = await pipelink.Send(new LoginUserCommand("burak.seyhan@commencis.com"));

        return loginUser;
    }).WithName("LoginUser")
    .WithOpenApi();

app.MapPost("/notify-user", async () =>
{
    await pipelink.Publish(new UserCreatedNotification { UserId = 123 });
    return Results.Ok("Notification sent");
}).WithName("NotifyUser")
.WithOpenApi();

app.MapGet("/stream-users", async (int count = 10) =>
{
    var users = pipelink.SendStream<StreamUserQuery, UserDto>(new StreamUserQuery(count));
    return users;
}).WithName("StreamUsers")
.WithOpenApi();

await app.RunAsync();
