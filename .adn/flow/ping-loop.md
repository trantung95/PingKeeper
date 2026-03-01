# Ping Loop Flow

## Overview

The `PingWorker` (BackgroundService) runs a continuous loop using `PeriodicTimer`. On each tick, it pings all configured endpoints sequentially and tracks their state.

## Startup Sequence

1. Host starts, Kestrel binds to port 8080
2. `PingWorker.ExecuteAsync` begins
3. 5-second initial delay (let host fully start)
4. First ping cycle runs immediately
5. `PeriodicTimer` starts ticking at `IntervalSeconds` intervals

## Per-Tick Flow

```
PeriodicTimer tick
    |
    v
Read IOptionsMonitor<PingKeeperConfig>.CurrentValue
    |
    v
For each endpoint in config.Endpoints:
    |
    +---> Create HttpClient from IHttpClientFactory("Ping")
    |     Set timeout (per-endpoint or global)
    |
    +---> GET endpoint.Url
    |
    +---> Success (2xx)?
    |     |
    |     Yes --> state.RecordSuccess()
    |     |       |
    |     |       +---> Was previously down? (recovery)
    |     |       |     Yes --> Log INFO, NotifyServiceRecoveredAsync()
    |     |       |     No  --> Log DEBUG, continue
    |     |
    |     No --> state.RecordFailure(errorMsg, threshold)
    |            |
    |            +---> Just crossed threshold? (newly down)
    |            |     Yes --> Log ERROR, NotifyServiceDownAsync()
    |            |     No  --> Log WARNING, continue
    |
    +---> Exception? (timeout, connection error, etc.)
          |
          +---> state.RecordFailure(errorMsg, threshold)
                Same transition logic as non-2xx above
```

## State Machine (per endpoint)

```
                 ConsecutiveFailures >= Threshold
    [Up] ─────────────────────────────────────────> [Down]
      ^                                               |
      |              First success                     |
      +────────────────────────────────────────────────+
```

**Initial state**: Up (0 failures, `IsDown = false`)

**Transitions:**
- **Up → Down**: `ConsecutiveFailures` reaches `ConsecutiveFailureThreshold`. Webhook fires once.
- **Down → Up**: Any successful ping while `IsDown == true`. Recovery webhook fires once (if enabled).
- **Steady state**: No notifications. Repeated failures after being declared down produce warnings only. Repeated successes produce debug logs only.

## Error Handling

| Error type | Handling |
|-----------|----------|
| Non-2xx response | Record as failure, log warning |
| `TaskCanceledException` (timeout) | Record as failure, log warning |
| `HttpRequestException` | Record as failure, log warning |
| Unexpected exception | Record as failure, log error with stack trace |
| Shutdown cancellation | Let propagate, exit loop cleanly |
| Webhook delivery failure | Log error, never throw — ping loop continues |

## Key Invariants

- One endpoint failure never stops pinging of other endpoints
- Webhook failures never crash the ping loop
- Notifications fire only on state transitions (up→down, down→up)
- Config changes take effect on the next tick (hot-reload via `IOptionsMonitor`)
