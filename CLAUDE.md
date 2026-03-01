# PingKeeper -- Keep Cloud Instances Alive

Periodically pings configured URLs (including itself) to keep cloud instances alive.
Without these pings, the server manager marks the instance as unused and destroys it.
Built with C# / .NET 8 as a standalone web worker.

For detailed architecture, design decisions, and deep documentation see the `.adn/` folder.

---

## Quick Commands

```bash
dotnet build PingKeeper.sln                        # build all
dotnet run                                         # run locally
dotnet test PingKeeper.sln                         # run all tests
dotnet test --filter "Category=Unit"               # unit tests only
dotnet test --filter "Category=E2E"                # E2E tests only (requires Docker)
docker build -t pingkeeper .                       # build Docker image
docker run -p 8080:8080 pingkeeper                 # run in Docker
curl http://localhost:8080/ping                    # test health endpoint
```

---

## Project Structure

```
PingKeeper.sln
PingKeeper.csproj
Program.cs
appsettings.json
appsettings.Development.json
Dockerfile
.dockerignore
.gitignore
LICENSE                       # MIT License
Models/
  PingKeeperConfig.cs         # Root config (interval, timeout, endpoints)
  ServiceEndpoint.cs          # Single monitored URL
  WebhookConfig.cs            # Webhook notification settings
  WebhookPayload.cs           # JSON payload record for webhooks
  ServiceState.cs             # Per-endpoint state machine (up/down tracking)
Services/
  PingWorker.cs               # BackgroundService with PeriodicTimer — main loop
  INotificationService.cs     # Notification interface
  WebhookNotificationService.cs  # Webhook (HTTP POST) implementation
  ServiceStateTracker.cs      # Singleton dictionary of ServiceState per endpoint
tests/
  PingKeeper.Tests/
    Unit/                     # 31 unit tests (ServiceState, Tracker, Webhook, PingWorker)
    E2E/                      # 8 E2E tests (Docker containers, public servers)
    Helpers/                  # MockHttpMessageHandler
Documents/
  Guide.docx                  # Original project guide
.adn/                         # Deep documentation (architecture, flows, config)
```

---

## Key Conventions

- **Framework**: C# / .NET 8, `Microsoft.NET.Sdk.Web` (minimal API for `/ping` endpoint + BackgroundService)
- **Pattern**: HYBR8 operational cycle (Heartbeat → Yield → Backoff → Recovery → 8-second grace)
- **Scheduling**: Built-in `PeriodicTimer` via `BackgroundService` (no Hangfire)
- **HTTP**: `IHttpClientFactory` with named clients (`"Ping"`, `"Webhook"`)
- **Configuration**: `IOptions<T>` / `IOptionsMonitor<T>` pattern; settings in `appsettings.json`
  - `appsettings.Development.json` is git-ignored
- **Logging**: `Microsoft.Extensions.Logging` (built-in, structured)
- **NuGet packages**: Zero additional for main project — everything from the SDK
- **Testing**: xUnit + FluentAssertions + Moq + Testcontainers
  - Unit tests: mocked HTTP, category `Unit`
  - E2E tests: Docker containers + public servers, category `E2E`
- **Docker**: Multi-stage build, `aspnet:8.0` runtime, port 8080

---

## Documentation (.adn/)

The `.adn/` directory is the **project DNA** — it contains the authoritative documentation for this project.

**IMPORTANT: Keep `.adn/` in sync with code changes.**
After any code change that affects behaviour, architecture, or contracts, update the `.adn/` docs:

| Change type | Update these `.adn/` files |
|---|---|
| Architecture or design decision change | `architecture/overview.md` |
| Config option added/removed/renamed | `configuration/config-reference.md` |
| Ping loop or notification flow change | `flow/ping-loop.md` |
| Deployment, Docker, or hosting change | `operations/deployment.md` |
| Coding pattern or convention change | `growth/coding-conventions.md` |
| Test strategy or convention change | `testing/testing-strategy.md` |
| Any significant change | `growth/self-maintenance.md` (verify checklist) |

### When to create new files

If a change introduces a new major concept that doesn't fit into an existing doc, create a new `.md` file under `.adn/`. Update `.adn/README.md` to include it in the folder map.

---

## Self-Sustaining Growth

1. **Before coding**: Read relevant `.adn/` docs
2. **While coding**: Follow patterns in `.adn/growth/coding-conventions.md`
3. **After coding**: Run the checklist in `.adn/growth/self-maintenance.md`:
   - `dotnet build PingKeeper.sln` — 0 errors, 0 warnings
   - `dotnet test PingKeeper.sln` — all tests pass
   - `dotnet run` — verify pings work
   - Update `.adn/` docs (mapping table above)
