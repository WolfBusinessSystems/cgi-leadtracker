using System.Security.Cryptography;
using System.Text;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.Events;
using CGI.LeadTracker.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CGI.LeadTracker.API.Application.Events;

public class LeadStageChangedDomainEventHandler : INotificationHandler<LeadStageChangedDomainEvent>
{
    private readonly ILeadRepository _repository;
    private readonly IMetaConversionsService _meta;
    private readonly IGoogleAdsService _google;
    private readonly ILogger<LeadStageChangedDomainEventHandler> _logger;

    public LeadStageChangedDomainEventHandler(
        ILeadRepository repository,
        IMetaConversionsService meta,
        IGoogleAdsService google,
        ILogger<LeadStageChangedDomainEventHandler> logger)
    {
        _repository = repository;
        _meta       = meta;
        _google     = google;
        _logger     = logger;
    }

    public async Task Handle(LeadStageChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!FunnelEventMapper.ShouldSendEvent(notification.NewStage)) return;

        // Lead está no change tracker (SaveChangesAsync ainda não foi chamado)
        var lead = await _repository.GetByIdAsync(notification.LeadId, cancellationToken);
        if (lead is null)
        {
            _logger.LogWarning("LeadStageChanged: lead {Id} não encontrado.", notification.LeadId);
            return;
        }

        var pd = lead.PersonalData;

        await TrySendMetaAsync(lead, notification.NewStage, pd, cancellationToken);
        await TrySendGoogleAsync(lead, notification.NewStage, cancellationToken);
    }

    private async Task TrySendMetaAsync(
        Lead lead, FunnelStage stage, Domain.AggregatesModel.Lead.PersonalData pd, CancellationToken ct)
    {
        if (lead.Identifier.Type != IdentifierType.Fbclid) return;
        if (!lead.NeedsConversionEvent(stage, AdPlatform.Meta)) return;

        var eventName = FunnelEventMapper.GetMetaEventName(stage);
        if (eventName is null) return;

        var convEvent = ConversionEvent.Create(lead.Id, AdPlatform.Meta, eventName, stage, lead.ContractValue);
        lead.RegisterConversionEvent(convEvent);

        var eventData = new MetaEventData(
            EventName:   eventName,
            EventTime:   DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            EventId:     convEvent.Id.ToString(),
            HashedEmail: Sha256(pd.NormalizedEmail),
            HashedPhone: pd.NormalizedPhone.Length > 0 ? Sha256(pd.NormalizedPhone) : null,
            HashedName:  pd.NormalizedName.Length  > 0 ? Sha256(pd.NormalizedName)  : null,
            HashedCpf:   pd.NormalizedCpf.Length   > 0 ? Sha256(pd.NormalizedCpf)   : null,
            Value:       lead.ContractValue,
            Currency:    "BRL");

        var result = await _meta.SendEventAsync(eventData, ct);

        if (result.IsSuccess)
            convEvent.MarkAsSent(result.Value);
        else
            convEvent.MarkAsFailed(result.Errors.First().Message);
    }

    private async Task TrySendGoogleAsync(Lead lead, FunnelStage stage, CancellationToken ct)
    {
        if (lead.Identifier.Type != IdentifierType.Gclid) return;
        if (!lead.NeedsConversionEvent(stage, AdPlatform.Google)) return;

        var actionName = FunnelEventMapper.GetGoogleConversionName(stage);
        if (actionName is null) return;

        var convEvent = ConversionEvent.Create(lead.Id, AdPlatform.Google, actionName, stage, lead.ContractValue);
        lead.RegisterConversionEvent(convEvent);

        var convData = new GoogleConversionData(
            Gclid:                lead.Identifier.NormalizedValue,
            ConversionActionName: actionName,
            ConversionDateTime:   DateTime.UtcNow,
            ConversionValue:      lead.ContractValue);

        var result = await _google.UploadClickConversionAsync(convData, ct);

        if (result.IsSuccess)
            convEvent.MarkAsSent(result.Value);
        else
            convEvent.MarkAsFailed(result.Errors.First().Message);
    }

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
