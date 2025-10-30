using System.Text.Json;
using AgOpenGPS.Api.Abstractions;
using AgOpenGPS.Api.Configuration;
using AgOpenGPS.Api.Hubs;
using AgOpenGPS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Options pattern for UDP settings
builder.Services.Configure<UdpOptions>(
    builder.Configuration.GetSection(UdpOptions.SectionName));

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

// Register state publisher
builder.Services.AddSingleton<IStatePublisher, SignalRStatePublisher>();

// Register UDP packet receiver (singleton - owns UDP connection)
builder.Services.AddSingleton<IUdpPacketReceiver, UdpPacketReceiver>();

// Register GPS processing service
builder.Services.AddSingleton<IGnssService, GnssService>();

// Register coordinate transformation service
builder.Services.AddSingleton<ICoordinateService, CoordinateService>();

// Register MediatR for CQRS command handling
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Register simulator services
builder.Services.AddSingleton<VehiclePhysicsService>();
builder.Services.AddSingleton<GnssDataGenerator>();
builder.Services.AddSingleton<AgIoProtocolSerializer>();
builder.Services.AddSingleton<SimulatorService>();
builder.Services.AddHostedService<SimulatorHostedService>();

// Register ApplicationOrchestrator as hosted service (GPS-driven)
builder.Services.AddHostedService<ApplicationOrchestrator>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors();

// Map SignalR hub endpoint
app.MapHub<StateHub>("/statehub");

app.Run();

// Make Program class accessible for testing with WebApplicationFactory
public partial class Program { }
