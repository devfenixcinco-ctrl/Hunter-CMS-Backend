using MreHunter.Api.Models;
using MySqlConnector;

namespace MreHunter.Api.Services
{
    public class PlanPlusSectionService
    {
        private readonly string _connectionString;

        public PlanPlusSectionService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<PlanPlusSection>> GetAllAsync()
        {
            var sections = new List<PlanPlusSection>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM plan_plus_section ORDER BY id DESC;";

            using var cmd = new MySqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sections.Add(Map(reader));
            }

            return sections;
        }

        public async Task<PlanPlusSection?> GetByIdAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT * FROM plan_plus_section WHERE id = @id;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return Map(reader);
        }

       public async Task<int> CreateAsync(PlanPlusSection request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                INSERT INTO plan_plus_section (
                    hero_badge,
                    hero_text,
                    hero_left_image,
                    benefits_title,
                    quote_title,
                    quote_subtitle,
                    quote_card_title,
                    quote_note,
                    quote_button_text,
                    video_title_1,
                    video_description_1,
                    video_url_1,
                    video_title_2,
                    video_description_2,
                    video_url_2
                ) VALUES (
                    @hero_badge,
                    @hero_text,
                    @hero_left_image,
                    @benefits_title,
                    @quote_title,
                    @quote_subtitle,
                    @quote_card_title,
                    @quote_note,
                    @quote_button_text,
                    @video_title_1,
                    @video_description_1,
                    @video_url_1,
                    @video_title_2,
                    @video_description_2,
                    @video_url_2
                );
                SELECT LAST_INSERT_ID();
            ";

            using var cmd = new MySqlCommand(query, conn);
            AddParameters(cmd, request);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

    public async Task<bool> UpdateAsync(int id, PlanPlusSection request)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"
                UPDATE plan_plus_section SET
                    hero_badge = @hero_badge,
                    hero_text = @hero_text,
                    hero_left_image = @hero_left_image,
                    benefits_title = @benefits_title,
                    quote_title = @quote_title,
                    quote_subtitle = @quote_subtitle,
                    quote_card_title = @quote_card_title,
                    quote_note = @quote_note,
                    quote_button_text = @quote_button_text,
                    video_title_1 = @video_title_1,
                    video_description_1 = @video_description_1,
                    video_url_1 = @video_url_1,
                    video_title_2 = @video_title_2,
                    video_description_2 = @video_description_2,
                    video_url_2 = @video_url_2
                WHERE id = @id;
            ";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            AddParameters(cmd, request);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "DELETE FROM plan_plus_section WHERE id = @id;";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static void AddParameters(MySqlCommand cmd, PlanPlusSection request)
        {
            cmd.Parameters.AddWithValue("@hero_badge", request.HeroBadge);
            cmd.Parameters.AddWithValue("@hero_text", request.HeroText);
            cmd.Parameters.AddWithValue("@hero_left_image", request.HeroLeftImage);

            cmd.Parameters.AddWithValue("@benefits_title", request.BenefitsTitle);

            cmd.Parameters.AddWithValue("@quote_title", request.QuoteTitle);
            cmd.Parameters.AddWithValue("@quote_subtitle", request.QuoteSubtitle);
            cmd.Parameters.AddWithValue("@quote_card_title", request.QuoteCardTitle);
            cmd.Parameters.AddWithValue("@quote_note", request.QuoteNote);
            cmd.Parameters.AddWithValue("@quote_button_text", request.QuoteButtonText);

            cmd.Parameters.AddWithValue("@video_title_1", request.VideoTitle1);
            cmd.Parameters.AddWithValue("@video_description_1", request.VideoDescription1);
            cmd.Parameters.AddWithValue("@video_url_1", request.VideoUrl1);

            cmd.Parameters.AddWithValue("@video_title_2", request.VideoTitle2);
            cmd.Parameters.AddWithValue("@video_description_2", request.VideoDescription2);
            cmd.Parameters.AddWithValue("@video_url_2", request.VideoUrl2);
        }

       private static PlanPlusSection Map(MySqlDataReader reader)
        {
            return new PlanPlusSection
            {
                Id = reader.GetInt32("id"),

                HeroBadge = reader["hero_badge"]?.ToString(),
                HeroText = reader["hero_text"]?.ToString(),
                HeroLeftImage = reader["hero_left_image"]?.ToString(),

                BenefitsTitle = reader["benefits_title"]?.ToString(),

                QuoteTitle = reader["quote_title"]?.ToString(),
                QuoteSubtitle = reader["quote_subtitle"]?.ToString(),
                QuoteCardTitle = reader["quote_card_title"]?.ToString(),
                QuoteNote = reader["quote_note"]?.ToString(),
                QuoteButtonText = reader["quote_button_text"]?.ToString(),

                VideoTitle1 = reader["video_title_1"]?.ToString(),
                VideoDescription1 = reader["video_description_1"]?.ToString(),
                VideoUrl1 = reader["video_url_1"]?.ToString(),

                VideoTitle2 = reader["video_title_2"]?.ToString(),
                VideoDescription2 = reader["video_description_2"]?.ToString(),
                VideoUrl2 = reader["video_url_2"]?.ToString()
            };
        }
    }
}