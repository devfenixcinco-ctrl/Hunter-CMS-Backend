using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MreHunter.Api.Infrastructure
{
    public sealed class UploadValidationException : Exception
    {
        public UploadValidationException(string message) : base(message) { }
    }

    public static class UploadValidation
    {
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public const long MaxImageBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Devuelve la extensión normalizada si el archivo es una imagen permitida; si no, lanza
        /// <see cref="UploadValidationException"/> (que se responde como 400).
        /// Evita subir .html/.svg/.js a wwwroot (XSS almacenado en el dominio de la API).
        /// </summary>
        public static string EnsureImage(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedImageExtensions.Contains(extension))
                throw new UploadValidationException("Formato no permitido. Solo jpg, jpeg, png y webp.");

            if (file.Length > MaxImageBytes)
                throw new UploadValidationException("La imagen supera el tamaño máximo de 5 MB.");

            return extension;
        }
    }

    /// <summary>Convierte un archivo inválido en un 400 con mensaje, en lugar de un 500.</summary>
    public sealed class UploadValidationExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is UploadValidationException ex)
            {
                context.Result = new BadRequestObjectResult(new { message = ex.Message });
                context.ExceptionHandled = true;
            }
        }
    }
}
