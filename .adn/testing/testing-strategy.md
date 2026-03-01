# Testing Strategy

## Overview

PingKeeper uses xUnit with FluentAssertions and Moq for unit testing, and Testcontainers for Docker-based E2E tests. Tests are categorised via `[Trait("Category", "Unit|E2E")]`.

## Test Project

```
tests/PingKeeper.Tests/
  GlobalUsings.cs              # global using Xunit;
  Helpers/
    MockHttpMessageHandler.cs  # Reusable HTTP mock (status code or custom func)
  Unit/
    ServiceStateTests.cs       # State machine transitions (11 tests)
    ServiceStateTrackerTests.cs # Dictionary management (4 tests)
    WebhookNotificationServiceTests.cs # Webhook delivery, skip, error handling (8 tests)
    PingWorkerTests.cs         # Ping logic with mocked HTTP (8 tests)
  E2E/
    DockerPingTests.cs         # Docker containers up/down/recovery (3 tests)
    PublicServerPingTests.cs   # Real public servers + failure detection (5 tests)
```

## Test Categories

| Category | Trait | What | Dependencies | Command |
|----------|-------|------|-------------|---------|
| Unit | `[Trait("Category", "Unit")]` | All business logic | Mocked HTTP, in-memory | `dotnet test --filter "Category=Unit"` |
| E2E | `[Trait("Category", "E2E")]` | Full system integration | Docker, internet | `dotnet test --filter "Category=E2E"` |

## Running Tests

```bash
dotnet test PingKeeper.sln                # all tests
dotnet test --filter "Category=Unit"      # unit only (fast, no Docker)
dotnet test --filter "Category=E2E"       # E2E only (requires Docker + internet)
```

## Unit Test Patterns

### Mocking IHttpClientFactory

```csharp
var handler = new MockHttpMessageHandler(HttpStatusCode.OK);
var factory = new Mock<IHttpClientFactory>();
factory.Setup(f => f.CreateClient("Ping"))
    .Returns(() => new HttpClient(handler, disposeHandler: false));
```

**Critical:** Use `disposeHandler: false` and a lambda `Returns(() => ...)` to create a new HttpClient per call. Without this, the handler gets disposed after the first client usage.

### MockHttpMessageHandler

Two constructors:
- `MockHttpMessageHandler(HttpStatusCode)` — fixed response
- `MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>)` — custom per-request logic

Tracks all requests via `SentRequests` list for assertions.

### Testing PingWorker

`PingAllEndpointsAsync` is `internal` (exposed via `InternalsVisibleTo`). Tests call it directly with mocked dependencies:

```csharp
var worker = new PingWorker(factory, optionsMonitor, stateTracker, notification, logger);
await worker.PingAllEndpointsAsync(CancellationToken.None);
// Assert on stateTracker and notification mock
```

## E2E Test Patterns

### Docker Tests (Testcontainers)

Uses `IAsyncLifetime` to manage container lifecycle per test:

```csharp
public class DockerPingTests : IAsyncLifetime
{
    private IContainer _container = null!;

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("nginx:alpine")
            .WithPortBinding(80, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();
        await _container.StartAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

**Test scenarios:**
1. Container running → ping succeeds
2. Container stopped → consecutive failures → declared DOWN → notification fires
3. Container restarted → recovery detected → recovery notification fires

**Note:** On Docker for Windows, port mappings may change after container stop/start. The recovery test handles this gracefully.

### Public Server Tests

Test against reliable public endpoints:
- `https://www.google.com` — always reachable
- `https://1.1.1.1` (Cloudflare) — always reachable
- `https://www.microsoft.com` — always reachable
- `.invalid` TLD — guaranteed DNS failure

These tests require internet connectivity.

## NuGet Packages (Test Project Only)

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.9.2 | Test framework |
| xunit.runner.visualstudio | 2.8.2 | VS Test adapter |
| Microsoft.NET.Test.Sdk | 17.11.1 | Test SDK |
| FluentAssertions | 6.12.2 | `.Should()` assertions |
| Moq | 4.20.72 | Mocking |
| Testcontainers | 3.10.0 | Docker container management |
| Microsoft.Extensions.Http | 8.0.1 | Real IHttpClientFactory in E2E |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | NullLogger for tests |
| Microsoft.Extensions.Options | 8.0.2 | IOptionsMonitor mock |

## Test Naming Convention

Pattern: `MethodName_Scenario_ExpectedResult`

Examples:
```
RecordSuccess_WhenUp_ReturnsFalse
RecordFailure_AtThreshold_ReturnsTrue
PingAllEndpoints_FailureAtThreshold_NotifiesDown
Ping_DetectsRecovery_AfterContainerRestarted
```
