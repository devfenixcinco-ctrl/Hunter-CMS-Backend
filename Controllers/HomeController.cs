using Microsoft.AspNetCore.Mvc;
using MreHunter.Api.Data;
using MreHunter.Api.Models;
using MySqlConnector;
using System.Text.Json;

namespace MreHunter.Api.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly Db _db;

        public HomeController(Db db)
        {
            _db = db;
        }

        // Helper: leer string nullable
        private static string? ReadNullableString(MySqlDataReader reader, string col)
            => reader[col] == DBNull.Value ? null : reader[col].ToString();

        // Helper: leer int nullable
        private static int? ReadNullableInt(MySqlDataReader reader, string col)
            => reader[col] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[col]);

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM home_info ORDER BY id DESC LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { message = "No hay configuración de Home aún" });

            var countriesJson = ReadNullableString(reader, "countries");

            List<string>? countries = null;

            if (!string.IsNullOrWhiteSpace(countriesJson))
            {
                try
                {
                    countries = JsonSerializer.Deserialize<List<string>>(countriesJson);
                }
                catch
                {
                    countries = null;
                }
            }

            var home = new HomeInfo
            {
                Id = reader.GetInt32("id"),

                NumbersSlides = ReadNullableInt(reader, "number_slider"),
                BannerTitle = ReadNullableString(reader, "banner_title"),
                BannerText = ReadNullableString(reader, "banner_text"),
                BannerUrl2 = ReadNullableString(reader, "banner_url_2"),
                BannerUrl3 = ReadNullableString(reader, "banner_url_3"),
                BannerUrl4 = ReadNullableString(reader, "banner_url_4"),
                BannerButtonURL = ReadNullableString(reader, "banner_button_url"),
                VideoUrl = ReadNullableString(reader, "videoUrl"),


                FirstCardTitle = ReadNullableString(reader, "first_card_title"),
                FirstCardText = ReadNullableString(reader, "first_card_text"),

                NumbersTitle = ReadNullableString(reader, "numbers_title"),

                YearsExperience = ReadNullableInt(reader, "years_experience"),
                ActiveClients = ReadNullableInt(reader, "active_clients"),
                Recoveries = ReadNullableInt(reader, "recoveries"),
                Captures = ReadNullableInt(reader, "captures"),

                Countries = countries,

                SecondBanner = ReadNullableString(reader, "second_banner"),
                SecondBannerText = ReadNullableString(reader, "second_banner_text"),
                SecondBannerNumber = ReadNullableString(reader, "second_banner_number"),

                ThirdBanner = ReadNullableString(reader, "third_banner"),
                FourthBanner = ReadNullableString(reader, "fourth_banner"),

                FourthBannerTitle = ReadNullableString(reader, "fourth_banner_title"),
                FourthBannerText = ReadNullableString(reader, "fourth_banner_text"),

                ThirdBannerText = ReadNullableString(reader, "third_banner_text"),
                ThirdBannerHighlight = ReadNullableString(reader, "third_banner_highlight"),
                ThirdBannerEmphasis = ReadNullableString(reader, "third_banner_emphasis"),

                CreatedAt = reader.GetDateTime("created_at")
            };

            return Ok(home);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] HomeUpdateRequest req)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var getIdCmd = new MySqlCommand("SELECT id FROM home_info ORDER BY id DESC LIMIT 1;", conn);
            var existingIdObj = await getIdCmd.ExecuteScalarAsync();

            if (existingIdObj != null)
            {
                int id = Convert.ToInt32(existingIdObj);

                var updateSql = @"
                    UPDATE home_info SET
                        number_slider=@number_slider,
                        banner_title=@banner_title,
                        banner_text=@banner_text,
                        banner_url_2=@banner_url_2,
                        banner_url_3=@banner_url_3,
                        banner_url_4=@banner_url_4,
                        videoUrl=@videoUrl,
                        first_card_title=@first_card_title,
                        first_card_text=@first_card_text,
                        numbers_title=@numbers_title,
                        years_experience=@years_experience,
                        active_clients=@active_clients,
                        recoveries=@recoveries,
                        captures=@captures,
                        countries=@countries,
                        second_banner=@second_banner,
                        second_banner_text=@second_banner_text,
                        second_banner_number=@second_banner_number,
                        third_banner=@third_banner,
                        fourth_banner=@fourth_banner,
                        fourth_banner_title=@fourth_banner_title,
                        fourth_banner_text=@fourth_banner_text,
                        third_banner_text=@third_banner_text,
                        third_banner_highlight=@third_banner_highlight,
                        third_banner_emphasis=@third_banner_emphasis
                    WHERE id=@id;";

                using var cmd = new MySqlCommand(updateSql, conn);

                cmd.Parameters.AddWithValue("@number_slider", (object?)req.NumbersSlides ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@banner_url_2", (object?)req.BannerUrl2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@banner_url_3", (object?)req.BannerUrl3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@banner_url_4", (object?)req.BannerUrl4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@videoUrl", (object?)req.VideoUrl ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@first_card_title", (object?)req.FirstCardTitle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@first_card_text", (object?)req.FirstCardText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numbers_title", (object?)req.NumbersTitle ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@years_experience", (object?)req.YearsExperience ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@active_clients", (object?)req.ActiveClients ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@recoveries", (object?)req.Recoveries ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@captures", (object?)req.Captures ?? DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@countries",
                    req.Countries != null
                        ? JsonSerializer.Serialize(req.Countries)
                        : DBNull.Value
                );

                cmd.Parameters.AddWithValue("@second_banner", (object?)req.SecondBanner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@second_banner_text", (object?)req.SecondBannerText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@second_banner_number", (object?)req.SecondBannerNumber ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@third_banner", (object?)req.ThirdBanner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fourth_banner", (object?)req.FourthBanner ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fourth_banner_title", (object?)req.FourthBannerTitle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fourth_banner_text", (object?)req.FourthBannerText ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@third_banner_text", (object?)req.ThirdBannerText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@third_banner_highlight", (object?)req.ThirdBannerHighlight ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@third_banner_emphasis", (object?)req.ThirdBannerEmphasis ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@banner_button_url", (object?)req.BannerButtonURL ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "Home actualizado (POST)", created = false, id });
            }

            var insertSql = @"
                INSERT INTO home_info
                (
                    number_slider,
                    banner_title,
                    banner_text,
                    banner_url_2,
                    banner_url_3,
                    banner_url_4,
                    banner_button_url,
                    videoUrl,
                    first_card_title,
                    first_card_text,
                    numbers_title,
                    years_experience,
                    active_clients,
                    recoveries,
                    captures,
                    countries,
                    second_banner,
                    second_banner_text,
                    second_banner_number,
                    third_banner,
                    fourth_banner,
                    fourth_banner_title,
                    fourth_banner_text,
                    third_banner_text,
                    third_banner_highlight,
                    third_banner_emphasis
                )
                VALUES
                (
                    @banner_title,
                    @banner_text,
                    @banner_url_2,
                    @banner_url_3,
                    @banner_url_4,
                    @banner_button_url,
                    @videoUrl,
                    @first_card_title,
                    @first_card_text,
                    @numbers_title,
                    @years_experience,
                    @active_clients,
                    @recoveries,
                    @captures,
                    @countries,
                    @second_banner,
                    @second_banner_text,
                    @second_banner_number,
                    @third_banner,
                    @fourth_banner,
                    @fourth_banner_title,
                    @fourth_banner_text,
                    @third_banner_text,
                    @third_banner_highlight,
                    @third_banner_emphasis
                );
                SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);

            insertCmd.Parameters.AddWithValue("@number_slider", (object?)req.NumbersSlides ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@banner_title", (object?)req.BannerTitle ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@banner_text", (object?)req.BannerText ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@banner_url_2", (object?)req.BannerUrl2 ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@banner_url_3", (object?)req.BannerUrl3 ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@banner_url_4", (object?)req.BannerUrl4 ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@videoUrl", (object?)req.VideoUrl ?? DBNull.Value);

            insertCmd.Parameters.AddWithValue("@first_card_title", (object?)req.FirstCardTitle ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@first_card_text", (object?)req.FirstCardText ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@numbers_title", (object?)req.NumbersTitle ?? DBNull.Value);

            insertCmd.Parameters.AddWithValue("@years_experience", (object?)req.YearsExperience ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@active_clients", (object?)req.ActiveClients ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@recoveries", (object?)req.Recoveries ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@captures", (object?)req.Captures ?? DBNull.Value);

            insertCmd.Parameters.AddWithValue(
                "@countries",
                req.Countries != null
                    ? JsonSerializer.Serialize(req.Countries)
                    : DBNull.Value
            );

            insertCmd.Parameters.AddWithValue("@second_banner", (object?)req.SecondBanner ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@second_banner_text", (object?)req.SecondBannerText ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@second_banner_number", (object?)req.SecondBannerNumber ?? DBNull.Value);

            insertCmd.Parameters.AddWithValue("@third_banner", (object?)req.ThirdBanner ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@fourth_banner", (object?)req.FourthBanner ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@fourth_banner_title", (object?)req.FourthBannerTitle ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@fourth_banner_text", (object?)req.FourthBannerText ?? DBNull.Value);

            insertCmd.Parameters.AddWithValue("@third_banner_text", (object?)req.ThirdBannerText ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@third_banner_highlight", (object?)req.ThirdBannerHighlight ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@third_banner_emphasis", (object?)req.ThirdBannerEmphasis ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@banner_button_url", (object?)req.BannerButtonURL ?? DBNull.Value);    

            int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            return Ok(new { message = "Home creado (POST)", created = true, id = newId });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] HomeUpdateRequest req)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"
                UPDATE home_info SET
                    number_slider=@number_slider,
                    banner_title=@banner_title,
                    banner_text=@banner_text,
                    banner_url_2=@banner_url_2,
                    banner_url_3=@banner_url_3,
                    banner_url_4=@banner_url_4,
                    banner_button_url=@banner_button_url,
                    videoUrl=@videoUrl,
                    first_card_title=@first_card_title,
                    first_card_text=@first_card_text,
                    numbers_title=@numbers_title,
                    years_experience=@years_experience,
                    active_clients=@active_clients,
                    recoveries=@recoveries,
                    captures=@captures,
                    countries=@countries,
                    second_banner=@second_banner,
                    second_banner_text=@second_banner_text,
                    second_banner_number=@second_banner_number,
                    third_banner=@third_banner,
                    fourth_banner=@fourth_banner,
                    fourth_banner_title=@fourth_banner_title,
                    fourth_banner_text=@fourth_banner_text,
                    third_banner_text=@third_banner_text,
                    third_banner_highlight=@third_banner_highlight,
                    third_banner_emphasis=@third_banner_emphasis
                WHERE id=1;";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@number_slider", (object?)req.NumbersSlides ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_title", (object?)req.BannerTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_text", (object?)req.BannerText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_url_2", (object?)req.BannerUrl2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_url_3", (object?)req.BannerUrl3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_url_4", (object?)req.BannerUrl4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@banner_button_url", (object?)req.BannerButtonURL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@videoUrl", (object?)req.VideoUrl ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@first_card_title", (object?)req.FirstCardTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@first_card_text", (object?)req.FirstCardText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@numbers_title", (object?)req.NumbersTitle ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@years_experience", (object?)req.YearsExperience ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@active_clients", (object?)req.ActiveClients ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@recoveries", (object?)req.Recoveries ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@captures", (object?)req.Captures ?? DBNull.Value);

            cmd.Parameters.AddWithValue(
                "@countries",
                req.Countries != null
                    ? JsonSerializer.Serialize(req.Countries)
                    : DBNull.Value
            );

            cmd.Parameters.AddWithValue("@second_banner", (object?)req.SecondBanner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@second_banner_text", (object?)req.SecondBannerText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@second_banner_number", (object?)req.SecondBannerNumber ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@third_banner", (object?)req.ThirdBanner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fourth_banner", (object?)req.FourthBanner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fourth_banner_title", (object?)req.FourthBannerTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fourth_banner_text", (object?)req.FourthBannerText ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@third_banner_text", (object?)req.ThirdBannerText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@third_banner_highlight", (object?)req.ThirdBannerHighlight ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@third_banner_emphasis", (object?)req.ThirdBannerEmphasis ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Home actualizado (PUT)" });
        }
    }
}