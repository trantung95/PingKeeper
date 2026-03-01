# Self-Maintenance Guide

Rules and checklists for keeping PingKeeper consistent and healthy as it grows.

---

## Post-Change Verification Checklist

Run after **every** code change:

- [ ] Test suite passes (`dotnet test PingKeeper.sln`)
- [ ] Unbroken build (`dotnet build PingKeeper.sln` — 0 errors, 0 warnings)
- [ ] New public methods have corresponding tests
- [ ] Get `/ping` endpoint returns OK (`dotnet run` then `curl localhost:8080/ping`)
- [ ] Tick cycle runs and logs results for configured endpoints
- [ ] Review `.adn/` docs for accuracy (see mapping in `CLAUDE.md`)
- [ ] All config options documented in `configuration/config-reference.md`
- [ ] Naming follows conventions in `growth/coding-conventions.md`

---

## Consistency Rules

### 1. Config Parity

For every `*Config` class:
- There's a `const SectionName` matching an `appsettings.json` key
- There's a `services.Configure<T>()` call in `Program.cs`
- There's a section in `.adn/configuration/config-reference.md`

### 2. DI Registration Completeness

Every service class must be registered in `Program.cs`. If a class exists but isn't registered, it's dead code — either register it or delete it.

### 3. HttpClient Naming

All HTTP calls must use `IHttpClientFactory` with a named client. Named clients: `"Ping"`, `"Webhook"`. New HTTP concerns should add new named clients.

---

## When Renaming Things

### Renaming a Config Key
1. Config POCO property
2. `appsettings.json` key
3. Any service that reads the config
4. `.adn/configuration/config-reference.md`
5. `CLAUDE.md` if it references the key

### Renaming a Service
1. Class file and class name
2. DI registration in `Program.cs`
3. Any class that injects it
4. `.adn/architecture/overview.md`

---

## When Removing Things

### Removing a Service
1. Delete the file
2. Remove DI registration from `Program.cs`
3. Remove from any interface (if it was an interface implementation)
4. Update `.adn/` docs
5. **Verify build compiles**

### Removing a Config Section
1. Remove config POCO
2. Remove `services.Configure<T>()` from `Program.cs`
3. Remove section from `appsettings.json`
4. Remove from `.adn/configuration/config-reference.md`

---

## When Adding Things

### Adding a New Notification Channel
1. Implement `INotificationService`
2. Register in `Program.cs` (or use a composite pattern if multiple channels needed)
3. Add config POCO if the channel has settings
4. Update `.adn/architecture/overview.md` and `.adn/configuration/config-reference.md`

### Adding a New Endpoint Type (e.g., POST with body)
1. Extend `ServiceEndpoint` with new properties
2. Update `PingWorker.PingEndpointAsync` to handle the new type
3. Update `.adn/configuration/config-reference.md` and `.adn/flow/ping-loop.md`

---

## Documentation Self-Check

Before considering any change complete:

1. **`.adn/architecture/overview.md`** — Does the architecture diagram still match?
2. **`.adn/configuration/config-reference.md`** — Do all config options match appsettings.json?
3. **`.adn/flow/ping-loop.md`** — Does the flow diagram still match the code?
4. **`.adn/README.md`** — Does the folder map include any new files?

---

## Preventing Common Mistakes

| Mistake | Prevention |
|---------|-----------|
| Hardcoded config values | Put in `appsettings.json` + Config POCO + DI binding |
| Yielding without cleanup on shutdown | Use `CancellationToken`, dispose `PeriodicTimer` |
| Bare `new HttpClient()` | Always use `IHttpClientFactory.CreateClient("name")` |
| Repeated notifications for same failure | `ServiceState` fires only on transitions |
| 8080 port conflicts in deployment | Set `ASPNETCORE_URLS` to override default port |
| Stale `.adn/` docs | Check `CLAUDE.md` mapping table before considering done |
| Forgotten environment variable docs | Update `.adn/configuration/config-reference.md` |
