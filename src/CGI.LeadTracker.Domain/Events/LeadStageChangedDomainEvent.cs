using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;

namespace CGI.LeadTracker.Domain.Events;

// Carrega a própria entidade (e não só o Id) porque o evento é publicado
// antes do SaveChanges — um lead recém-criado ainda não existe no banco.
public record LeadStageChangedDomainEvent(
    Lead Lead,
    FunnelStage? PreviousStage,
    FunnelStage NewStage) : IDomainEvent;
