using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Globalization;
using System.Net;
using MimeKit.Utils;

namespace MreHunter.Api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;

        public EmailService(IConfiguration config, IWebHostEnvironment environment)
        {
            _config = config;
            _environment = environment;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";

            var portStr = _config["Email:SmtpPort"];
            var port = int.TryParse(portStr, out var p) ? p : 587;

            var user = _config["Email:SmtpUser"] ?? "";
            var pass = (_config["Email:SmtpPass"] ?? "").Replace(" ", "");
            var from = _config["Email:From"] ?? user;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                throw new Exception("Falta Email:SmtpUser o Email:SmtpPass en appsettings.");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody
            }.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Timeout = 20000;

            if (port == 465)
                await smtp.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
            else if (port == 587)
                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            else
                await smtp.ConnectAsync(host, port, SecureSocketOptions.Auto);

            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
public async Task SendLoginAlertAsync(string toEmail, string username, string? ip)
{
    var subject = "Nuevo inicio de sesión - MreHunter";

    var safeUser = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(username) ? "usuario" : username);
    var safeIp = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(ip) ? "No disponible" : ip);

    var colombiaTime = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
    var localDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, colombiaTime);

    var loginDate = localDate.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("es-CO"));
    var loginTime = localDate.ToString("hh:mm tt", new CultureInfo("es-CO"));

    var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";

    var portStr = _config["Email:SmtpPort"];
    var port = int.TryParse(portStr, out var p) ? p : 587;

    var user = _config["Email:SmtpUser"] ?? "";
    var pass = (_config["Email:SmtpPass"] ?? "").Replace(" ", "");
    var from = _config["Email:From"] ?? user;

    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        throw new Exception("Falta Email:SmtpUser o Email:SmtpPass en appsettings.");

    var message = new MimeMessage();
    message.From.Add(MailboxAddress.Parse(from));
    message.To.Add(MailboxAddress.Parse(toEmail));
    message.Subject = subject;

    var builder = new BodyBuilder();

    var logoPath = Path.Combine(_environment.WebRootPath ?? "", "images", "MreRolando.png");

    string logoHtml;

    if (File.Exists(logoPath))
    {
        var logo = builder.LinkedResources.Add(logoPath);
        logo.ContentId = "logo-mrehunter";

        logoHtml = @"
            <img src='cid:logo-mrehunter'
                 alt='MRE HUNTER'
                 style='display:block;margin:0 auto 16px auto;max-width:320px;width:100%;height:auto;border:0;' />";
    }
    else
    {
        logoHtml = @"
            <div style='text-align:center;margin-bottom:10px;'>
                <span style='font-size:32px;font-weight:900;color:#ef4444;'>MRE</span>
                <span style='font-size:32px;font-weight:900;color:#ffffff;'>HUNTER</span>
            </div>";
    }

    var body = $@"
    <!DOCTYPE html>
    <html lang='es'>
    <body style='margin:0;padding:0;background-color:#eef2f7;font-family:Arial,Helvetica,sans-serif;color:#111827;'>

    <table width='100%' cellpadding='0' cellspacing='0' style='padding:40px 16px;background:#eef2f7;'>

    <tr>
    <td align='center'>

    <table width='640' style='background:#ffffff;border-radius:22px;border:1px solid #e5e7eb;overflow:hidden;'>

    <tr>
    <td style='background:linear-gradient(135deg,#020617,#0f172a);padding:34px 24px 24px 24px;text-align:center;'>

    {logoHtml}

    <div style='display:inline-block;background:#ffffff1f;color:#ffffff;font-size:12px;font-weight:700;letter-spacing:.8px;padding:9px 18px;border-radius:999px;'>
    ALERTA DE SEGURIDAD
    </div>

    </td>
    </tr>

    <tr>
    <td style='padding:36px;'>

    <h1 style='margin:0 0 16px;font-size:30px;font-weight:800;color:#111827;text-align:center;'>
    Nuevo inicio de sesión detectado
    </h1>

    <p style='margin:0 0 14px;font-size:16px;line-height:28px;color:#374151;text-align:center;'>
    Hola <strong>{safeUser}</strong>,
    </p>

    <p style='margin:0 0 24px;font-size:16px;line-height:28px;color:#4b5563;text-align:center;'>
    Detectamos un inicio de sesión reciente en tu cuenta de <strong>MreHunter</strong>.
    Te compartimos los detalles para que puedas verificar la actividad.
    </p>

    <table width='100%' style='background:#f9fafb;border:1px solid #e5e7eb;border-radius:16px;padding:18px;margin-bottom:24px;'>

    <tr>
    <td style='font-size:14px;color:#6b7280;'>Fecha</td>
    <td align='right' style='font-size:15px;font-weight:700;color:#111827;'>{loginDate}</td>
    </tr>

    <tr>
    <td style='font-size:14px;color:#6b7280;padding-top:10px;'>Hora</td>
    <td align='right' style='font-size:15px;font-weight:700;color:#111827;padding-top:10px;'>{loginTime}</td>
    </tr>

    <tr>
    <td style='font-size:14px;color:#6b7280;padding-top:10px;'>Dirección IP</td>
    <td align='right' style='font-size:15px;font-weight:700;color:#111827;padding-top:10px;'>{safeIp}</td>
    </tr>

    </table>

    <div style='background:#111827;border-radius:16px;padding:22px;color:#d1d5db;text-align:center;'>

    <strong style='color:#ffffff;'>¿Reconoces esta actividad?</strong>

    <p style='margin-top:10px;font-size:15px;line-height:26px;'>
    Si fuiste tú, no necesitas hacer nada.
    <br>
    Si no reconoces este acceso, cambia tu contraseña inmediatamente.
    </p>

    </div>

    </td>
    </tr>

    <tr>
    <td style='background:#f8fafc;border-top:1px solid #e5e7eb;padding:22px;text-align:center;'>

    <p style='margin:0;font-size:13px;color:#64748b;'>
    © {DateTime.UtcNow.Year} MreHunter. Todos los derechos reservados.
    </p>

    <p style='margin-top:6px;font-size:12px;color:#94a3b8;'>
    Correo automático de seguridad. No respondas a este mensaje.
    </p>

    </td>
    </tr>

    </table>

    </td>
    </tr>

    </table>

    </body>
    </html>";

        builder.HtmlBody = body;

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        smtp.Timeout = 20000;

        if (port == 465)
            await smtp.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
        else if (port == 587)
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        else
            await smtp.ConnectAsync(host, port, SecureSocketOptions.Auto);

        await smtp.AuthenticateAsync(user, pass);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
}
    }
}