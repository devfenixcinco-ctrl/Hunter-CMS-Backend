using MySqlConnector;
using MreHunter.Api.Models;

namespace MreHunter.Api.Services
{
    public class ProductsService
    {
        private readonly IConfiguration _configuration;

        public ProductsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<ProductsPageResponse?> GetProductsPageAsync()
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            ProductsPage? page = null;
            var helpCards = new List<ProductHelpCard>();
            var offices = new List<ProductOffice>();

            var pageQuery = @"SELECT * 
                              FROM products_page 
                              ORDER BY id ASC 
                              LIMIT 1;";

            using (var command = new MySqlCommand(pageQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    page = new ProductsPage
                    {
                        Id = reader.GetInt32("id"),
                        HeroTitle = reader.GetString("hero_title"),
                        HeroButtonText = reader.GetString("hero_button_text"),
                        HeroButtonLink = reader.IsDBNull(reader.GetOrdinal("hero_button_link")) ? null : reader.GetString("hero_button_link"),
                        HeroImageUrl = reader.IsDBNull(reader.GetOrdinal("hero_image_url")) ? null : reader.GetString("hero_image_url"),
                        HelpTitle = reader.GetString("help_title"),
                        HelpSubtitle = reader.IsDBNull(reader.GetOrdinal("help_subtitle")) ? null : reader.GetString("help_subtitle"),
                        OfficesTitle = reader.GetString("offices_title"),
                        CoverageTitle = reader.GetString("coverage_title"),
                        CoverageSubtitle = reader.IsDBNull(reader.GetOrdinal("coverage_subtitle")) ? null : reader.GetString("coverage_subtitle"),
                        CoverageButtonText = reader.GetString("coverage_button_text"),
                        CoverageButtonLink = reader.IsDBNull(reader.GetOrdinal("coverage_button_link")) ? null : reader.GetString("coverage_button_link"),
                        CoverageImageUrl = reader.IsDBNull(reader.GetOrdinal("coverage_image_url")) ? null : reader.GetString("coverage_image_url"),
                        CreatedAt = reader.GetDateTime("created_at"),
                        UpdatedAt = reader.GetDateTime("updated_at")
                    };
                }
            }

            if (page == null)
                return null;

            var helpCardsQuery = @"SELECT * 
                                   FROM product_help_cards 
                                   WHERE products_page_id = @ProductsPageId
                                   ORDER BY sort_order ASC, id ASC;";

            using (var command = new MySqlCommand(helpCardsQuery, connection))
            {
                command.Parameters.AddWithValue("@ProductsPageId", page.Id);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    helpCards.Add(new ProductHelpCard
                    {
                        Id = reader.GetInt32("id"),
                        ProductsPageId = reader.GetInt32("products_page_id"),
                        Icon = reader.IsDBNull(reader.GetOrdinal("icon")) ? null : reader.GetString("icon"),
                        Title = reader.GetString("title"),
                        Subtitle = reader.IsDBNull(reader.GetOrdinal("subtitle")) ? null : reader.GetString("subtitle"),
                        ButtonText = reader.GetString("button_text"),
                        ButtonLink = reader.IsDBNull(reader.GetOrdinal("button_link")) ? null : reader.GetString("button_link"),
                        SortOrder = reader.GetInt32("sort_order"),
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }
            }

            var officesQuery = @"SELECT * 
                                 FROM product_offices 
                                 WHERE products_page_id = @ProductsPageId
                                 ORDER BY sort_order ASC, id ASC;";

            using (var command = new MySqlCommand(officesQuery, connection))
            {
                command.Parameters.AddWithValue("@ProductsPageId", page.Id);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    offices.Add(new ProductOffice
                    {
                        Id = reader.GetInt32("id"),
                        ProductsPageId = reader.GetInt32("products_page_id"),
                        OfficeName = reader.GetString("office_name"),
                        Address = reader.GetString("address"),
                        MapUrl = reader.IsDBNull(reader.GetOrdinal("map_url")) ? null : reader.GetString("map_url"),
                        SortOrder = reader.GetInt32("sort_order"),
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }
            }

            return new ProductsPageResponse
            {
                Page = page,
                HelpCards = helpCards,
                Offices = offices
            };
        }

     public async Task<int> CreateProductsPageAsync(ProductsPage request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO products_page
                          (hero_title, hero_button_text, hero_button_link, hero_image_url,
                           help_title, help_subtitle, offices_title,
                           coverage_title, coverage_subtitle, coverage_button_text,
                           coverage_button_link, coverage_image_url)
                          VALUES
                          (@HeroTitle, @HeroButtonText, @HeroButtonLink, @HeroImageUrl,
                           @HelpTitle, @HelpSubtitle, @OfficesTitle,
                           @CoverageTitle, @CoverageSubtitle, @CoverageButtonText,
                           @CoverageButtonLink, @CoverageImageUrl);
                          SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroButtonText", request.HeroButtonText);
            command.Parameters.AddWithValue("@HeroButtonLink", (object?)request.HeroButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)request.HeroImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@HelpTitle", request.HelpTitle);
            command.Parameters.AddWithValue("@HelpSubtitle", (object?)request.HelpSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@OfficesTitle", request.OfficesTitle);
            command.Parameters.AddWithValue("@CoverageTitle", request.CoverageTitle);
            command.Parameters.AddWithValue("@CoverageSubtitle", (object?)request.CoverageSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverageButtonText", request.CoverageButtonText);
            command.Parameters.AddWithValue("@CoverageButtonLink", (object?)request.CoverageButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverageImageUrl", (object?)request.CoverageImageUrl ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateProductsPageAsync(int id, ProductsPage request, string scheme, string host)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE products_page SET
                            hero_title = @HeroTitle,
                            hero_button_text = @HeroButtonText,
                            hero_button_link = @HeroButtonLink,
                            hero_image_url = @HeroImageUrl,
                            help_title = @HelpTitle,
                            help_subtitle = @HelpSubtitle,
                            offices_title = @OfficesTitle,
                            coverage_title = @CoverageTitle,
                            coverage_subtitle = @CoverageSubtitle,
                            coverage_button_text = @CoverageButtonText,
                            coverage_button_link = @CoverageButtonLink,
                            coverage_image_url = @CoverageImageUrl
                          WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@HeroTitle", request.HeroTitle);
            command.Parameters.AddWithValue("@HeroButtonText", request.HeroButtonText);
            command.Parameters.AddWithValue("@HeroButtonLink", (object?)request.HeroButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@HeroImageUrl", (object?)request.HeroImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@HelpTitle", request.HelpTitle);
            command.Parameters.AddWithValue("@HelpSubtitle", (object?)request.HelpSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@OfficesTitle", request.OfficesTitle);
            command.Parameters.AddWithValue("@CoverageTitle", request.CoverageTitle);
            command.Parameters.AddWithValue("@CoverageSubtitle", (object?)request.CoverageSubtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverageButtonText", request.CoverageButtonText);
            command.Parameters.AddWithValue("@CoverageButtonLink", (object?)request.CoverageButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverageImageUrl", (object?)request.CoverageImageUrl ?? DBNull.Value);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> CreateHelpCardAsync(ProductHelpCard request)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO product_help_cards
                          (products_page_id, icon, title, subtitle, button_text, button_link, sort_order)
                          VALUES
                          (@ProductsPageId, @Icon, @Title, @Subtitle, @ButtonText, @ButtonLink, @SortOrder);
                          SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductsPageId", request.ProductsPageId);
            command.Parameters.AddWithValue("@Icon", (object?)request.Icon ?? DBNull.Value);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@Subtitle", (object?)request.Subtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@ButtonText", request.ButtonText);
            command.Parameters.AddWithValue("@ButtonLink", (object?)request.ButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@SortOrder", request.SortOrder);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateHelpCardAsync(int id, ProductHelpCard request)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE product_help_cards SET
                            products_page_id = @ProductsPageId,
                            icon = @Icon,
                            title = @Title,
                            subtitle = @Subtitle,
                            button_text = @ButtonText,
                            button_link = @ButtonLink,
                            sort_order = @SortOrder
                          WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@ProductsPageId", request.ProductsPageId);
            command.Parameters.AddWithValue("@Icon", (object?)request.Icon ?? DBNull.Value);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@Subtitle", (object?)request.Subtitle ?? DBNull.Value);
            command.Parameters.AddWithValue("@ButtonText", request.ButtonText);
            command.Parameters.AddWithValue("@ButtonLink", (object?)request.ButtonLink ?? DBNull.Value);
            command.Parameters.AddWithValue("@SortOrder", request.SortOrder);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteHelpCardAsync(int id)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"DELETE FROM product_help_cards WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> CreateOfficeAsync(ProductOffice request)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"INSERT INTO product_offices
                          (products_page_id, office_name, address, map_url, sort_order)
                          VALUES
                          (@ProductsPageId, @OfficeName, @Address, @MapUrl, @SortOrder);
                          SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProductsPageId", request.ProductsPageId);
            command.Parameters.AddWithValue("@OfficeName", request.OfficeName);
            command.Parameters.AddWithValue("@Address", request.Address);
            command.Parameters.AddWithValue("@MapUrl", (object?)request.MapUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@SortOrder", request.SortOrder);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateOfficeAsync(int id, ProductOffice request)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"UPDATE product_offices SET
                            products_page_id = @ProductsPageId,
                            office_name = @OfficeName,
                            address = @Address,
                            map_url = @MapUrl,
                            sort_order = @SortOrder
                          WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@ProductsPageId", request.ProductsPageId);
            command.Parameters.AddWithValue("@OfficeName", request.OfficeName);
            command.Parameters.AddWithValue("@Address", request.Address);
            command.Parameters.AddWithValue("@MapUrl", (object?)request.MapUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@SortOrder", request.SortOrder);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteOfficeAsync(int id)
        {
            var connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"DELETE FROM product_offices WHERE id = @Id;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}