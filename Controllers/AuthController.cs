using Microsoft.AspNetCore.Mvc;
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

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Tags("Autenticación")]
    public class AuthController : ControllerBase
    {
        private readonly Db _db;
        private readonly EmailService _email;
        private readonly IConfiguration _config;

        public AuthController(Db db, EmailService email, IConfiguration config)
        {
            _db = db;
            _email = email;
            _config = config;
        }
        //  REGISTER
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

            if (password.Length < 6)
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

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

                await _email.SendAsync(email, subject, body);
            }
            catch { }

            return Ok(new { message = "Usuario registrado correctamente.", userId });
        }

        //  LOGIN (Rate limiting por sesión + lockout por usuario)
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            const int MAX_FAILED = 3;
            const int LOCKOUT_MINUTES = 3;

            var email = (request.Email ?? "").Trim().ToLower();
            var password = request.Password ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Email y contraseña son obligatorios." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            long userId;
            string dbEmail;
            string dbName;
            string passwordHash;
            int failedAttempts;
            DateTime? lockoutUntil;

            using (var cmd = new MySqlCommand(@"
                SELECT id, email, name, password_hash, failed_login_attempts, lockout_until
                FROM users
                WHERE LOWER(email)=@email
                LIMIT 1;", conn))
            {
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Usuario no encontrado" });

                userId = Convert.ToInt64(reader["id"]);
                dbEmail = reader["email"].ToString()!;
                dbName = reader["name"].ToString()!;
                passwordHash = reader["password_hash"].ToString()!;

                failedAttempts = reader["failed_login_attempts"] == DBNull.Value ? 0 : Convert.ToInt32(reader["failed_login_attempts"]);
                lockoutUntil = reader["lockout_until"] == DBNull.Value ? null : Convert.ToDateTime(reader["lockout_until"]);
            }

            var now = DateTime.UtcNow;

            if (lockoutUntil.HasValue && lockoutUntil.Value > now)
            {
                var remainingMinutes = (int)Math.Ceiling((lockoutUntil.Value - now).TotalMinutes);
                return StatusCode(423, new
                {
                    message = "Cuenta temporalmente bloqueada por intentos fallidos.",
                    retryAfterMinutes = remainingMinutes
                });
            }

            if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
            {
                failedAttempts++;

                DateTime? newLockoutUntil = null;
                if (failedAttempts >= MAX_FAILED)
                {
                    newLockoutUntil = now.AddMinutes(LOCKOUT_MINUTES);
                    failedAttempts = MAX_FAILED;
                }

                using (var upd = new MySqlCommand(@"
                    UPDATE users
                    SET failed_login_attempts=@failed,
                        lockout_until=@lockout_until,
                        updated_at=@now
                    WHERE id=@id;", conn))
                {
                    upd.Parameters.AddWithValue("@failed", failedAttempts);
                    upd.Parameters.AddWithValue("@lockout_until", (object?)newLockoutUntil ?? DBNull.Value);
                    upd.Parameters.AddWithValue("@now", now);
                    upd.Parameters.AddWithValue("@id", userId);
                    await upd.ExecuteNonQueryAsync();
                }

                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Contraseña incorrecta",
                    remainingAttempts = Math.Max(0, MAX_FAILED - failedAttempts),
                    locked = newLockoutUntil != null
                });
            }

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

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            try { await _email.SendLoginAlertAsync(dbEmail, dbName, ip); } catch { }

            return Ok(new { message = "Login exitoso", token });
        }

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

                try { await _email.SendAsync(email, subject, body); }
                catch (Exception ex) { Console.WriteLine("SMTP ERROR (forgot): " + ex.Message); }

                return Ok(okResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ForgotPassword ERROR: " + ex.Message);
                return Ok(okResponse);
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var token = (request.Token ?? "").Trim();
            var newPassword = request.NewPassword ?? "";

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token requerido." });

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });

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
