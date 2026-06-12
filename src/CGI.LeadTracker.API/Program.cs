using System.Text;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.API.Application.Adapters.Google;
using CGI.LeadTracker.API.Application.Adapters.Meta;
using CGI.LeadTracker.API.Application.Adapters.RdStation;
using CGI.LeadTracker.API.Application.Behaviors;
using CGI.LeadTracker.API.Application.Services;
using CGI.LeadTracker.API.Middleware;
using CGI.LeadTracker.API.Services;
using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Infrastructure;
using CGI.LeadTracker.Infrastructure.Extensions;
using CGI.LeadTracker.Infrastructure.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── MediatR ──────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ── Infraestrutura ────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Autenticação JWT ──────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Auth:Secret"]!;
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém os claims originais do JWT (sub, email, name) sem remapeamento legado
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer          = true,
            ValidIssuer             = builder.Configuration["Auth:Issuer"],
            ValidateAudience        = true,
            ValidAudience           = builder.Configuration["Auth:Audience"],
            ValidateLifetime        = true,
            ClockSkew               = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ── Integrações externas ──────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddTransient<RdStationTokenHandler>();

// BaseAddress precisa terminar em '/' para preservar o segmento de versão da URL
builder.Services.AddHttpClient<IRdStationService, RdStationService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["RdStation:BaseUrl"]!.TrimEnd('/') + "/"))
    .AddHttpMessageHandler<RdStationTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<IMetaConversionsService, MetaConversionsService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Meta:BaseUrl"]!.TrimEnd('/') + "/"))
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IGoogleAdsService, GoogleAdsService>();
builder.Services.AddScoped<IConversionDispatchService, ConversionDispatchService>();
builder.Services.AddScoped<ILeadSyncService, LeadSyncService>();
builder.Services.AddHostedService<LeadSyncBackgroundService>();

// ── Serviços de identidade ────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityService, IdentityService>();

// ── Tratamento de erros e health ──────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LeadTrackerContext>();

// ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "CGI LeadTracker API";
        options.Theme = ScalarTheme.DeepSpace;
    });

    // Seed: garante um admin padrão em desenvolvimento
    using var scope       = app.Services.CreateScope();
    var userRepo          = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var unitOfWork        = scope.ServiceProvider.GetRequiredService<CGI.LeadTracker.Domain.SeedWork.IUnitOfWork>();
    const string adminEmail = "admin@presenca.com.br";
    if (await userRepo.GetByEmailAsync(adminEmail) is null)
    {
        var admin = User.Create("Administrador", adminEmail, PasswordHelper.Hash("Admin@123"));
        await userRepo.AddAsync(admin);
        await unitOfWork.SaveEntitiesAsync();
        Log.Information("Usuário admin criado: {Email} / Admin@123", adminEmail);
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Iniciando CGI LeadTracker API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
