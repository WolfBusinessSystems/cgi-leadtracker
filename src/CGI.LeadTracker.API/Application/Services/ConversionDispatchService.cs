using System.Security.Cryptography;
using System.Text;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.Services;

namespace CGI.LeadTracker.API.Application.Services;

public class ConversionDispatchService : IConversionDispatchService
{
    private readonly IMetaConversionsService _meta;
    private readonly IGoogleAdsService _google;
    private readonly ILogger<ConversionDispatchService> _logger;

    public ConversionDispatchService(
        IMetaConversionsService meta,
        IGoogleAdsService google,
        ILogger<ConversionDispatchService> logger)
    {
        _meta   = meta;
        _google = google;
        _logger = logger;
    }

    public async Task DispatchAsync(Lead lead, FunnelStage stage, CancellationToken cancellationToken = default)
    {
        if (!FunnelEventMapper.ShouldSendEvent(stage)) return;

        switch (lead.Identifier.Type)
        {
            case IdentifierType.Fbclid:
                await TrySendMetaAsync(lead, stage, cancellationToken);
                break;
            case IdentifierType.Gclid:
                await TrySendGoogleAsync(lead, stage, cancellationToken);
                break;
            default:
                _logger.LogWarning(
                    "Lead {Id} com identificador '{Type}' não suportado para conversão.",
                    lead.Id, lead.Identifier.Type);
                break;
        }
    }

    private async Task TrySendMetaAsync(Lead lead, FunnelStage stage, CancellationToken ct)
    {
        if (!lead.NeedsConversionEvent(stage, AdPlatform.Meta)) return;

        var eventName = FunnelEventMapper.GetMetaEventName(stage);
        if (eventName is null) return;

        var convEvent = ConversionEvent.Create(lead.Id, AdPlatform.Meta, eventName, stage, lead.ContractValue);
        lead.RegisterConversionEvent(convEvent);

        var pd = lead.PersonalData;

        // fbc liga o evento ao clique no anúncio — formato fb.1.{epoch_ms}.{fbclid}
        var fbc = $"fb.1.{new DateTimeOffset(lead.CreatedAt, TimeSpan.Zero).ToUnixTimeMilliseconds()}.{lead.Identifier.NormalizedValue}";

        var eventData = new MetaEventData(
            EventName:   eventName,
            EventTime:   DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            EventId:     convEvent.Id.ToString(),
            Fbc:         fbc,
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
