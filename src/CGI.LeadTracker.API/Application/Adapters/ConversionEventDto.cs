namespace CGI.LeadTracker.API.Application.Adapters;

public record ConversionEventDto(
    Guid Id,
    string Platform,
    string EventName,
    string Stage,
    decimal? ContractValue,
    DateTime CreatedAt,
    string Status,
    string? ExternalEventId);
