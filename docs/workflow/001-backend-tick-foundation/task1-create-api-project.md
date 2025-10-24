# Task 1: Create AgOpenGPS.Api Project

## Goal

Create new .NET 8 Web API project with SignalR.

## Steps

1. Create project: `dotnet new webapi -n AgOpenGPS.Api` in `SourceCode/`
2. Add SignalR (already in .NET 8 template)
3. Configure Program.cs: AddSignalR, CORS, run on port 5000
4. Add root endpoint that returns "AgOpenGPS.Api is running"

## Files Created

- `SourceCode/AgOpenGPS.Api/Program.cs`
- `SourceCode/AgOpenGPS.Api/appsettings.json`
- `SourceCode/AgOpenGPS.Api/AgOpenGPS.Api.csproj`

## Acceptance

- [ ] Project builds
- [ ] Runs on localhost:5000
- [ ] Root endpoint returns message
- [ ] SignalR services registered

## Test

```bash
dotnet run
curl http://localhost:5000
```
