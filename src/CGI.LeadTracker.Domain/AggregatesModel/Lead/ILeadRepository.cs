using CGI.LeadTracker.Domain.SeedWork;

namespace CGI.LeadTracker.Domain.AggregatesModel.Lead;

public interface ILeadRepository : IRepository<Lead>
{
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lead?> GetByRdStationIdAsync(string rdStationId, CancellationToken cancellationToken = default);
    Task<Lead?> GetByIdentifierAsync(string identifierValue, IdentifierType identifierType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lead>> GetLeadsForSyncAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);
    void Update(Lead lead);
}
