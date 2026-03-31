using MySqlConnector;

namespace MreHunter.Api.Data
{
    public class Db
    {
        private readonly IConfiguration _config;

        public Db(IConfiguration config)
        {
            _config = config;
        }

        public MySqlConnection GetConnection()
        {
            var cs = _config.GetConnectionString("DefaultConnection");
            return new MySqlConnection(cs);
        }
    }
}
