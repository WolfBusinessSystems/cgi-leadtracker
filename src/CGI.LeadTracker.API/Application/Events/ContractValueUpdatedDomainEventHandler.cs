using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CGI.LeadTracker.API.Application.Events;

// Quando o valor do contrato muda, dispara re-envio do evento ContractClosed
// para refletir o novo valor nas plataformas de anúncios.
// O domínio já garante (NeedsConversionEvent) que só reenvia se o valor mudou.
public class ContractValueUpdatedDomainEventHandler : INotificationHandler<ContractValueUpdatedDomainEvent>
{
    private readonly ILeadRepository _repository;
    private readonly IMediator _mediator;
    private readonly ILogger<ContractValueUpdatedDomainEventHandler> _logger;

    public ContractValueUpdatedDomainEventHandler(
        ILeadRepository repository,
        IMediator mediator,
        ILogger<ContractValueUpdatedDomainEventHandler> logger)
    {
        _repository = repository;
        _mediator   = mediator;
        _logger     = logger;
    }

    public async Task Handle(ContractValueUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Valor de contrato atualizado: Lead={Id}, De={Old}, Para={New}.",
            notification.LeadId, notification.PreviousValue, notification.NewValue);

        // Reutiliza o handler de mudança de estágio para reenviar o evento ContractClosed
        await _mediator.Publish(
            new LeadStageChangedDomainEvent(notification.LeadId, FunnelStage.ContractClosed, FunnelStage.ContractClosed),
            cancellationToken);
    }
}
