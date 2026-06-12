using System.Net.Http.Json;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CGI.LeadTracker.API.Application.Adapters.Meta;

public class MetaConversionsService : IMetaConversionsService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MetaConversionsService> _logger;

    public MetaConversionsService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<MetaConversionsService> logger)
    {
        _http          = http;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<Result<string>> SendEventAsync(
        MetaEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var pixelId     = _configuration["Meta:PixelId"]!;
        var accessToken = _configuration["Meta:AccessToken"]!;

        var payload = new MetaEventsRequest(
            Data: new[]
            {
                new MetaEventPayload(
                    EventName:  eventData.EventName,
                    EventTime:  eventData.EventTime,
                    EventId:    eventData.EventId,
                    UserData:   new MetaUserData(
                        Em:         eventData.HashedEmail is not null ? [eventData.HashedEmail] : null,
                        Ph:         eventData.HashedPhone is not null ? [eventData.HashedPhone] : null,
                        Fn:         eventData.HashedName  is not null ? [eventData.HashedName]  : null,
                        Fbc:        eventData.Fbc,
                        ExternalId: eventData.HashedCpf   is not null ? [eventData.HashedCpf]   : null),
                    CustomData: eventData.Value is not null
                        ? new MetaCustomData(eventData.Value, eventData.Currency ?? "BRL")
                        : null)
            },
            AccessToken: accessToken);

        // URL relativa sem '/' inicial — caminho absoluto descartaria o /vXX.X do BaseAddress
        var response = await _http.PostAsJsonAsync(
            $"{pixelId}/events",
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Meta API erro {Status}: {Error}", response.StatusCode, error);
            return Result.Fail<string>($"Meta API erro {(int)response.StatusCode}: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<MetaEventsResponse>(
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Meta evento '{Event}' enviado. FbTraceId={TraceId}",
            eventData.EventName, result!.FbTraceId);

        return Result.Ok(result.FbTraceId);
    }
}
