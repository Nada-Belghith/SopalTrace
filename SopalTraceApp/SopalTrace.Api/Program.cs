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
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration de Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- CONFIGURATION SWAGGER AVEC SUPPORT JWT ---
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "SopalTrace API", Version = "v1" });
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

// 1. Base de données (Une seule base maintenant !)
var connectionString = builder.Configuration.GetConnectionString("SopalTraceConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'SopalTraceConnection' is not configured.");

builder.Services.AddDbContext<SopalTraceDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

// 2. Injection des dépendances (Clean Architecture)
builder.Services.AddScoped<IErpService, SqlErpService>();
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

// --- CONFIGURATION DU CORS (CORRIGÉ POUR HTTPONLY COOKIES) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueJsPolicy", b =>
        b.WithOrigins("http://localhost:5173") // ⚠️ L'URL exacte de ton front Vue.js
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()); // 💡 INDISPENSABLE pour autoriser le passage du cookie HttpOnly
});

var app = builder.Build();

// 3. Utilisation du Middleware d'exceptions personnalisé
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// L'ordre ici est CRITIQUE pour la sécurité
app.UseCors("VueJsPolicy"); // ⚠️ Utilisation de la nouvelle politique stricte
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();