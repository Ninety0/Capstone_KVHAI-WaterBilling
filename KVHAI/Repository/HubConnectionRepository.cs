using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class HubConnectionRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public HubConnectionRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
        }

        public async Task SaveHubConnection(HubConnect hubConnect)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO hubconnection_tb (connection_id, resident_id,username) VALUES(@connection, @res_id, @username)", connection))
                    {
                        command.Parameters.AddWithValue("@connection", hubConnect.Connection_ID);
                        command.Parameters.AddWithValue("@res_id", hubConnect.Resident_ID);
                        command.Parameters.AddWithValue("@username", hubConnect.Username);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task RemoveHubConnection(string connection_id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM hubconnection_tb WHERE connection_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", connection_id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task<List<HubConnect>> SelectHubConnection(string res_id)
        {
            try
            {
                var hubConnectionList = new List<HubConnect>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT * FROM hubconnection_tb WHERE resident_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", res_id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                hubConnectionList.Add(new HubConnect
                                {
                                    Connection_ID = reader["connection_id"].ToString(),
                                });
                            }
                        }
                    }
                }
                return hubConnectionList;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
