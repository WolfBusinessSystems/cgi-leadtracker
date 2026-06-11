namespace CGI.LeadTracker.Domain.AggregatesModel.Lead;

public record LeadIdentifier(IdentifierType Type, string Value)
{
    public string NormalizedValue => Value?.Trim() ?? string.Empty;
}
