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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<SopalTraceDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

// 2. Injection des dépendances (Clean Architecture)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPlanNcRepository, PlanNcRepository>();
builder.Services.AddScoped<IPlanNcService, PlanNcService>();
builder.Services.AddScoped<IErpService, SqlErpService>();
builder.Services.AddScoped<IPlanVerifMachineRepository, PlanVerifMachineRepository>();
builder.Services.AddScoped<IPlanVerifMachineService, PlanVerifMachineService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJournalConnexionRepository, JournalConnexionRepository>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPlanAssRepository, PlanAssRepository>();
builder.Services.AddScoped<IPlanAssService, PlanAssService>();
builder.Services.AddScoped<IPlanFabricationRepository, PlanFabricationRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPlanFabricationService, PlanFabricationService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateModeleRequestValidator>();
builder.Services.AddScoped<IPlanEchanRepository, PlanEchanRepository>();
builder.Services.AddScoped<IPlanEchanService, PlanEchanService>();
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IReferentielService, ReferentielService>();
builder.Services.AddScoped<IHubService, HubService>();
builder.Services.AddScoped<IModeleFabricationService, ModeleFabricationService>();
builder.Services.AddScoped<IPlanPfRepository, PlanPfRepository>();
builder.Services.AddScoped<IPlanPfService, PlanPfService>();

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