namespace CGI.LeadTracker.API.Application.Adapters;

public record TokenDto(
    string Token,
    string Name,
    string Email,
    string Role,
    DateTime ExpiresAt);
