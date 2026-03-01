using PingKeeper.Models;
using PingKeeper.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PingKeeperConfig>(
    builder.Configuration.GetSection(PingKeeperConfig.SectionName));
builder.Services.Configure<WebhookConfig>(
    builder.Configuration.GetSection(WebhookConfig.SectionName));

builder.Services.AddSingleton<ServiceStateTracker>();
builder.Services.AddSingleton<INotificationService, WebhookNotificationService>();

builder.Services.AddHttpClient("Ping");
builder.Services.AddHttpClient("Webhook");

builder.Services.AddHostedService<PingWorker>();

var app = builder.Build();

app.MapGet("/ping", () => Results.Ok("OK"));

app.Run();
