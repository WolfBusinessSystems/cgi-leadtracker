using AutoMapper;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Infrastructure;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.API.Application.Queries;

public class GetLeadByIdQueryHandler : IRequestHandler<GetLeadByIdQuery, Result<LeadDto>>
{
    private readonly LeadTrackerContext _context;
    private readonly IMapper _mapper;

    public GetLeadByIdQueryHandler(LeadTrackerContext context, IMapper mapper)
    {
        _context = context;
        _mapper  = mapper;
    }

    public async Task<Result<LeadDto>> Handle(GetLeadByIdQuery request, CancellationToken cancellationToken)
    {
        var lead = await _context.Leads
            .AsNoTracking()
            .Include(l => l.ConversionEvents)
            .FirstOrDefaultAsync(l => l.Id == request.LeadId, cancellationToken);

        if (lead is null)
            return Result.Fail<LeadDto>($"Lead '{request.LeadId}' não encontrado.");

        return Result.Ok(_mapper.Map<LeadDto>(lead));
    }
}
