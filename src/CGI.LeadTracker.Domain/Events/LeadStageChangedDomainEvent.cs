using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;

namespace CGI.LeadTracker.Domain.Events;

public record LeadStageChangedDomainEvent(
    Guid LeadId,
    FunnelStage? PreviousStage,
    FunnelStage NewStage) : IDomainEvent;
