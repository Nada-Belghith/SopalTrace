using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using SopalTrace.Api.Middlewares;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Services;
using SopalTrace.Application.Validators;
using SopalTrace.Infrastructure.Data;
using SopalTrace.Infrastructure.Repositories;
using SopalTrace.Infrastructure.Services;
using SopalTrace.Infrastructure.Services.Erp;
using SopalTrace.Infrastructure.Services.Security;
using SopalTrace.Infrastructure.UnitOfWork;
using SopalTrace.Application.Services.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Enregistrement du fournisseur d'encodage pour supporter Windows-1252 (utile pour l'import Excel/CSV)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// Configuration globale pour EPPlus 8+
OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("SopalTrace");

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
builder.Services.AddControllers();
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
builder.Services.AddScoped<IPlanVerifMachineService, PlanVerifMachineService>();
builder.Services.AddScoped<IPlanEchantillonnageService, PlanEchantillonnageService>();

builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecEncfRepository, SopalTrace.Infrastructure.Repositories.Execution.ExecEncfRepository>();
builder.Services.AddScoped<SopalTrace.Application.Interfaces.Execution.IExecEncfService, SopalTrace.Application.Services.ExecEncfService>();
builder.Services.AddScoped<IErpService, SqlErpService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJournalConnexionRepository, JournalConnexionRepository>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
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
var secretKey = builder.Configuration["Jwt:Secret"] ?? "VotreCleSecreteDePlusDe32Caracteres";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
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
});

// --- CONFIGURATION DU CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueJsPolicy", b =>
        b.WithOrigins("http://localhost:5173")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials());
});

var app = builder.Build();

// 3. Middleware
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("VueJsPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();