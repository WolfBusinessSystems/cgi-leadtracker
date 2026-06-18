using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly LeadTrackerContext _context;

    public UserRepository(LeadTrackerContext context) => _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    // Rastreado (sem AsNoTracking) — usado para atualizações como troca de senha.
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);
}
