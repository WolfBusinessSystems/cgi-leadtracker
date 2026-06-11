using FluentResults;

namespace CGI.LeadTracker.API.Application.Adapters;

public interface ILeadSyncService
{
    Task<Result> SyncAsync(CancellationToken cancellationToken = default);
}
