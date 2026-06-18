using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Domain.SeedWork;
using CGI.LeadTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CGI.LeadTracker.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não configurada. Defina via variável de ambiente " +
                "ConnectionStrings__DefaultConnection, user-secrets (dev) ou appsettings.Production.json.");

        services.AddDbContext<LeadTrackerContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySql =>
                {
                    mySql.EnableRetryOnFailure(3);
                    mySql.CommandTimeout(30);
                }));

        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LeadTrackerContext>());

        return services;
    }
}
