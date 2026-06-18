using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Domain.SeedWork;
using CGI.LeadTracker.Infrastructure;
using CGI.LeadTracker.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CGI.LeadTracker.API.Extensions;

// Inicialização do banco no startup: aplica migrations pendentes e semeia o usuário
// admin de forma idempotente. Roda em todos os ambientes. Pode ser desligado via
// Database:MigrateOnStartup / Database:SeedOnStartup (deploys com DBA controlando o schema).
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp     = scope.ServiceProvider;
        var config = app.Configuration;
        var logger = sp.GetRequiredService<ILogger<Program>>();

        if (config.GetValue("Database:MigrateOnStartup", true))
        {
            var context  = sp.GetRequiredService<LeadTrackerContext>();
            var pending  = (await context.Database.GetPendingMigrationsAsync()).ToList();

            if (pending.Count > 0)
            {
                logger.LogInformation("Aplicando {Count} migration(s) pendente(s): {Migrations}",
                    pending.Count, string.Join(", ", pending));
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations aplicadas com sucesso.");
            }
            else
            {
                logger.LogInformation("Banco de dados já está atualizado — nenhuma migration pendente.");
            }
        }

        if (config.GetValue("Database:SeedOnStartup", true))
            await SeedAdminAsync(sp, config, app.Environment, logger);
    }

    private static async Task SeedAdminAsync(
        IServiceProvider sp,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger logger)
    {
        var userRepo   = sp.GetRequiredService<IUserRepository>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

        var name  = config["Seed:AdminName"]  ?? "Administrador";
        var email = config["Seed:AdminEmail"] ?? "admin@presenca.com.br";

        if (await userRepo.GetByEmailAsync(email) is not null)
        {
            logger.LogInformation("Seed: usuário admin '{Email}' já existe — ignorando.", email);
            return;
        }

        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException(
                    "Seed:AdminPassword não configurada. Defina a senha do admin via variável de ambiente " +
                    "Seed__AdminPassword (ou appsettings.Production.json) antes do primeiro start, ou desative " +
                    "o seed com Database:SeedOnStartup=false.");

            // Em desenvolvimento, usa uma senha padrão para facilitar o setup local.
            password = "Admin@123";
            logger.LogWarning("Seed: usando senha de DESENVOLVIMENTO padrão para '{Email}'. Não use em produção.", email);
        }

        var admin = User.Create(name, email, PasswordHelper.Hash(password));
        await userRepo.AddAsync(admin);
        await unitOfWork.SaveEntitiesAsync();

        logger.LogInformation("Seed: usuário admin '{Email}' criado.", email);
    }
}
