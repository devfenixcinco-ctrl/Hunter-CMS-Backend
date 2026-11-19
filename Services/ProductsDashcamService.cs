using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MreHunter.Api.Infrastructure;

namespace MreHunter.Api.Services
{
    public class ProductsDashcamService
    {
        public bool IsInvalidString(string? value)
            => string.IsNullOrWhiteSpace(value);

        public object NormalizeValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        public bool IsValidJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> SaveImageAsync(IFormFile? file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folderName
            );

            // Se guarda siempre en WebP y con 1920 px de ancho como máximo (ver ImageOptimizer).
            var fileName = await ImageOptimizer.SaveAsWebpAsync(file, uploadsFolder, Guid.NewGuid().ToString());

            return $"/uploads/{folderName}/{fileName}";
        }
    }
}