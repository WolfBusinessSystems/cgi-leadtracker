using FluentResults;

namespace CGI.LeadTracker.API.Application.Adapters;

public record MetaEventData(
    string EventName,
    long EventTime,
    string EventId,
    string HashedEmail,
    string? HashedPhone,
    string? HashedName,
    string? HashedCpf,
    decimal? Value,
    string? Currency);

public interface IMetaConversionsService
{
    Task<Result<string>> SendEventAsync(
        MetaEventData eventData,
        CancellationToken cancellationToken = default);
}
