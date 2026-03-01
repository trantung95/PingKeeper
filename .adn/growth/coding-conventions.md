# Coding Conventions

Authoritative reference for all naming, patterns, and rules used in PingKeeper.

---

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Files | PascalCase, matches primary class | `PingWorker.cs` |
| Classes | PascalCase with role suffix | `WebhookNotificationService` |
| Interfaces | `I`-prefix + PascalCase | `INotificationService` |
| Config POCOs | PascalCase + `Config` suffix | `PingKeeperConfig`, `WebhookConfig` |
| Records | PascalCase (for DTOs/payloads) | `WebhookPayload` |

### Class Suffix Rules

| Suffix | Purpose |
|--------|---------|
| `*Config` | Config POCO bound to `appsettings.json` section |
| `*Worker` | BackgroundService (hosted service) |
| `*Service` | Business logic / notification delivery |
| `*Tracker` | Stateful singleton tracking runtime data |
| `*Payload` | Immutable data transfer object (record) |
| `*State` | Mutable per-entity runtime state |
| `*Endpoint` | Config model for a monitored URL |

---

## Config POCO Pattern

```csharp
/// <summary>
/// Top-level settings for the feature.
/// Used by the corresponding worker or service class.
/// Nested collections define sub-entities.
/// Global defaults apply when per-entity overrides are absent.
/// </summary>
public sealed class MyFeatureConfig
{
    public const string SectionName = "MyFeature";

    /// <summary>Description.</summary>
    public int IntervalSeconds { get; set; } = 60;
}
```

Rules:
- `sealed class`
- `const string SectionName` matching appsettings.json key
- Default values on all properties
- XML doc comments on public properties

Registration:
```csharp
builder.Services.Configure<MyFeatureConfig>(
    builder.Configuration.GetSection(MyFeatureConfig.SectionName));
```

Inject as `IOptionsMonitor<T>` (for hot-reload) or `IOptions<T>` (for read-once).

---

## DI Lifetime Rules

| Lifetime | When to Use | Examples |
|----------|-------------|---------|
| **Singleton** | Stateless services, stateful trackers, notification services | `ServiceStateTracker`, `WebhookNotificationService` |
| **Hosted** | Background workers | `PingWorker` (via `AddHostedService`) |
| **Transient** | Not used in this project | — |
| **Scoped** | Not used in this project | — |

---

## BackgroundService Pattern

```csharp
/// <summary>
/// Health-checks or processes items on a periodic schedule.
/// Yields control between ticks via async/await and PeriodicTimer.
/// Best-effort error handling ensures the loop never crashes.
/// Reads fresh configuration each tick via IOptionsMonitor.
/// 8-second initial delay allows the host to fully initialize.
/// </summary>
public sealed class MyWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MyWorker starting");

        var initialDelay = _options.CurrentValue.InitialDelaySeconds;
        await Task.Delay(TimeSpan.FromSeconds(initialDelay), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Work
        }

        _logger.LogInformation("MyWorker stopping");
    }
}
```

Rules:
- Use `PeriodicTimer` for recurring work
- Log lifecycle events at `Information` level
- Log errors at `Error` level with exception
- Never let exceptions crash the loop

---

## Error Handling Rules

1. Individual endpoint failures never stop the loop
2. Webhook delivery failures are logged but never thrown
3. Distinguish `TaskCanceledException` from timeout vs shutdown:
   ```csharp
   catch (TaskCanceledException) when (!ct.IsCancellationRequested)
   {
       // Timeout — treat as failure
   }
   ```
4. Unexpected exceptions: catch, log error, continue

---

## HttpClient Rules

- Always use `IHttpClientFactory.CreateClient("name")` — never `new HttpClient()`
- Named clients: `"Ping"` for endpoint checks, `"Webhook"` for notifications
- Set timeout per-request via `client.Timeout`, not globally

---

## Notification Service Pattern

```csharp
/// <summary>
/// Transmits notifications via the configured delivery channel.
/// Returns silently when the channel is not configured.
/// All delivery failures are logged but never thrown.
/// Notification payloads include a source identifier for tracing.
/// </summary>
public sealed class MyChannelNotificationService : INotificationService
{
    public async Task NotifyServiceDownAsync(ServiceState state, CancellationToken ct)
    {
        var payload = new WebhookPayload(...);
        await SendAsync(payload, ct);
    }
}
```

Rules:
- Implement `INotificationService`
- Never throw on delivery failure — log and continue
- Skip silently when delivery channel is not configured
- Include source identifier in payloads for tracing

---

## HYBR8 Pattern

The HYBR8 pattern defines the five-phase operational cycle used by PingKeeper's workers:

| Phase | Meaning | Implementation |
|-------|---------|----------------|
| **H**eartbeat | Send health-check requests | `PingEndpointAsync` |
| **Y**ield | Async wait between cycles | `PeriodicTimer.WaitForNextTickAsync` |
| **B**ackoff | Count failures against threshold | `ServiceState.RecordFailure` |
| **R**ecovery | Detect and notify when service recovers | `ServiceState.RecordSuccess` |
| **8**-second grace | Initial delay before first tick | `Task.Delay(InitialDelaySeconds)` |

All background workers should follow this pattern. The 8-second grace period gives the host time to complete DNS resolution, container networking, and service registration before the first health-check fires.

See `architecture/overview.md` and `flow/ping-loop.md` for cross-references.

---

## General Rules

- All times are **UTC**
- File-scoped namespaces (`namespace PingKeeper.Models;`)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- `required` keyword for mandatory config properties
- Records for immutable DTOs, classes for mutable state
