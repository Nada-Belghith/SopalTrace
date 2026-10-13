using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SopalTrace.Api.Middlewares;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Application.Strategies;
using SopalTrace.Application.Validators;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;
using SopalTrace.Infrastructure.Services;
using SopalTrace.Infrastructure.Services.Erp;
using SopalTrace.Infrastructure.Services.Security;
using SopalTrace.Infrastructure.UnitOfWork;
using SopalTrace.Application.Services.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces.Repositories;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Enregistrement du fournisseur d'encodage pour supporter Windows-1252 (utile pour l'import Excel/CSV)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);


// Configuration de Serilog avec enrichissement de contexte
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SopalTrace")
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
// Validation automatique : tout DTO reçu doté d'un validateur FluentValidation est contrôlé avant l'action.
builder.Services.AddControllers(options => options.Filters.Add<SopalTrace.Api.Filters.ValidationFluentFiltre>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- CONFIGURATION SWAGGER AVEC SUPPORT JWT ---
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SopalTrace API", 
        Version = "v1",
        Description = "API de gestion des plans de qualité (assemblage et fabrication)"
    });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Veuillez entrer le token sous la forme 'Bearer {votre_token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[]{}
        }
    });
});

// 1. Base de données
var connectionString = builder.Configuration.GetConnectionString("SopalTraceConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'SopalTraceConnection' is not configured.");

builder.Services.AddScoped<SopalTrace.Infrastructure.Data.Interceptors.AuditableEntityInterceptor>();

builder.Services.AddDbContext<SopalTraceDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<SopalTrace.Infrastructure.Data.Interceptors.AuditableEntityInterceptor>();
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null))
        .AddInterceptors(interceptor);
});

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

// 2. Injection des dépendances (Clean Architecture)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register new Document Domain services
builder.Services.AddScoped<IDocumentEnteteRepository, DocumentEnteteRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDocumentTypeStrategy, PlanAssemblageStrategy>();
builder.Services.AddScoped<IDocumentTypeStrategy, PlanProduitFiniStrategy>();
builder.Services.AddScoped<IDocumentTypeStrategy, ControlePosteStrategy>();
builder.Services.AddScoped<IDocumentVerifMachineService, DocumentVerifMachineService>();
builder.Services.AddScoped<IDocumentEchantillonnageService, DocumentEchantillonnageService>();
builder.Services.AddScoped<IIso2859Service, SopalTrace.Infrastructure.Services.Iso2859Service>();
builder.Services.AddScoped<IExecEchantillonnageService, ExecEchantillonnageService>();
builder.Services.AddScoped<IExecRcPosteRepository, SopalTrace.Infrastructure.Repositories.Execution.ExecRcPosteRepository>();
builder.Services.AddScoped<IExecRcPosteService, ExecRcPosteService>();
builder.Services.AddScoped<IModeleFabricationService, ModeleFabricationService>();
builder.Services.AddScoped<IPlanFabricationService, PlanFabricationService>();

builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecEncfRepository, SopalTrace.Infrastructure.Repositories.Execution.ExecEncfRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecEncfService, SopalTrace.Application.Services.ExecEncfService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecPlanAssemblageRepository, SopalTrace.Infrastructure.Repositories.Execution.ExecPlanAssemblageRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecPlanAssemblageService, SopalTrace.Application.Services.ExecPlanAssemblageService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.ITracabiliteRepository, SopalTrace.Infrastructure.Repositories.Execution.TracabiliteRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.ITracabiliteService, SopalTrace.Application.Services.TracabiliteService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IOperateurRepository, SopalTrace.Infrastructure.Repositories.Execution.OperateurRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IOccurrenceRepository, SopalTrace.Infrastructure.Repositories.Execution.OccurrenceRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecutionOfService, SopalTrace.Application.Services.Execution.ExecutionOfService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecutionCatalogueService, SopalTrace.Application.Services.Execution.ExecutionCatalogueService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IAssemblageExecutionService, SopalTrace.Application.Services.Execution.AssemblageExecutionService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IAvancementAssemblageService, SopalTrace.Application.Services.Execution.AvancementAssemblageService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IVerifMachinePosteService, SopalTrace.Application.Services.Execution.VerifMachinePosteService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IVerifMachineExecutionService, SopalTrace.Application.Services.Execution.VerifMachineExecutionService>();
builder.Services.AddScoped<IOccurrenceService, SopalTrace.Application.Services.OccurrenceService>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IPlanificationProductionRepository, SopalTrace.Infrastructure.Repositories.Execution.PlanificationProductionRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IPlanificationProductionService, SopalTrace.Application.Services.Execution.PlanificationProductionService>();
// Contrôles de production de l'assemblage planifiés chaque minute, que les écrans soient ouverts ou non.
builder.Services.AddHostedService<SopalTrace.Api.TachesDeFond.PlanificationProductionWorker>();
builder.Services.AddScoped<IErpService, SqlErpService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJournalConnexionRepository, JournalConnexionRepository>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserSessionValidator, UserSessionValidator>();
builder.Services.AddSingleton(builder.Configuration.GetSection("Auth").Get<SopalTrace.Application.Settings.AuthSettings>()
    ?? new SopalTrace.Application.Settings.AuthSettings());
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAdministrationRepository, UserAdministrationRepository>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IMagasinRepository, MagasinRepository>();
builder.Services.AddScoped<IMagasinService, MagasinService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Alertes
builder.Services.AddScoped<SopalTrace.Application.Alertes.IAlerteDefinition<SopalTrace.Application.Alertes.PlanManquantContexte>, SopalTrace.Application.Alertes.AlertePlanManquantDefinition>();
builder.Services.AddScoped<SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.PlanManquantContexte>>();
builder.Services.AddScoped<SopalTrace.Application.Alertes.IAlerteDefinition<SopalTrace.Application.Alertes.NotifsSuperviseurContexte>, SopalTrace.Application.Alertes.SuperviseurNotifsAlerteDefinition>();
builder.Services.AddScoped<SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsSuperviseurContexte>>();

builder.Services.AddScoped<SopalTrace.Application.Alertes.IAlerteDefinition<SopalTrace.Application.Alertes.NotifsManagerContexte>, SopalTrace.Application.Alertes.ManagerNotifsAlerteDefinition>();
builder.Services.AddScoped<SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsManagerContexte>>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddScoped<IFrequencyParserService, FrequencyParserService>();
builder.Services.AddScoped<IRefFormulaireRepository, RefFormulaireRepository>();
builder.Services.AddScoped<IRefFormulaireService, RefFormulaireService>();
builder.Services.AddScoped<IFormulaireStructureService, FormulaireStructureService>();
builder.Services.AddScoped<ICatalogueReferentielService, CatalogueReferentielService>();
builder.Services.AddScoped<IHubService, HubService>();
builder.Services.AddScoped<IDictionnaireQualiteRepository, DictionnaireQualiteRepository>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<SopalTrace.Infrastructure.Services.ExcelImport.IFusionStrategy, SopalTrace.Infrastructure.Services.ExcelImport.Strategies.Ser05FusionStrategy>();
builder.Services.AddScoped<SopalTrace.Infrastructure.Services.ExcelImport.IFusionStrategy, SopalTrace.Infrastructure.Services.ExcelImport.Strategies.MasFusionStrategy>();
builder.Services.AddScoped<SopalTrace.Infrastructure.Services.ExcelImport.IFusionStrategy, SopalTrace.Infrastructure.Services.ExcelImport.Strategies.BeeFusionStrategy>();
builder.Services.AddScoped<SopalTrace.Infrastructure.Services.ExcelImport.IFusionStrategy, SopalTrace.Infrastructure.Services.ExcelImport.Strategies.DefaultFusionStrategy>();
builder.Services.AddScoped<SopalTrace.Infrastructure.Services.ExcelImport.IExcelImportFactory, ExcelImportFactory>();
builder.Services.AddScoped<SopalTrace.Infrastructure.Services.ExcelImport.IExcelImportRccfService, SopalTrace.Infrastructure.Services.ExcelImport.ExcelImportRccfService>();

// --- CONFIGURATION DE L'AUTHENTIFICATION JWT ---
// Aucune valeur par défaut : une clé connue publiquement permettrait de forger des jetons.
var secretKey = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
    throw new InvalidOperationException("La clé 'Jwt:Secret' doit être configurée (32 caractères minimum).");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    x.SaveToken = false;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = "SopalTraceApi",
        ValidateAudience = true,
        ValidAudience = "SopalTraceVueJs",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    // Un jeton signé ne suffit pas : le compte doit être ACTIVE, son tampon de sécurité inchangé et son rôle
    // identique à celui du jeton (changement de rôle, désactivation ou nouveau mot de passe ferment les sessions aussitôt).
    x.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var validateur = context.HttpContext.RequestServices.GetRequiredService<IUserSessionValidator>();
            var sub = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tampon = context.Principal?.FindFirst(SessionClaims.SecurityStamp)?.Value;
            var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            if (!Guid.TryParse(sub, out var userId) || !await validateur.IsSessionValidAsync(userId, tampon, role))
                context.Fail("Session révoquée.");
        }
    };
});

// --- CONFIGURATION DU CORS ---
// Origines autorisées lues depuis la configuration (Cors__AllowedOrigins__0, __1...).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
    allowedOrigins = new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueJsPolicy", b =>
        b.WithOrigins(allowedOrigins)
         .WithMethods("GET", "POST", "PUT", "DELETE")
         .WithHeaders("Authorization", "Content-Type", "Accept")
         .AllowCredentials());
});

// --- LIMITATION DE DÉBIT (anti-DoS / anti-brute-force) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("matricule")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonyme",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // Points d'entrée anonymes de l'authentification : limite stricte par adresse IP.
    options.AddPolicy(SopalTrace.Api.PolitiquesDebit.Authentification, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonyme",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("Auth:RequetesParMinute", 10),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Derrière nginx : l'adresse du client est lue dans X-Forwarded-For (sinon tous les postes partageraient
// l'adresse de nginx et la limite de débit de l'authentification s'appliquerait à toute l'usine).
// L'API n'est joignable que par nginx (aucun port publié) : un seul saut de proxy est accepté.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var app = builder.Build();

// 3. Middleware
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionMiddleware>();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    // L'API ne sert que du JSON ; Swagger (dev uniquement) a besoin de charger ses scripts.
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        headers["Cache-Control"] = "no-store";
    }
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseCors("VueJsPolicy");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();
