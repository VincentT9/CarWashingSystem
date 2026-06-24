using System.Text;
using System.Threading.RateLimiting;
using API.Helpers;
using API.Middlewares;
using API.Seed;
using BusinessLayer.Helpers;
using BusinessLayer.IService;
using BusinessLayer.IService.AI;
using BusinessLayer.Service;
using BusinessLayer.Service.AI;
using BusinessLayer.Validators;
using DataAccessLayer.Context;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── CORS ────────────────────────────────────────────────────
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>() ?? new CorsSettings();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (corsSettings.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsSettings.AllowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// ─── Controllers ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// ─── Swagger + JWT Bearer Authorization ──────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoWash Pro API",
        Version = "v1",
        Description = "Smart Car Washing Management System API"
    });

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGciOiJIUzI1NiIs...",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── Database ────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseNpgsql(
        builder.Configuration.GetConnectionString("MyDB"));
});

// ─── JWT Configuration ───────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secretKey = jwtSettings["SecretKey"]!;
var issuer = jwtSettings["Issuer"]!;
var audience = jwtSettings["Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
});

// ─── Rate Limiting (AI) ──────────────────────────────────────
var aiSettings = builder.Configuration.GetSection("AiSettings").Get<AiSettings>() ?? new AiSettings();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AiCustomer", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = aiSettings.RateLimitPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("AiAdmin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.Identity?.Name ?? "admin-anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = aiSettings.AdminRateLimitPerMinute,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// ─── FluentValidation ────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ─── Configuration ───────────────────────────────────────────
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));
builder.Services.Configure<AiSettings>(builder.Configuration.GetSection("AiSettings"));

// ─── Dependency Injection ────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentCustomerService, CurrentCustomerService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleOwnershipValidator, VehicleOwnershipValidator>();
builder.Services.AddScoped<IWashHistoryService, WashHistoryService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IBehavioralLogService, BehavioralLogService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IAdminDashboardReadService, AdminDashboardReadService>();

// AI layer
builder.Services.AddSingleton<AiConversationStore>();
builder.Services.AddHttpClient<IGenerativeAIClient, GeminiClient>();
if (aiSettings.UseMockCustomerContext)
    builder.Services.AddScoped<ICustomerAIContextProvider, MockCustomerAIContextProvider>();
else
    builder.Services.AddScoped<ICustomerAIContextProvider, CustomerAIContextProvider>();
builder.Services.AddScoped<IAdminAIContextProvider, AdminAIContextProvider>();
builder.Services.AddScoped<IAIService, AiService>();

// ─── Build ───────────────────────────────────────────────────
var app = builder.Build();

// ─── Seed demo data ──────────────────────────────────────────
if (!app.Environment.IsEnvironment("Testing"))
    await DataSeeder.SeedAsync(app.Services);

// ─── Middleware Pipeline ─────────────────────────────────────
app.UseGlobalExceptionHandling();

app.UseCors("DefaultCors");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoWash Pro API v1");
});

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
