using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Infrastructure;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.API.Application.Queries;

public class GetLeadByIdentifierQueryHandler : IRequestHandler<GetLeadByIdentifierQuery, Result<LeadDto>>
{
    private readonly LeadTrackerContext _context;

    public GetLeadByIdentifierQueryHandler(LeadTrackerContext context) => _context = context;

    public async Task<Result<LeadDto>> Handle(GetLeadByIdentifierQuery request, CancellationToken cancellationToken)
    {
        var lead = await _context.Leads
            .AsNoTracking()
            .Include(l => l.ConversionEvents)
            .FirstOrDefaultAsync(
                l => l.Identifier.Value == request.IdentifierValue &&
                     l.Identifier.Type  == request.IdentifierType,
                cancellationToken);

        if (lead is null)
            return Result.Fail<LeadDto>(
                $"Lead com identificador '{request.IdentifierValue}' ({request.IdentifierType}) não encontrado.");

        return Result.Ok(LeadMapper.ToDto(lead));
    }
}
