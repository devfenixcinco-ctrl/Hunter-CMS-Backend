using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MreHunter.Api.Data;
using MreHunter.Api.Infrastructure;
using MreHunter.Api.Services;
using MreHunter.Api.Models.Examples;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    // Seguridad: todo endpoint que no sea GET exige token, salvo los marcados con [AllowAnonymous].
    options.Conventions.Add(new RequireAuthForWritesConvention());

    // Rendimiento: cualquier escritura autenticada invalida la caché de salida.
    options.Filters.Add<OutputCacheEvictionFilter>();

    // Archivos subidos con formato inválido responden 400 en lugar de 500.
    options.Filters.Add<UploadValidationExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// ---------- Proxy inverso (nginx) ----------
// Permite que Request.Scheme sea "https" y que RemoteIpAddress sea la IP real del cliente.
// Por defecto solo se confía en proxies de loopback (nginx en la misma máquina).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// ---------- CORS ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Sin orígenes configurados el sitio no podría llamar a la API: mejor no arrancar
// (el deploy lo detecta y vuelve solo a la versión anterior).
if (allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins no está configurado en appsettings.json.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // Producción: solo el sitio público (appsettings.json).
        // Desarrollo: además localhost, la red local y el sitio de pruebas (appsettings.Development.json).
        // En el servidor se pueden agregar más con Cors__AllowedOrigins__2=https://... en /etc/miapi.env.
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            // El navegador guarda la respuesta del preflight y no repite el OPTIONS en cada llamada.
            .SetPreflightMaxAge(TimeSpan.FromHours(2));
    });
});

// ---------- Compresión ----------
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// ---------- Caché de salida ----------
// Los GET públicos se cachean en memoria; la política por defecto ya excluye peticiones
// autenticadas, respuestas con Set-Cookie y códigos distintos de 200.
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy
        .Expire(TimeSpan.FromMinutes(10))
        .SetVaryByHeader("Origin")
        .Tag(CacheTags.Cms));
});

// ---------- Rate limiting ----------
static string ClientIp(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "no-ip";

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: 5 intentos por minuto por IP real (ya no se puede evadir cambiando cookie o X-Forwarded-For).
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Formularios públicos (contacto, newsletter, empleo, recuperar contraseña): evita spam y abuso del correo.
    options.AddPolicy("public-forms", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ClientIp(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

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

// ---------- Swagger ----------
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

// ---------- JWT ----------
var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Sin una clave fuerte cualquiera podría firmar tokens válidos: no se arranca.
if (jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key no está configurada o tiene menos de 32 caracteres. " +
        "En el servidor debe definirse Jwt__Key en /etc/miapi.env.");

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

// ---------- Servicios ----------
builder.Services.AddSingleton<Db>();
builder.Services.AddMemoryCache(); // Bloqueo de login por correo + IP (AuthController)
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailBackgroundService>();

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

app.UseForwardedHeaders();

// Swagger solo en desarrollo, o en producción si se habilita explícitamente con Swagger__Enabled=true.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    // Las imágenes subidas se cachean 1 hora en el navegador (algunas usan nombre fijo, p. ej. logo.jpeg).
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public,max-age=3600"
});
app.UseRouting();
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapControllers();
app.Run();
