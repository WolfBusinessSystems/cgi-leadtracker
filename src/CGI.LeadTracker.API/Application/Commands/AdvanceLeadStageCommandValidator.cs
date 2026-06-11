using FluentValidation;

namespace CGI.LeadTracker.API.Application.Commands;

public class AdvanceLeadStageCommandValidator : AbstractValidator<AdvanceLeadStageCommand>
{
    public AdvanceLeadStageCommandValidator()
    {
        RuleFor(c => c.LeadId)
            .NotEmpty().WithMessage("O ID do lead é obrigatório.");

        RuleFor(c => c.NewStage)
            .IsInEnum().WithMessage("Etapa do funil inválida.");
    }
}
