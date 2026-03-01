# Deployment

## Docker (Recommended)

### Build

```bash
docker build -t pingkeeper .
```

### Run

```bash
docker run -d \
  -p 8080:8080 \
  -e PingKeeper__Endpoints__0__Name=Self \
  -e PingKeeper__Endpoints__0__Url=https://my-instance.example.com/ping \
  -e Webhook__Url=https://hooks.slack.com/services/xxx \
  --name pingkeeper \
  pingkeeper
```

### Docker Compose

```yaml
version: "3.8"
services:
  pingkeeper:
    build: .
    ports:
      - "8080:8080"
    environment:
      - PingKeeper__IntervalSeconds=60
      - PingKeeper__Endpoints__0__Name=Self
      - PingKeeper__Endpoints__0__Url=https://my-instance.example.com/ping
      - Webhook__Url=https://hooks.slack.com/services/xxx
    restart: unless-stopped
```

## Standalone (.NET)

### Build

```bash
dotnet publish -c Release -o ./publish
```

### Run

```bash
cd ./publish
dotnet PingKeeper.dll
```

### Self-Contained (Linux)

```bash
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
chmod +x ./publish/PingKeeper
./publish/PingKeeper
```

## Linux (systemd)

### Create service file

```ini
# /etc/systemd/system/pingkeeper.service
[Unit]
Description=PingKeeper - Keep instances alive
After=network.target

[Service]
Type=notify
ExecStart=/opt/pingkeeper/PingKeeper
WorkingDirectory=/opt/pingkeeper
Restart=always
RestartSec=10
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:8080

[Install]
WantedBy=multi-user.target
```

### Enable and start

```bash
sudo systemctl daemon-reload
sudo systemctl enable pingkeeper
sudo systemctl start pingkeeper
sudo systemctl status pingkeeper
```

### View logs

```bash
sudo journalctl -u pingkeeper -f
```

## Configuration for Production

### Environment Variables

Override any config value using `__` as section separator:

```bash
export PingKeeper__IntervalSeconds=120
export PingKeeper__Endpoints__0__Name=Self
export PingKeeper__Endpoints__0__Url=https://my-app.example.com/ping
export Webhook__Url=https://hooks.slack.com/services/xxx
export ASPNETCORE_URLS=http://+:8080
```

### Port Configuration

ASP.NET Core 8 defaults to port 8080. Override with:

```bash
export ASPNETCORE_URLS=http://+:5000
```

## Firewall

| Port | Direction | Required         |
|------|-----------|------------------|
| 8080 | Inbound  | `/ping` endpoint |
| 443  | Outbound | HTTPS pings      |
| 443  | Outbound | Webhook delivery |

## Health Verification

After deployment:
1. `curl http://localhost:8080/ping` — should return `"OK"`
2. Check logs for "PingKeeper worker starting"
3. Wait one interval, check logs for ping results
4. Configure a non-existent URL, verify failure detection after 3 attempts
