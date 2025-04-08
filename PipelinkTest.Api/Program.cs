using Pipelink.Behaviors;
using Pipelink.Extensions;
using Pipelink.Handlers;
using Pipelink.Interfaces;
using PipelinkTest.Api.CommandHandlers;
using PipelinkTest.Api.Commands;
using PipelinkTest.Api.Dtos;
using PipelinkTest.Api.Notifications;
using PipelinkTest.Api.Queries;
using PipelinkTest.Api.QueryHandlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddPipelink();
builder.Services.AddPipelinkHandlersAndBehaviors();
builder.Services.AddTransient<IRequestHandler<GetUserQuery, UserDto>, GetUserQueryHandler>();
builder.Services.AddTransient<IRequestHandler<LoginUserCommand, LoginUserDto>, LoginUserCommandHandler>();
builder.Services.AddTransient<IPipelineBehavior<GetUserQuery, UserDto>, ValidationBehavior<GetUserQuery, UserDto>>();
builder.Services.AddTransient<INotificationHandler<UserCreatedNotification>, SendWelcomeEmailHandler>();

var app = builder.Build();

var mediator = app.Services.GetRequiredService<Pipelink.Implementation.Pipelink>();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/user", async () =>
{
    var user = await mediator.Send(new GetUserQuery(1));

    return user;
}).WithName("GetUserById")
.WithOpenApi();

app.MapGet("/login", async () => 
    {
        var loginUser = await mediator.Send(new LoginUserCommand("burak.seyhan@commencis.com"));

        return loginUser;
    }).WithName("LoginUser")
    .WithOpenApi();

app.MapPost("/notify-user", async () =>
{
    await mediator.Publish(new UserCreatedNotification { UserId = 123 });
    return Results.Ok("Notification sent");
}).WithName("NotifyUser")
.WithOpenApi();

app.MapGet("/weatherforecast", async () =>
    {
        var user = await mediator.Send(new GetUserQuery(1));
        
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

await app.RunAsync();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}