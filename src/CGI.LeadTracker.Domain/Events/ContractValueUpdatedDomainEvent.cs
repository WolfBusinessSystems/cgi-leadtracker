using CGI.LeadTracker.Domain.SeedWork;

namespace CGI.LeadTracker.Domain.Events;

public record ContractValueUpdatedDomainEvent(
    Guid LeadId,
    decimal? PreviousValue,
    decimal NewValue) : IDomainEvent;
