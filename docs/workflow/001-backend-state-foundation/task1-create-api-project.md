# Task 1: Create AgOpenGPS.Api Project

## Goal

Create the AgOpenGPS.Api backend project as a .NET 8 Web API with SignalR support.

## Steps

1. Create new .NET 8 Web API project in SourceCode/AgOpenGPS.Api/
2. Configure project settings (target framework, output directory)
3. Add SignalR package dependency
4. Configure minimal API startup (Program.cs)
5. Add SignalR endpoint routing
6. Configure CORS for localhost development (allow FormGPS to connect)
7. Set application URL (e.g., http://localhost:5000)
8. Verify project builds successfully

## Key Points

- Use .NET 8 (cross-platform, modern)
- Enable SignalR in dependency injection
- Configure HTTP/WebSockets transport
- Allow CORS from localhost (FormGPS will connect from different process)
- Keep startup configuration minimal (no authentication, no HTTPS for now)
- Default to console hosting (Kestrel web server)

## Project Structure

After completion, the project should have:
- AgOpenGPS.Api.csproj (project file)
- Program.cs (application entry point and configuration)
- Properties/launchSettings.json (development settings)

## Acceptance

- [ ] AgOpenGPS.Api project exists in SourceCode/AgOpenGPS.Api/
- [ ] Project targets .NET 8
- [ ] SignalR packages added
- [ ] Program.cs configures SignalR and CORS
- [ ] Project builds without errors
- [ ] Can run with `dotnet run` (starts web server on localhost:5000)

## Test

Run `dotnet build SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj` - should build successfully.
Run `dotnet run --project SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj` - should start web server.
