using FluentValidation;

namespace CGI.LeadTracker.API.Application.Queries;

public class GetLeadByIdQueryValidator : AbstractValidator<GetLeadByIdQuery>
{
    public GetLeadByIdQueryValidator()
    {
        RuleFor(q => q.LeadId)
            .NotEmpty().WithMessage("O ID do lead é obrigatório.");
    }
}
