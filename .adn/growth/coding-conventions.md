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
public sealed class MyWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MyWorker starting");

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

## General Rules

- All times are **UTC**
- File-scoped namespaces (`namespace PingKeeper.Models;`)
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- `required` keyword for mandatory config properties
- Records for immutable DTOs, classes for mutable state
