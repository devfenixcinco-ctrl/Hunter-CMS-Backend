using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MySqlConnector;
using MreHunter.Api.Models;
using System.Linq;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/work-with-us")]
    [Tags("¿Quienes Somos? (Formulario)")]
    public class JobsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public JobsController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        [Authorize] // Datos personales de postulantes: solo administradores
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return StatusCode(500, new
                    {
                        ok = false,
                        code = "DATABASE_CONFIG_MISSING",
                        mensaje = "No se encontró la configuración de base de datos."
                    });
                }

                var applications = new List<object>();

                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT id, document_id, first_name, last_name, phone, cv_file_path, created_at
                    FROM job_applications
                    ORDER BY created_at DESC;
                ";

                await using var command = new MySqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    applications.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        DocumentId = reader.GetString("document_id"),
                        FirstName = reader.GetString("first_name"),
                        LastName = reader.GetString("last_name"),
                        Phone = reader.GetString("phone"),
                        CvFilePath = reader.GetString("cv_file_path"),
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }

                return Ok(new
                {
                    ok = true,
                    data = applications
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    ok = false,
                    code = "SERVER_ERROR",
                    mensaje = "Error al obtener las postulaciones."
                });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("public-forms")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Apply([FromForm] JobApplicationRequest request)
        {

    try
    {

        if (!ModelState.IsValid)
        {
            foreach (var kvp in ModelState)
            {
                var key = kvp.Key;
                var errors = kvp.Value.Errors;
                foreach (var error in errors)
                {
                }
            }

            return BadRequest(new
            {
                ok = false,
                code = "INVALID_FORM",
                mensaje = "Los datos del formulario no son válidos."
            });
        }


        if (string.IsNullOrWhiteSpace(request.DocumentId) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Phone))
        {

            return BadRequest(new
            {
                ok = false,
                code = "MISSING_FIELDS",
                mensaje = "Todos los campos son obligatorios."
            });
        }

        if (request.CvFile == null)
        {

            return BadRequest(new
            {
                ok = false,
                code = "MISSING_FILE",
                mensaje = "Debes adjuntar tu hoja de vida."
            });
        }


        if (request.CvFile.Length == 0)
        {

            return BadRequest(new
            {
                ok = false,
                code = "MISSING_FILE",
                mensaje = "Debes adjuntar tu hoja de vida."
            });
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".doc",
            ".docx"
        };

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        var fileExtension = Path.GetExtension(request.CvFile.FileName);

        if (string.IsNullOrWhiteSpace(fileExtension) || !allowedExtensions.Contains(fileExtension))
        {

            return BadRequest(new
            {
                ok = false,
                code = "INVALID_FILE_EXTENSION",
                mensaje = "Documento inválido. Solo se permiten archivos PDF, DOC o DOCX."
            });
        }

        if (string.IsNullOrWhiteSpace(request.CvFile.ContentType) ||
            !allowedContentTypes.Contains(request.CvFile.ContentType))
        {

            return BadRequest(new
            {
                ok = false,
                code = "INVALID_FILE_TYPE",
                mensaje = "Tipo de archivo no permitido. Solo se permiten documentos PDF, DOC o DOCX."
            });
        }

        if (request.CvFile.Length > 5 * 1024 * 1024)
        {

            return BadRequest(new
            {
                ok = false,
                code = "FILE_TOO_LARGE",
                mensaje = "El archivo no puede superar los 5 MB."
            });
        }

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "cv");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var invalidChars = Path.GetInvalidFileNameChars();

        var safeFirstName = new string(request.FirstName
            .Trim()
            .Where(c => !invalidChars.Contains(c))
            .ToArray())
            .Replace(" ", "_");

        var safeLastName = new string(request.LastName
            .Trim()
            .Where(c => !invalidChars.Contains(c))
            .ToArray())
            .Replace(" ", "_");

        if (string.IsNullOrWhiteSpace(safeFirstName))
            safeFirstName = "sin_nombre";

        if (string.IsNullOrWhiteSpace(safeLastName))
            safeLastName = "sin_apellido";

        var uniqueFileName =
            $"{DateTime.UtcNow:yyyyMMddHHmmss}_{safeFirstName}_{safeLastName}_{Guid.NewGuid():N}{fileExtension}";

        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.CvFile.CopyToAsync(stream);
        }


        var relativePath = Path.Combine("uploads", "cv", uniqueFileName).Replace("\\", "/");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {

            return StatusCode(500, new
            {
                ok = false,
                code = "DATABASE_CONFIG_MISSING",
                mensaje = "No se encontró la configuración de base de datos."
            });
        }

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        var query = @"
            INSERT INTO job_applications
            (document_id, first_name, last_name, phone, cv_file_path, created_at)
            VALUES
            (@document_id, @first_name, @last_name, @phone, @cv_file_path, @created_at);
        ";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@document_id", request.DocumentId.Trim());
        command.Parameters.AddWithValue("@first_name", request.FirstName.Trim());
        command.Parameters.AddWithValue("@last_name", request.LastName.Trim());
        command.Parameters.AddWithValue("@phone", request.Phone.Trim());
        command.Parameters.AddWithValue("@cv_file_path", relativePath);
        command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();

        return Ok(new
        {
            ok = true,
            code = "SUCCESS",
            mensaje = "Postulación enviada correctamente."
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());

        return StatusCode(500, new
        {
            ok = false,
            code = "SERVER_ERROR",
            mensaje = "Ocurrió un error al procesar la postulación."
        });
    }
}
    }
}