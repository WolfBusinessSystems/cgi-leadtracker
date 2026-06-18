using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Infrastructure;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.API.Application.Queries;

public class GetLeadByIdQueryHandler : IRequestHandler<GetLeadByIdQuery, Result<LeadDto>>
{
    private readonly LeadTrackerContext _context;

    public GetLeadByIdQueryHandler(LeadTrackerContext context) => _context = context;

    public async Task<Result<LeadDto>> Handle(GetLeadByIdQuery request, CancellationToken cancellationToken)
    {
        var lead = await _context.Leads
            .AsNoTracking()
            .Include(l => l.ConversionEvents)
            .FirstOrDefaultAsync(l => l.Id == request.LeadId, cancellationToken);

        if (lead is null)
            return Result.Fail<LeadDto>($"Lead '{request.LeadId}' não encontrado.");

        return Result.Ok(LeadMapper.ToDto(lead));
    }
}
