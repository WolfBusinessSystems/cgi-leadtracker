using CGI.LeadTracker.Domain.SeedWork;

namespace CGI.LeadTracker.Domain.AggregatesModel.Lead;

public class ConversionEvent : Entity
{
    public Guid LeadId { get; private set; }
    public AdPlatform Platform { get; private set; }
    public string EventName { get; private set; } = default!;
    public FunnelStage Stage { get; private set; }
    public decimal? ContractValue { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ConversionEventStatus Status { get; private set; }
    public string? ExternalEventId { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ConversionEvent() { }

    public static ConversionEvent Create(
        Guid leadId,
        AdPlatform platform,
        string eventName,
        FunnelStage stage,
        decimal? contractValue = null)
    {
        return new ConversionEvent
        {
            LeadId        = leadId,
            Platform      = platform,
            EventName     = eventName,
            Stage         = stage,
            ContractValue = contractValue,
            Status        = ConversionEventStatus.Pending,
            CreatedAt     = DateTime.UtcNow
        };
    }

    public void MarkAsSent(string externalEventId)
    {
        Status          = ConversionEventStatus.Sent;
        ExternalEventId = externalEventId;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status       = ConversionEventStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
