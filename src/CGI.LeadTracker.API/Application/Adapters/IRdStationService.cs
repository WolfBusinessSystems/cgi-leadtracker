using CGI.LeadTracker.Domain.AggregatesModel.Lead;

namespace CGI.LeadTracker.API.Application.Adapters;

public record RdStationLeadData(
    string RdStationId,
    string? Gclid,
    string? Fbclid,
    string Name,
    string Email,
    string? Phone,
    string? Cpf,
    FunnelStage Stage,
    decimal? DealValue,
    DateTime UpdatedAt);

public interface IRdStationService
{
    Task<IReadOnlyList<RdStationLeadData>> GetLeadsUpdatedSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default);

    Task<RdStationLeadData?> GetLeadByIdAsync(
        string rdStationId,
        CancellationToken cancellationToken = default);
}
