using System.Text.Json;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Hubs;
using AgOpenGPS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        // Configure JSON serialization for SignalR messages
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configure CORS for localhost development (allow FormGPS connections)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register SignalR state publisher implementation
builder.Services.AddSingleton<IStatePublisher, SignalRStatePublisher>();

// Register ApplicationOrchestrator as hosted service (runs at 10 Hz)
builder.Services.AddHostedService<ApplicationOrchestrator>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors();

// Map SignalR hub endpoint
app.MapHub<StateHub>("/statehub");

app.Run();

// Make Program class accessible for testing with WebApplicationFactory
public partial class Program { }
