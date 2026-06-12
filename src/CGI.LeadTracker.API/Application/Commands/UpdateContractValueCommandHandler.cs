using AutoMapper;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public class UpdateContractValueCommandHandler : IRequestHandler<UpdateContractValueCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateContractValueCommandHandler(ILeadRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository  = repository;
        _unitOfWork  = unitOfWork;
        _mapper      = mapper;
    }

    public async Task<Result<LeadDto>> Handle(UpdateContractValueCommand request, CancellationToken cancellationToken)
    {
        var lead = await _repository.GetByIdAsync(request.LeadId, cancellationToken);
        if (lead is null)
            return Result.Fail<LeadDto>($"Lead '{request.LeadId}' não encontrado.");

        var result = lead.UpdateContractValue(request.ContractValue);
        if (result.IsFailed)
            return result.ToResult<LeadDto>();

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok(_mapper.Map<LeadDto>(lead));
    }
}
