using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.Events;
using MediatR;

namespace CGI.LeadTracker.API.Application.Events;

// Quando o valor do contrato muda, reenvia o evento ContractClosed para refletir
// o novo valor nas plataformas. O domínio já garante (NeedsConversionEvent) que
// só reenvia se o valor mudou desde o último envio.
public class ContractValueUpdatedDomainEventHandler : INotificationHandler<ContractValueUpdatedDomainEvent>
{
    private readonly IConversionDispatchService _dispatcher;
    private readonly ILogger<ContractValueUpdatedDomainEventHandler> _logger;

    public ContractValueUpdatedDomainEventHandler(
        IConversionDispatchService dispatcher,
        ILogger<ContractValueUpdatedDomainEventHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(ContractValueUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Valor de contrato atualizado: Lead={Id}, De={Old}, Para={New}.",
            notification.Lead.Id, notification.PreviousValue, notification.NewValue);

        await _dispatcher.DispatchAsync(notification.Lead, FunnelStage.ContractClosed, cancellationToken);
    }
}
