using System.Data.SqlClient;

namespace KVHAI.Models
{
    public class DBConnect
    {
        private readonly string? connectionString;

        public DBConnect(IConfiguration configuration)
        {
            var baseConnectionString = configuration.GetConnectionString("DefaultConnection");
            var initialCatalog = "kvha1";
            //var userId = "kvhai";
            var userId = "kvhai_admin";
            var password = "katarunganvillage";

            connectionString = $"{baseConnectionString};Initial Catalog={initialCatalog};Persist Security Info=True;User ID={userId};Password={password};";

            //connectionString = configuration.GetConnectionString("DefaultConnection");// + $"Initial Catalog=kvha1;User ID=kvhai;Password=1234;";
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var connection = GetConnection();
            await connection.OpenAsync();
            return connection;
        }

    }
}
