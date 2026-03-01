using Microsoft.Extensions.Options;
using PingKeeper.Models;

namespace PingKeeper.Services;

public sealed class PingWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<PingKeeperConfig> _options;
    private readonly ServiceStateTracker _stateTracker;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PingWorker> _logger;

    public PingWorker(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<PingKeeperConfig> options,
        ServiceStateTracker stateTracker,
        INotificationService notificationService,
        ILogger<PingWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _stateTracker = stateTracker;
        _notificationService = notificationService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PingKeeper worker starting");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var intervalSeconds = _options.CurrentValue.IntervalSeconds;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        await PingAllEndpointsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PingAllEndpointsAsync(stoppingToken);
        }

        _logger.LogInformation("PingKeeper worker stopping");
    }

    internal async Task PingAllEndpointsAsync(CancellationToken ct)
    {
        var options = _options.CurrentValue;
        var endpoints = options.Endpoints;

        if (endpoints.Count == 0)
        {
            _logger.LogWarning("No endpoints configured");
            return;
        }

        _logger.LogDebug("Pinging {Count} endpoint(s)", endpoints.Count);

        foreach (var endpoint in endpoints)
        {
            await PingEndpointAsync(endpoint, options, ct);
        }
    }

    private async Task PingEndpointAsync(ServiceEndpoint endpoint, PingKeeperConfig options, CancellationToken ct)
    {
        var state = _stateTracker.GetOrCreate(endpoint);
        var timeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds ?? options.TimeoutSeconds);

        try
        {
            var client = _httpClientFactory.CreateClient("Ping");
            client.Timeout = timeout;

            using var response = await client.GetAsync(endpoint.Url, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("{Name} ({Url}): OK ({StatusCode})", endpoint.Name, endpoint.Url, (int)response.StatusCode);

                if (state.RecordSuccess())
                {
                    _logger.LogInformation("{Name} has recovered", endpoint.Name);
                    await _notificationService.NotifyServiceRecoveredAsync(state, ct);
                }
            }
            else
            {
                var errorMsg = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                _logger.LogWarning("{Name} ({Url}): {Error}", endpoint.Name, endpoint.Url, errorMsg);

                if (state.RecordFailure(errorMsg, options.ConsecutiveFailureThreshold))
                {
                    _logger.LogError("{Name} is DOWN after {Count} consecutive failures", endpoint.Name, state.ConsecutiveFailures);
                    await _notificationService.NotifyServiceDownAsync(state, ct);
                }
            }
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            var errorMsg = $"Request timed out after {timeout.TotalSeconds}s";
            _logger.LogWarning("{Name} ({Url}): {Error}", endpoint.Name, endpoint.Url, errorMsg);

            if (state.RecordFailure(errorMsg, options.ConsecutiveFailureThreshold))
            {
                _logger.LogError("{Name} is DOWN after {Count} consecutive failures", endpoint.Name, state.ConsecutiveFailures);
                await _notificationService.NotifyServiceDownAsync(state, ct);
            }
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"Connection error: {ex.Message}";
            _logger.LogWarning("{Name} ({Url}): {Error}", endpoint.Name, endpoint.Url, errorMsg);

            if (state.RecordFailure(errorMsg, options.ConsecutiveFailureThreshold))
            {
                _logger.LogError("{Name} is DOWN after {Count} consecutive failures", endpoint.Name, state.ConsecutiveFailures);
                await _notificationService.NotifyServiceDownAsync(state, ct);
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Unexpected error pinging {Name} ({Url})", endpoint.Name, endpoint.Url);
            state.RecordFailure(ex.Message, options.ConsecutiveFailureThreshold);
        }
    }
}
