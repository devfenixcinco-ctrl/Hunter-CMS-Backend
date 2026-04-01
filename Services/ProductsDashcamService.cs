using System.Text.Json;
using Microsoft.AspNetCore.Http;

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

            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{folderName}/{fileName}";
        }
    }
}