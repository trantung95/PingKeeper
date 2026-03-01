# Architecture Overview

## High-Level Summary

PingKeeper is a .NET 8 web worker that periodically sends HTTP GET requests to configured URLs (including its own global URL) to keep cloud instances alive. When a service goes down (consecutive failures exceed a threshold), it sends webhook notifications. When a service recovers, it optionally notifies as well.

## Architecture

```
Program.cs (composition root)
    |
    |-- WebApplication (Kestrel)
    |       |
    |       +-- GET /ping  (minimal API health endpoint)
    |
    +-- PingWorker (BackgroundService)
            |
            |-- IHttpClientFactory ("Ping" client)
            |-- ServiceStateTracker (singleton state dictionary)
            +-- INotificationService
                    |
                    +-- WebhookNotificationService
                            |
                            +-- IHttpClientFactory ("Webhook" client)
```

## Key Design Decisions

### 1. `Microsoft.NET.Sdk.Web` Instead of `Sdk.Worker`

The app needs a minimal HTTP endpoint (`/ping`) so it can ping itself by its own global URL. This proves to the server manager that the instance is alive and has internet connectivity. `Sdk.Web` provides Kestrel + hosting + all worker capabilities.

### 2. PeriodicTimer Instead of Hangfire

`PeriodicTimer` (built into .NET) is the right tool for a simple recurring task. Hangfire adds a storage backend dependency, dashboard, and complexity that is unnecessary for pinging URLs every 60 seconds.

### 3. IHttpClientFactory with Named Clients

Two named clients prevent socket exhaustion and separate concerns:
- `"Ping"` -- for health-checking endpoints
- `"Webhook"` -- for sending notifications

Never use `new HttpClient()` directly.

### 4. State Machine for Failure Detection

`ServiceState` per endpoint tracks consecutive failures:
- **Up → Down**: fires after `ConsecutiveFailureThreshold` consecutive failures (default: 3)
- **Down → Up**: fires on first success after being down

Notifications fire only on transitions — no repeated alerts.

### 5. IOptionsMonitor for Hot-Reload

`IOptionsMonitor<T>` reads the current config value on each tick. Endpoints can be added/removed by editing `appsettings.json` without restarting the service.

### 6. Best-Effort Notifications

Webhook delivery failures are logged but never thrown. The notification system must never crash the ping loop.

## Technology Stack

| Component     | Technology                           |
|---------------|--------------------------------------|
| Runtime       | .NET 8                               |
| Web framework | ASP.NET Core Minimal API             |
| Scheduling    | PeriodicTimer + BackgroundService    |
| HTTP clients  | IHttpClientFactory (named)           |
| Logging       | Microsoft.Extensions.Logging         |
| Configuration | IOptionsMonitor<T> + appsettings.json|
| Serialization | System.Text.Json                     |
| Container     | Docker (aspnet:8.0)                  |

## NuGet Packages

**Zero additional packages.** Everything is provided by `Microsoft.NET.Sdk.Web`:
- `Microsoft.Extensions.Hosting` (BackgroundService, IHost)
- `Microsoft.Extensions.Http` (IHttpClientFactory)
- `Microsoft.Extensions.Logging` (ILogger)
- `Microsoft.Extensions.Options` (IOptionsMonitor)
- `System.Text.Json` (JSON serialization)
