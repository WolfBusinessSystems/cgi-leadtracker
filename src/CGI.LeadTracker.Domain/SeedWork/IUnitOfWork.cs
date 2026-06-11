namespace CGI.LeadTracker.Domain.SeedWork;

public interface IUnitOfWork
{
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
