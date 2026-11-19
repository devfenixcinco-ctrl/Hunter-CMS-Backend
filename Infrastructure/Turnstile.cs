using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MreHunter.Api.Infrastructure
{
    /// <summary>
    /// Configuración de Cloudflare Turnstile (sección "Turnstile").
    /// La clave secreta va solo en el servidor: Turnstile__SecretKey en /etc/miapi.env.
    /// </summary>
    public sealed class TurnstileOptions
    {
        public string SecretKey { get; set; } = "";

        /// <summary>Dominios desde los que se acepta un token resuelto (Cloudflare devuelve el hostname).</summary>
        public string[] AllowedHostnames { get; set; } = Array.Empty<string>();

        /// <summary>La validación solo se activa cuando hay clave secreta.</summary>
        public bool Enabled => !string.IsNullOrWhiteSpace(SecretKey);
    }

    public sealed record TurnstileResult(bool Success, string[] ErrorCodes);

    /// <summary>
    /// Valida el token del widget contra la API siteverify de Cloudflare.
    /// https://developers.cloudflare.com/turnstile/get-started/server-side-validation/
    /// </summary>
    public sealed class TurnstileValidator
    {
        private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        /// <summary>Límite que documenta Cloudflare para el token.</summary>
        private const int MaxTokenLength = 2048;

        private readonly HttpClient _http;
        private readonly TurnstileOptions _options;
        private readonly ILogger<TurnstileValidator> _logger;

        public TurnstileValidator(HttpClient http, Microsoft.Extensions.Options.IOptions<TurnstileOptions> options, ILogger<TurnstileValidator> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool Enabled => _options.Enabled;

        public async Task<TurnstileResult> ValidateAsync(string? token, string? remoteIp, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(token))
                return new(false, new[] { "missing-input-response" });

            if (token.Length > MaxTokenLength)
                return new(false, new[] { "invalid-input-response" });

            var form = new Dictionary<string, string> { ["secret"] = _options.SecretKey, ["response"] = token };
            if (!string.IsNullOrWhiteSpace(remoteIp))
                form["remoteip"] = remoteIp;

            try
            {
                // Una sola llamada, sin reintentos automáticos: si Cloudflare no responde, el formulario se rechaza.
                using var response = await _http.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(form), ct);
                var body = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken: ct);

                if (body == null)
                    return new(false, new[] { "bad-response" });

                if (!body.Success)
                    return new(false, body.ErrorCodes ?? Array.Empty<string>());

                // El token debe haberse resuelto en uno de nuestros dominios (evita reutilizar tokens de otro sitio).
                if (_options.AllowedHostnames.Length > 0 &&
                    !_options.AllowedHostnames.Contains(body.Hostname ?? "", StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Token de Turnstile resuelto en un dominio no permitido: {Hostname}", body.Hostname);
                    return new(false, new[] { "hostname-mismatch" });
                }

                return new(true, Array.Empty<string>());
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                _logger.LogError(ex, "No se pudo verificar el token de Turnstile con Cloudflare.");
                return new(false, new[] { "siteverify-unreachable" });
            }
        }

        private sealed class SiteVerifyResponse
        {
            [JsonPropertyName("success")] public bool Success { get; set; }
            [JsonPropertyName("hostname")] public string? Hostname { get; set; }
            [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
        }
    }

    /// <summary>
    /// Exige un token válido de Cloudflare Turnstile en el endpoint.
    /// El token se lee de la cabecera X-Turnstile-Token o, en formularios multipart,
    /// del campo estándar "cf-turnstile-response". Si Turnstile no está configurado, no hace nada.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireTurnstileAttribute : Attribute, IAsyncActionFilter
    {
        public const string HeaderName = "X-Turnstile-Token";
        public const string FormFieldName = "cf-turnstile-response";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var validator = http.RequestServices.GetRequiredService<TurnstileValidator>();

            if (!validator.Enabled)
            {
                await next();
                return;
            }

            string? token = http.Request.Headers[HeaderName].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token) && http.Request.HasFormContentType)
                token = (await http.Request.ReadFormAsync(http.RequestAborted))[FormFieldName].FirstOrDefault();

            var ip = http.Connection.RemoteIpAddress?.ToString();
            var result = await validator.ValidateAsync(token, ip, http.RequestAborted);

            if (!result.Success)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    code = "TURNSTILE_FAILED",
                    message = "No pudimos verificar que eres una persona. Recarga la página e intenta de nuevo.",
                    errors = result.ErrorCodes
                });
                return;
            }

            await next();
        }
    }
}
