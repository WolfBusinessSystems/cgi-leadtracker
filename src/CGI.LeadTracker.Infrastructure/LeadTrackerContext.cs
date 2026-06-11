using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Domain.SeedWork;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.Infrastructure;

public class LeadTrackerContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<ConversionEvent> ConversionEvents => Set<ConversionEvent>();
    public DbSet<User> Users => Set<User>();

    public LeadTrackerContext(DbContextOptions<LeadTrackerContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Coleta todas as entidades com eventos pendentes
        var entitiesWithEvents = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        // Extrai os eventos e limpa das entidades antes de salvar
        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        // Publica os eventos (handlers podem modificar estado — tudo salvo junto)
        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeadTrackerContext).Assembly);
    }
}
