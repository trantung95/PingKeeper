# .adn - Project DNA

This folder contains the complete documentation ("DNA") of the **PingKeeper** project -- a service that keeps cloud instances alive by periodically pinging URLs, built on .NET 8.

## Purpose

The `.adn/` folder allows any AI assistant (or new developer) to fully understand, maintain, and extend this project without reading every source file. It captures architecture decisions, configuration, data flows, and operational procedures.

## How to Use

1. **Start here** -- read this file, then `architecture/overview.md` for the big picture.
2. **Understand the ping loop** -- read `flow/ping-loop.md`.
3. **Configure** -- see `configuration/config-reference.md` for all options.
4. **Deploy** -- see `operations/deployment.md` for Docker and systemd setup.
5. **Extend the project** -- read `growth/coding-conventions.md` for patterns.
6. **After making changes** -- follow `growth/self-maintenance.md` verification checklist.

## Folder Map

```
.adn/
  README.md                          # This file
  architecture/
    overview.md                      # System architecture, tech stack, key decisions
  configuration/
    config-reference.md              # All appsettings.json options
  flow/
    ping-loop.md                     # Ping cycle data flow, state machine
  operations/
    deployment.md                    # Docker, systemd, environment variables
  testing/
    testing-strategy.md              # Test categories, patterns, coverage
  growth/
    coding-conventions.md            # Naming, patterns, DI lifetimes
    self-maintenance.md              # Post-change verification checklist
```

## Conventions

- All times are **UTC**.
- Config section names match C# class `SectionName` constants.
- Zero external NuGet packages for main project — everything from the SDK.
- Test project uses xUnit + FluentAssertions + Moq + Testcontainers.
