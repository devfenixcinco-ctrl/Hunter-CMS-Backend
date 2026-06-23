using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MreHunter.Api.Data;
using MreHunter.Api.Services;
using MreHunter.Api.Middleware;
using MreHunter.Api.Models.Examples;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
       policy.WithOrigins(
        "http://localhost:4200",
        "http://192.168.2.200:4200",
        "http://192.168.2.133:4200",
        "https://hunter.pruebasfenix.com"
    )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
    {
        var sessionId = httpContext.Request.Cookies["mh_session"];
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        var ip = string.IsNullOrWhiteSpace(forwardedFor)
            ? (httpContext.Connection.RemoteIpAddress?.ToString() ?? "no-ip")
            : forwardedFor.Split(',')[0].Trim();

        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
            userAgent = "no-ua";

        if (userAgent.Length > 64)
            userAgent = userAgent[..64];

        var partitionKey = string.IsNullOrWhiteSpace(sessionId)
            ? $"anon:{ip}|ua:{userAgent}"
            : $"sess:{sessionId}|ip:{ip}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, token) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Demasiados intentos. Intenta nuevamente en unos segundos."
        }, cancellationToken: token);
    };
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<FaqSettingsUpdateRequestExample>();

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.ExampleFilters();

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MreHunter.Api",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Escribe asi: Bearer {tu_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddSingleton<Db>();
builder.Services.AddTransient<EmailService>();

builder.Services.AddScoped<ProductsService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<ProductsPetsService>();
builder.Services.AddScoped<ProductsDashcamService>();
builder.Services.AddScoped<ProductsPortableService>();
builder.Services.AddScoped<PortableBenefitsService>();
builder.Services.AddScoped<PortableMediaSectionsService>();
builder.Services.AddScoped<DashcamMediaSectionsService>();
builder.Services.AddScoped<DashcamBenefitsService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GpsSectionService>();
builder.Services.AddScoped<PlanPlusSectionService>();
builder.Services.AddScoped<PlanPlusBenefitService>();
builder.Services.AddScoped<ProductsPetsBenefitService>();
builder.Services.AddScoped<ProductsPetsMediaSectionService>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");
app.UseMiddleware<SessionCookieMiddleware>();
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();