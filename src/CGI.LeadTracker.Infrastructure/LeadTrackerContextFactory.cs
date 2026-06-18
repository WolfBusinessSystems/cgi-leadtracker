using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CGI.LeadTracker.Infrastructure;

// Usado apenas em design-time pelas ferramentas do EF (dotnet ef migrations/database update).
// Localiza o appsettings do projeto API independentemente do diretório de trabalho atual,
// e honra ASPNETCORE_ENVIRONMENT + variáveis de ambiente (ex.: ConnectionStrings__DefaultConnection).
public class LeadTrackerContextFactory : IDesignTimeDbContextFactory<LeadTrackerContext>
{
    public LeadTrackerContext CreateDbContext(string[] args)
    {
        var apiDir      = ResolveApiDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não configurada. Defina em appsettings.{Environment}.json " +
                "ou via variável de ambiente ConnectionStrings__DefaultConnection.");

        var options = new DbContextOptionsBuilder<LeadTrackerContext>()
            .UseMySql(connection, ServerVersion.AutoDetect(connection))
            .Options;

        return new LeadTrackerContext(options, new NoOpPublisher());
    }

    // Sobe na árvore de diretórios até encontrar a pasta do projeto API.
    private static string ResolveApiDirectory()
    {
        const string apiProject = "CGI.LeadTracker.API";

        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            // O próprio diretório atual é o projeto API
            if (dir.Name == apiProject && File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
                return dir.FullName;

            // O projeto API está sob src/ a partir daqui (raiz do repo)
            var nested = Path.Combine(dir.FullName, "src", apiProject);
            if (File.Exists(Path.Combine(nested, "appsettings.json")))
                return nested;

            // Projeto irmão (rodando de dentro do projeto Infrastructure)
            var sibling = Path.Combine(dir.FullName, apiProject);
            if (File.Exists(Path.Combine(sibling, "appsettings.json")))
                return sibling;
        }

        return Directory.GetCurrentDirectory();
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
