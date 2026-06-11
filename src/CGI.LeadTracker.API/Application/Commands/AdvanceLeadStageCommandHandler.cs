using AutoMapper;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public class AdvanceLeadStageCommandHandler : IRequestHandler<AdvanceLeadStageCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AdvanceLeadStageCommandHandler(ILeadRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository  = repository;
        _unitOfWork  = unitOfWork;
        _mapper      = mapper;
    }

    public async Task<Result<LeadDto>> Handle(AdvanceLeadStageCommand request, CancellationToken cancellationToken)
    {
        var lead = await _repository.GetByIdAsync(request.LeadId, cancellationToken);
        if (lead is null)
            return Result.Fail<LeadDto>($"Lead '{request.LeadId}' não encontrado.");

        var result = lead.AdvanceStage(request.NewStage);
        if (result.IsFailed)
            return result.ToResult<LeadDto>();

        _repository.Update(lead);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok(_mapper.Map<LeadDto>(lead));
    }
}
