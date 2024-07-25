using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class AddressRepository
    {
        private readonly DBConnect _dbConnect;

        public AddressRepository(DBConnect dBConnect)
        {
            _dbConnect = dBConnect;
        }

        public async Task<int> CreateAddress(int res_id, int st_id, SqlTransaction transaction, SqlConnection connection)
        {
            try
            {
                using (var command = new SqlCommand("INSERT INTO address_tb (res_id,st_id) VALUES(@res,@st)", connection, transaction))
                {
                    command.Parameters.AddWithValue("@res", res_id);
                    command.Parameters.AddWithValue("@st", st_id);

                    await command.ExecuteNonQueryAsync();

                    return 1;
                }
            }
            catch (Exception)
            {
                return 0;
            }

        }
    }
}
