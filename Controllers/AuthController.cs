using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Infrastructure;
using MySqlConnector;
using MreHunter.Api.Services;
using MreHunter.Api.Models;
using MreHunter.Api.Data;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Tags("Autenticación")]
    public class AuthController : ControllerBase
    {
        private readonly Db _db;
        private readonly EmailQueue _emailQueue;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        public AuthController(Db db, EmailQueue emailQueue, IConfiguration config, IMemoryCache cache)
        {
            _db = db;
            _emailQueue = emailQueue;
            _config = config;
            _cache = cache;
        }
        //  REGISTER (requiere token: solo un administrador autenticado puede crear usuarios)
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var email = (request.Email ?? "").Trim().ToLower();
            var password = request.Password ?? "";
            var name = (request.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Email, password y name son requeridos." });

            if (password.Length < MinPasswordLength)
                return BadRequest(new { message = $"La contraseña debe tener al menos {MinPasswordLength} caracteres." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using (var checkCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM users WHERE LOWER(email)=@email;", conn))
            {
                checkCmd.Parameters.AddWithValue("@email", email);
                var exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                if (exists > 0)
                    return BadRequest(new { message = "Usuario ya existe." });
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            long userId;
            using (var insertCmd = new MySqlCommand(@"
                INSERT INTO users (email, password_hash, name, created_at, updated_at)
                VALUES (@email, @hash, @name, @now, @now);
                SELECT LAST_INSERT_ID();", conn))
            {
                insertCmd.Parameters.AddWithValue("@email", email);
                insertCmd.Parameters.AddWithValue("@hash", hash);
                insertCmd.Parameters.AddWithValue("@name", name);
                insertCmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                userId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());
            }

            try
            {
                var subject = "Bienvenido a MreHunter";
                var body = $@"
                    <div style=""font-family: Arial, sans-serif; line-height:1.5;"">
                        <h3>Hola {System.Net.WebUtility.HtmlEncode(name)} 👋</h3>
                        <p>Tu cuenta fue creada correctamente.</p>
                        <p><b>Email:</b> {System.Net.WebUtility.HtmlEncode(email)}</p>
                        <p>Ya puedes iniciar sesión cuando quieras.</p>
                        <hr />
                        <p style=""color:#666;font-size:12px;"">
                            Si tú no creaste esta cuenta, ignora este correo.
                        </p>
                    </div>";

                _emailQueue.Enqueue(e => e.SendAsync(email, subject, body));
            }
            catch { }

            return Ok(new { message = "Usuario registrado correctamente.", userId });
        }

        //  LOGIN
        //  - Límite global por IP (rate limiting "login": 5 por minuto).
        //  - Bloqueo por correo + IP: tras 3 fallos, ESE equipo queda bloqueado para ESE correo 3 minutos.
        //    Un tercero ya no puede bloquear la cuenta de un administrador desde otra IP, y como se aplica
        //    igual a correos que no existen, el bloqueo no revela qué correos están registrados.
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var email = (request.Email ?? "").Trim().ToLower();
            var password = request.Password ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Email y contraseña son obligatorios." });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "sin-ip";
            var failKey = $"login-fail:{email}:{ip}";

            if (_cache.TryGetValue(failKey, out int fails) && fails >= MaxFailedLogins)
                return TooManyFailedLogins();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long userId;
            string dbEmail;
            string dbName;
            string passwordHash;

            using (var cmd = new MySqlCommand(@"
                SELECT id, email, name, password_hash
                FROM users
                WHERE LOWER(email)=@email
                LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    // Se verifica contra un hash ficticio para que el tiempo de respuesta sea igual
                    // exista o no el usuario, y se responde lo mismo que con contraseña incorrecta.
                    BCrypt.Net.BCrypt.Verify(password, DummyPasswordHash);
                    RegisterFailedLogin(failKey);
                    return InvalidCredentials();
                }

                userId = Convert.ToInt64(reader["id"]);
                dbEmail = reader["email"].ToString()!;
                dbName = reader["name"].ToString()!;
                passwordHash = reader["password_hash"].ToString()!;
            }

            var now = DateTime.UtcNow;

            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
            {
                RegisterFailedLogin(failKey);
                return InvalidCredentials();
            }

            _cache.Remove(failKey);

            using (var reset = new MySqlCommand(@"
                UPDATE users
                SET failed_login_attempts=0,
                    lockout_until=NULL,
                    updated_at=@now
                WHERE id=@id;", conn))
            {
                reset.Parameters.AddWithValue("@now", now);
                reset.Parameters.AddWithValue("@id", userId);
                await reset.ExecuteNonQueryAsync();
            }

            var token = GenerateJwtToken(userId, dbEmail, dbName);

            _emailQueue.Enqueue(e => e.SendLoginAlertAsync(dbEmail, dbName, ip));

            return Ok(new { message = "Login exitoso", token });
        }

        [AllowAnonymous]
        [EnableRateLimiting("public-forms")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var email = (request.Email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email requerido." });

            var okResponse = new { message = "Si el email existe, se enviará un enlace de recuperación." };

            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                long? userId = null;
                string? userName = null;

                using (var cmd = new MySqlCommand(@"
                    SELECT id, name
                    FROM users
                    WHERE LOWER(email)=@email
                    LIMIT 1;", conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        userId = Convert.ToInt64(reader["id"]);
                        userName = reader["name"].ToString();
                    }
                }

                if (userId == null)
                    return Ok(okResponse);

                var rawToken = GenerateSecureToken(48);
                var tokenHash = Sha256(rawToken);
                var expiresAt = DateTime.UtcNow.AddMinutes(30);

                using (var cmd = new MySqlCommand(@"
                    UPDATE password_reset_tokens
                    SET used_at=@now
                    WHERE user_id=@uid AND used_at IS NULL;", conn))
                {
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@uid", userId.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmd = new MySqlCommand(@"
                    INSERT INTO password_reset_tokens (user_id, token_hash, expires_at, used_at, created_at)
                    VALUES (@uid, @hash, @exp, NULL, @created);", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId.Value);
                    cmd.Parameters.AddWithValue("@hash", tokenHash);
                    cmd.Parameters.AddWithValue("@exp", expiresAt);
                    cmd.Parameters.AddWithValue("@created", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }

                var template = _config["Frontend:ResetPasswordUrl"];
                if (string.IsNullOrWhiteSpace(template))
                {
                    Console.WriteLine("⚠️ Frontend:ResetPasswordUrl no está definido.");
                    return Ok(okResponse);
                }

                var resetUrl = template.Replace("{token}", Uri.EscapeDataString(rawToken));

                var subject = "Recuperación de contraseña - MreHunter";
                var body = $@"
                    <div style=""font-family: Arial, sans-serif; line-height:1.5;"">
                        <h3>Recuperación de contraseña</h3>
                        <p>Hola {System.Net.WebUtility.HtmlEncode(userName ?? "usuario")},</p>
                        <p>Haz clic en el enlace para restablecer tu contraseña:</p>
                        <p><a href=""{resetUrl}"">{resetUrl}</a></p>
                        <p>Este enlace expira en 30 minutos.</p>
                    </div>";

                _emailQueue.Enqueue(e => e.SendAsync(email, subject, body));

                return Ok(okResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ForgotPassword ERROR: " + ex.Message);
                return Ok(okResponse);
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var token = (request.Token ?? "").Trim();
            var newPassword = request.NewPassword ?? "";

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token requerido." });

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
                return BadRequest(new { message = $"La contraseña debe tener al menos {MinPasswordLength} caracteres." });

            var tokenHash = Sha256(token);

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long? tokenId = null;
            long? userId = null;

            using (var cmd = new MySqlCommand(@"
                SELECT id, user_id
                FROM password_reset_tokens
                WHERE token_hash=@hash
                  AND used_at IS NULL
                  AND expires_at > @now
                ORDER BY id DESC
                LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@hash", tokenHash);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    tokenId = Convert.ToInt64(reader["id"]);
                    userId = Convert.ToInt64(reader["user_id"]);
                }
            }

            if (tokenId == null || userId == null)
                return BadRequest(new { message = "Token inválido o expirado." });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            using (var cmd = new MySqlCommand(@"
                UPDATE users
                SET password_hash=@ph, updated_at=@now
                WHERE id=@uid;", conn))
            {
                cmd.Parameters.AddWithValue("@ph", passwordHash);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@uid", userId.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            using (var cmd = new MySqlCommand(@"
                UPDATE password_reset_tokens
                SET used_at=@now
                WHERE id=@tid;", conn))
            {
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@tid", tokenId.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

        /// <summary>Largo mínimo al crear o restablecer una contraseña. Las existentes no se ven afectadas.</summary>
        private const int MinPasswordLength = 10;

        private const int MaxFailedLogins = 3;
        private static readonly TimeSpan FailedLoginWindow = TimeSpan.FromMinutes(3);

        // Cada fallo reinicia la ventana: el bloqueo dura 3 minutos desde el último intento fallido.
        private void RegisterFailedLogin(string key)
        {
            var fails = _cache.TryGetValue(key, out int current) ? current + 1 : 1;
            _cache.Set(key, fails, FailedLoginWindow);
        }

        private ObjectResult TooManyFailedLogins()
            => StatusCode(StatusCodes.Status401Unauthorized, new { message = "Demasiados intentos fallidos. Espera unos minutos e intenta de nuevo." });

        // Respuesta única para usuario inexistente o contraseña incorrecta: no revela qué correos están registrados.
        private ObjectResult InvalidCredentials()
            => StatusCode(StatusCodes.Status401Unauthorized, new { message = "Correo o contraseña incorrectos." });

        private string GenerateJwtToken(long userId, string email, string name)
        {
            var key = _config["Jwt:Key"] ?? throw new Exception("Jwt:Key no definido");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var minutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 60;

            var claims = new List<Claim>
            {
                new Claim("id", userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name)
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private static string GenerateSecureToken(int bytesLength)
        {
            var bytes = RandomNumberGenerator.GetBytes(bytesLength);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static string Sha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
