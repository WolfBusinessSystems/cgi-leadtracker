namespace CGI.LeadTracker.Domain.SeedWork;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    private List<IDomainEvent>? _domainEvents;

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents?.AsReadOnly() ?? (IReadOnlyCollection<IDomainEvent>)Array.Empty<IDomainEvent>();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= [];
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents?.Remove(domainEvent);

    public void ClearDomainEvents() =>
        _domainEvents?.Clear();
}
