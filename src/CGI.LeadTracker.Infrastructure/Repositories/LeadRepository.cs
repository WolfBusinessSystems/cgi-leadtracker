using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.Infrastructure.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly LeadTrackerContext _context;

    public LeadRepository(LeadTrackerContext context) =>
        _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Leads
            .Include(l => l.ConversionEvents)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<Lead?> GetByRdStationIdAsync(string rdStationId, CancellationToken cancellationToken = default) =>
        await _context.Leads
            .Include(l => l.ConversionEvents)
            .FirstOrDefaultAsync(l => l.RdStationId == rdStationId, cancellationToken);

    public async Task<Lead?> GetByIdentifierAsync(
        string identifierValue,
        IdentifierType identifierType,
        CancellationToken cancellationToken = default) =>
        await _context.Leads
            .Include(l => l.ConversionEvents)
            .FirstOrDefaultAsync(
                l => l.Identifier.Value == identifierValue && l.Identifier.Type == identifierType,
                cancellationToken);

    public async Task<IReadOnlyList<Lead>> GetLeadsForSyncAsync(CancellationToken cancellationToken = default) =>
        await _context.Leads
            .Include(l => l.ConversionEvents)
            .Where(l => l.CurrentStage != FunnelStage.Disqualified)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Lead lead, CancellationToken cancellationToken = default) =>
        await _context.Leads.AddAsync(lead, cancellationToken);

    public void Update(Lead lead) =>
        _context.Leads.Update(lead);
}
