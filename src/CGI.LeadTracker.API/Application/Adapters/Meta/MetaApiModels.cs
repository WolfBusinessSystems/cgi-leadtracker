using System.Text.Json.Serialization;

namespace CGI.LeadTracker.API.Application.Adapters.Meta;

// Modelos que espelham o JSON da Meta Conversions API v19.0

public record MetaEventsRequest(
    [property: JsonPropertyName("data")] IReadOnlyList<MetaEventPayload> Data,
    [property: JsonPropertyName("access_token")] string AccessToken);

public record MetaEventPayload(
    [property: JsonPropertyName("event_name")]  string EventName,
    [property: JsonPropertyName("event_time")]  long EventTime,
    [property: JsonPropertyName("event_id")]    string EventId,
    [property: JsonPropertyName("user_data")]   MetaUserData UserData,
    [property: JsonPropertyName("custom_data")] MetaCustomData? CustomData);

public record MetaUserData(
    [property: JsonPropertyName("em")] IReadOnlyList<string>? Em,
    [property: JsonPropertyName("ph")] IReadOnlyList<string>? Ph,
    [property: JsonPropertyName("fn")] IReadOnlyList<string>? Fn);

public record MetaCustomData(
    [property: JsonPropertyName("value")]    decimal? Value,
    [property: JsonPropertyName("currency")] string? Currency);

public record MetaEventsResponse(
    [property: JsonPropertyName("events_received")] int EventsReceived,
    [property: JsonPropertyName("fbtrace_id")]      string FbTraceId);
