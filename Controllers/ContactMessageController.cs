using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MreHunter.Api.Data;
using MreHunter.Api.Infrastructure;
using MreHunter.Api.Models;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/contact-form")]
    [Tags("Formulario Contacto (Datos)")]
    public class ContactMessageController : ControllerBase
    {
        private readonly Db _db;
        private readonly EmailQueue _emailQueue;
        private readonly IConfiguration _configuration;

        public ContactMessageController(
            Db db,
            EmailQueue emailQueue,
            IConfiguration configuration)
        {
            _db = db;
            _emailQueue = emailQueue;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [EnableRateLimiting("public-forms")]
        [RequireTurnstile]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactMessageRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Body requerido." });

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest(new { message = "El nombre es obligatorio." });

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "El mensaje es obligatorio." });

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO contact_messages
                (
                    first_name,
                    last_name,
                    phone,
                    email,
                    message,
                    created_at
                )
                VALUES
                (
                    @firstName,
                    @lastName,
                    @phone,
                    @email,
                    @message,
                    NOW()
                );
            ";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            cmd.Parameters.AddWithValue("@firstName", request.FirstName ?? "");
            cmd.Parameters.AddWithValue("@lastName", request.LastName ?? "");
            cmd.Parameters.AddWithValue("@phone", request.Phone ?? "");
            cmd.Parameters.AddWithValue("@email", request.Email ?? "");
            cmd.Parameters.AddWithValue("@message", request.Message ?? "");

            await cmd.ExecuteNonQueryAsync();

            var toEmail = _configuration["ContactForm:ToEmail"];

            if (!string.IsNullOrWhiteSpace(toEmail))
            {
                var subject = "Nuevo mensaje del formulario de contacto";

                // Los datos vienen de un formulario público: se escapan para que no inyecten HTML en el correo.
                static string E(string? v) => WebUtility.HtmlEncode(v ?? "");

                var body = $@"
                    <h2>Nuevo mensaje de contacto</h2>

                    <p><strong>Nombre:</strong> {E(request.FirstName)}</p>
                    <p><strong>Apellido:</strong> {E(request.LastName)}</p>
                    <p><strong>Teléfono:</strong> {E(request.Phone)}</p>
                    <p><strong>Email:</strong> {E(request.Email)}</p>

                    <hr />

                    <p><strong>Mensaje:</strong></p>
                    <p>{E(request.Message).Replace("\n", "<br />")}</p>
                ";

                // El correo se envía en segundo plano: el usuario no espera al servidor SMTP.
                _emailQueue.Enqueue(email => email.SendAsync(toEmail, subject, body));
            }

            return Ok(new
            {
                message = "Mensaje enviado correctamente."
            });
        }
    }
}
