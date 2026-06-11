namespace CGI.LeadTracker.API.Application.Adapters;

public record LeadDto(
    Guid Id,
    string RdStationId,
    string IdentifierType,
    string IdentifierValue,
    string Name,
    string Email,
    string? Phone,
    string? Cpf,
    decimal? ContractValue,
    string CurrentStage,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ConversionEventDto> ConversionEvents);
