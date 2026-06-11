namespace CGI.LeadTracker.API.Application.Adapters;

public interface IIdentityService
{
    Guid GetUserId();
    string GetName();
    string GetEmail();
    string GetRole();
}
