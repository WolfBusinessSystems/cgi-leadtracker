using CGI.LeadTracker.Domain.AggregatesModel.Lead;

namespace CGI.LeadTracker.API.Application.Adapters;

// Mapeamento manual domínio -> DTO. Substitui o AutoMapper: os DTOs são records
// posicionais e os campos vêm de objetos aninhados (Identifier, PersonalData),
// o que torna o mapeamento explícito mais simples e seguro que a configuração por convenção.
public static class LeadMapper
{
    public static LeadDto ToDto(Lead lead) => new(
        Id:               lead.Id,
        RdStationId:      lead.RdStationId,
        IdentifierType:   lead.Identifier.Type.ToString(),
        IdentifierValue:  lead.Identifier.Value,
        Name:             lead.PersonalData.Name,
        Email:            lead.PersonalData.Email,
        Phone:            lead.PersonalData.Phone,
        Cpf:              lead.PersonalData.Cpf,
        ContractValue:    lead.ContractValue,
        CurrentStage:     lead.CurrentStage.ToString(),
        CreatedAt:        lead.CreatedAt,
        UpdatedAt:        lead.UpdatedAt,
        ConversionEvents: lead.ConversionEvents.Select(ToDto).ToList());

    public static ConversionEventDto ToDto(ConversionEvent e) => new(
        Id:              e.Id,
        Platform:        e.Platform.ToString(),
        EventName:       e.EventName,
        Stage:           e.Stage.ToString(),
        ContractValue:   e.ContractValue,
        CreatedAt:       e.CreatedAt,
        Status:          e.Status.ToString(),
        ExternalEventId: e.ExternalEventId);
}
