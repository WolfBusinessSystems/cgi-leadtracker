using CGI.LeadTracker.API.Application.Adapters;

namespace CGI.LeadTracker.API.Application.Services;

public class LeadSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly ILogger<LeadSyncBackgroundService> _logger;

    public LeadSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<LeadSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;

        var minutes = configuration.GetValue("LeadSyncSettings:IntervalMinutes", 60);
        _interval   = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "LeadSync background service iniciado. Intervalo: {Interval} min.", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ILeadSyncService>();
            var result = await syncService.SyncAsync(ct);

            if (result.IsFailed)
                _logger.LogWarning("Sync com erros: {Errors}", string.Join(" | ", result.Errors.Select(e => e.Message)));
        }
        catch (OperationCanceledException)
        {
            // shutdown normal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha inesperada no LeadSync background service.");
        }
    }
}
