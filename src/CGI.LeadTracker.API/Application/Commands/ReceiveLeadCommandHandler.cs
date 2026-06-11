using AutoMapper;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public class ReceiveLeadCommandHandler : IRequestHandler<ReceiveLeadCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReceiveLeadCommandHandler(ILeadRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository  = repository;
        _unitOfWork  = unitOfWork;
        _mapper      = mapper;
    }

    public async Task<Result<LeadDto>> Handle(ReceiveLeadCommand request, CancellationToken cancellationToken)
    {
        var byRdStation = await _repository.GetByRdStationIdAsync(request.RdStationId, cancellationToken);
        if (byRdStation is not null)
            return Result.Fail<LeadDto>($"Lead com RD Station ID '{request.RdStationId}' já existe.");

        var byIdentifier = await _repository.GetByIdentifierAsync(request.IdentifierValue, request.IdentifierType, cancellationToken);
        if (byIdentifier is not null)
            return Result.Fail<LeadDto>($"Lead com identificador '{request.IdentifierValue}' ({request.IdentifierType}) já existe.");

        var identifier   = new LeadIdentifier(request.IdentifierType, request.IdentifierValue);
        var personalData = new PersonalData(request.Name, request.Email, request.Phone, request.Cpf);
        var lead         = Lead.Create(request.RdStationId, identifier, personalData);

        await _repository.AddAsync(lead, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok(_mapper.Map<LeadDto>(lead));
    }
}
