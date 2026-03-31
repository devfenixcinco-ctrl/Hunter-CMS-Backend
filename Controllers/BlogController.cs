using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        private readonly Db _db;

        public BlogController(Db db)
        {
            _db = db;
        }

        private static string? ReadNullableString(MySqlDataReader r, string c)
            => r[c] == DBNull.Value ? null : r[c].ToString();

        private static DateTime? ReadNullableDateTime(MySqlDataReader r, string c)
            => r[c] == DBNull.Value ? (DateTime?)null : r.GetDateTime(c);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = new List<BlogPost>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT id, image_url, title, content, publish_date, created_at
                FROM blog_posts
                ORDER BY created_at DESC;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new BlogPost
                {
                    Id = reader.GetInt32("id"),
                    ImageUrl = ReadNullableString(reader, "image_url"),
                    Title = ReadNullableString(reader, "title"),
                    Text = ReadNullableString(reader, "content"),
                    PublishDate = ReadNullableDateTime(reader, "publish_date"),
                    CreatedAt = reader.GetDateTime("created_at")
                });
            }

            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string sql = @"
                SELECT id, image_url, title, content, publish_date, created_at
                FROM blog_posts
                WHERE id = @id
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { message = "Post no encontrado" });

            var post = new BlogPost
            {
                Id = reader.GetInt32("id"),
                ImageUrl = ReadNullableString(reader, "image_url"),
                Title = ReadNullableString(reader, "title"),
                Text = ReadNullableString(reader, "content"),
                PublishDate = ReadNullableDateTime(reader, "publish_date"),
                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(post);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] BlogCreateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { message = "Title es obligatorio" });

            string? imageUrl = null;

            if (req.Image != null)
            {
                imageUrl = await SaveImage(req.Image, "blog", "blog_" + Guid.NewGuid());
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string sql = @"
                INSERT INTO blog_posts (image_url, title, content, publish_date)
                VALUES (@img, @title, @content, @date);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@img", (object?)imageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", (object?)req.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@content", (object?)req.Text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@date", (object?)req.PublishDate ?? DBNull.Value);

            int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return Ok(new { message = "Post creado", id = newId, imageUrl });
        }

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] BlogUpdateRequest req)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string? imageUrl = null;

            using (var getCurrent = new MySqlCommand("SELECT image_url FROM blog_posts WHERE id = @id LIMIT 1;", conn))
            {
                getCurrent.Parameters.AddWithValue("@id", id);
                var currentObj = await getCurrent.ExecuteScalarAsync();

                if (currentObj == null)
                    return NotFound(new { message = "Post no encontrado" });

                imageUrl = currentObj == DBNull.Value ? null : currentObj.ToString();
            }

            if (req.Image != null)
            {
                imageUrl = await SaveImage(req.Image, "blog", "blog_" + id);
            }

            string sql = @"
                UPDATE blog_posts
                SET image_url = @img,
                    title = @title,
                    content = @content,
                    publish_date = @date
                WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@img", (object?)imageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", (object?)req.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@content", (object?)req.Text ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@date", (object?)req.PublishDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);

            int affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0)
                return NotFound(new { message = "Post no encontrado" });

            return Ok(new { message = "Post actualizado", imageUrl });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string sql = @"DELETE FROM blog_posts WHERE id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            int affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0)
                return NotFound(new { message = "Post no encontrado" });

            return Ok(new { message = "Post eliminado" });
        }

        private async Task<string> SaveImage(IFormFile file, string folder, string fileNameWithoutExtension)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No se recibió ningún archivo.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new Exception("Formato no permitido. Solo jpg, jpeg, png y webp.");

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                folder
            );

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var ext in allowedExtensions)
            {
                var oldFile = Path.Combine(uploadsFolder, fileNameWithoutExtension + ext);
                if (System.IO.File.Exists(oldFile))
                    System.IO.File.Delete(oldFile);
            }

            var finalFileName = fileNameWithoutExtension + extension;
            var filePath = Path.Combine(uploadsFolder, finalFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/uploads/{folder}/{finalFileName}";
        }
    }
}