# Configuration Reference

All configuration lives in `appsettings.json`. Use environment variables for production overrides (double-underscore `__` as separator).

## PingKeeper

Section: `"PingKeeper"` / Class: `PingKeeperConfig`

| Key                          | Type                 | Default | Description                                    |
|------------------------------|----------------------|---------|------------------------------------------------|
| IntervalSeconds              | int                  | 60      | Seconds between ping cycles                    |
| TimeoutSeconds               | int                  | 10      | Default HTTP request timeout per ping          |
| ConsecutiveFailureThreshold  | int                  | 3       | Failures before declaring a service down       |
| Endpoints                    | ServiceEndpoint[]    | []      | List of endpoints to monitor                   |

### ServiceEndpoint

| Key            | Type    | Default | Description                                         |
|----------------|---------|---------|-----------------------------------------------------|
| Name           | string  | —       | **Required.** Friendly name for logging/notifications|
| Url            | string  | —       | **Required.** URL to send HTTP GET to                |
| TimeoutSeconds | int?    | null    | Per-endpoint timeout override (uses global if null)  |

### Self-Ping

Add the app's own global URL as an endpoint to keep the instance alive:

```json
{
  "Name": "Self",
  "Url": "https://my-instance.example.com/ping"
}
```

## Webhook

Section: `"Webhook"` / Class: `WebhookConfig`

| Key              | Type   | Default | Description                                       |
|------------------|--------|---------|---------------------------------------------------|
| Url              | string | ""      | Webhook URL for notifications. Empty = disabled.  |
| TimeoutSeconds   | int    | 15      | HTTP timeout for webhook POST requests            |
| NotifyOnRecovery | bool   | true    | Send notification when a service recovers         |

### Webhook Payload

When a service goes down or recovers, a JSON POST is sent:

```json
{
  "serviceName": "My API",
  "serviceUrl": "https://api.example.com/health",
  "status": "Down",
  "errorMessage": "HTTP 503 Service Unavailable",
  "consecutiveFailures": 3,
  "timestamp": "2024-01-15T10:30:00+00:00"
}
```

`status` is either `"Down"` or `"Recovered"`.

## Logging

Section: `"Logging"` — Standard ASP.NET Core logging configuration.

| Key                            | Type   | Default       | Description              |
|--------------------------------|--------|---------------|--------------------------|
| LogLevel:Default               | string | "Information" | Global minimum log level |
| LogLevel:PingKeeper            | string | "Debug"       | PingKeeper namespace     |
| LogLevel:Microsoft.Hosting.Lifetime | string | "Information" | Host lifecycle events |

## Environment Variable Overrides

Use double-underscore `__` as section separator:

```bash
export PingKeeper__IntervalSeconds=120
export PingKeeper__Endpoints__0__Name=Self
export PingKeeper__Endpoints__0__Url=https://my-app.example.com/ping
export Webhook__Url=https://hooks.slack.com/services/xxx
```
