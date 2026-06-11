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

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);
}
