using AutoMapper;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;

namespace CGI.LeadTracker.API.Application.Adapters;

public class LeadMappingProfile : Profile
{
    public LeadMappingProfile()
    {
        CreateMap<ConversionEvent, ConversionEventDto>()
            .ForMember(d => d.Platform, o => o.MapFrom(s => s.Platform.ToString()))
            .ForMember(d => d.Stage,    o => o.MapFrom(s => s.Stage.ToString()))
            .ForMember(d => d.Status,   o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<Lead, LeadDto>()
            .ForMember(d => d.IdentifierType,  o => o.MapFrom(s => s.Identifier.Type.ToString()))
            .ForMember(d => d.IdentifierValue, o => o.MapFrom(s => s.Identifier.Value))
            .ForMember(d => d.Name,            o => o.MapFrom(s => s.PersonalData.Name))
            .ForMember(d => d.Email,           o => o.MapFrom(s => s.PersonalData.Email))
            .ForMember(d => d.Phone,           o => o.MapFrom(s => s.PersonalData.Phone))
            .ForMember(d => d.Cpf,             o => o.MapFrom(s => s.PersonalData.Cpf))
            .ForMember(d => d.CurrentStage,    o => o.MapFrom(s => s.CurrentStage.ToString()))
            .ForMember(d => d.ConversionEvents, o => o.MapFrom(s => s.ConversionEvents));
    }
}
