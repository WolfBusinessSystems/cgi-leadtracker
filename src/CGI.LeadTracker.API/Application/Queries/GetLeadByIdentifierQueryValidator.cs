using FluentValidation;

namespace CGI.LeadTracker.API.Application.Queries;

public class GetLeadByIdentifierQueryValidator : AbstractValidator<GetLeadByIdentifierQuery>
{
    public GetLeadByIdentifierQueryValidator()
    {
        RuleFor(q => q.IdentifierValue)
            .NotEmpty().WithMessage("O identificador não pode ser vazio.")
            .MaximumLength(500).WithMessage("O identificador não pode ultrapassar 500 caracteres.");

        RuleFor(q => q.IdentifierType)
            .IsInEnum().WithMessage("Tipo de identificador inválido. Use 1 (Gclid) ou 2 (Fbclid).");
    }
}
