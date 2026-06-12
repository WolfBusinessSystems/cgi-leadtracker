using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.Events;
using MediatR;

namespace CGI.LeadTracker.API.Application.Events;

public class LeadStageChangedDomainEventHandler : INotificationHandler<LeadStageChangedDomainEvent>
{
    private readonly IConversionDispatchService _dispatcher;

    public LeadStageChangedDomainEventHandler(IConversionDispatchService dispatcher) =>
        _dispatcher = dispatcher;

    public Task Handle(LeadStageChangedDomainEvent notification, CancellationToken cancellationToken) =>
        _dispatcher.DispatchAsync(notification.Lead, notification.NewStage, cancellationToken);
}
