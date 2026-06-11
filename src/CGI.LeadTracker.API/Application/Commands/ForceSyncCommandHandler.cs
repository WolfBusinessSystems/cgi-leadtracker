using CGI.LeadTracker.API.Application.Adapters;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public class ForceSyncCommandHandler : IRequestHandler<ForceSyncCommand, Result>
{
    private readonly ILeadSyncService _syncService;
    private readonly ILogger<ForceSyncCommandHandler> _logger;

    public ForceSyncCommandHandler(ILeadSyncService syncService, ILogger<ForceSyncCommandHandler> logger)
    {
        _syncService = syncService;
        _logger      = logger;
    }

    public async Task<Result> Handle(ForceSyncCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reprocessamento manual solicitado.");
        var result = await _syncService.SyncAsync(cancellationToken);

        if (result.IsFailed)
            _logger.LogWarning("Reprocessamento concluído com falhas: {Errors}", result.Errors);

        return result;
    }
}
