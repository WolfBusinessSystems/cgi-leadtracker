using FluentValidation;

namespace CGI.LeadTracker.API.Application.Commands;

public class ForceSyncCommandValidator : AbstractValidator<ForceSyncCommand>
{
    public ForceSyncCommandValidator()
    {
        // Comando sem parâmetros — sem regras de validação de entrada
    }
}
