namespace CGI.LeadTracker.Domain.AggregatesModel.Lead;

public record PersonalData(
    string Name,
    string Email,
    string? Phone,
    string? Cpf)
{
    // Normalização para hash SHA-256 conforme exigido por Meta e Google
    public string NormalizedName  => Name?.Trim().ToLowerInvariant()  ?? string.Empty;
    public string NormalizedEmail => Email?.Trim().ToLowerInvariant() ?? string.Empty;

    public string NormalizedPhone => Phone is null ? string.Empty :
        new string(Phone.Where(char.IsDigit).ToArray());

    public string NormalizedCpf => Cpf is null ? string.Empty :
        new string(Cpf.Where(char.IsDigit).ToArray());
}
