using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class RequestDetailsRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public RequestDetailsRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
        }

        public async Task<int> GetRequestID(string addresID, string residentID)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT request_id FROM request_tb WHERE addr_id = @aid AND res_id = @rid", connection))
                    {
                        command.Parameters.AddWithValue("@aid", addresID);
                        command.Parameters.AddWithValue("@rid", residentID);

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<Address>> GetPendingRemovalRequests()
        {
            var pendingAddresses = new List<Address>();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT * FROM address_tb WHERE remove_request_token IS NOT NULL AND remove_token_date IS NOT NULL  ORDER BY remove_token_date DESC
                        ", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var address = new Address
                                {
                                    Address_ID = Convert.ToInt32(reader["addr_id"].ToString()),
                                    Resident_ID = Convert.ToInt32(reader["res_id"].ToString()),
                                    Block = reader["block"].ToString() ?? String.Empty,
                                    Lot = reader["lot"].ToString() ?? String.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString()),
                                    Remove_Request_Token = reader["remove_request_token"].ToString() ?? string.Empty,
                                    Remove_Token_Date = reader["remove_token_date"].ToString() ?? string.Empty,
                                };
                                pendingAddresses.Add(address);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception and handle it
                Console.WriteLine(ex.Message);
            }

            return pendingAddresses;
        }

    }
}
