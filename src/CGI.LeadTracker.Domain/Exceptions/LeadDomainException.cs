namespace CGI.LeadTracker.Domain.Exceptions;

public class LeadDomainException : Exception
{
    public LeadDomainException(string message) : base(message) { }
    public LeadDomainException(string message, Exception innerException) : base(message, innerException) { }
}
