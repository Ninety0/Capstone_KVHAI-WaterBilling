using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class RequestDetailsRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;
        private readonly StreetRepository _streetRepository;

        public RequestDetailsRepository(DBConnect dBConnect, InputSanitize inputSanitize, StreetRepository streetRepository)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _streetRepository = streetRepository;
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

        public async Task<List<RequestDetails>> GetPendingRemovalRequests()
        {
            var pendingAddresses = new List<RequestDetails>();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT * FROM request_tb
                        ", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var addressList = await GetAddressById(reader["addr_id"].ToString() ?? string.Empty);
                                var name = await GetNameByID(reader["res_id"].ToString() ?? string.Empty);
                                var address = "";

                                foreach (var item in addressList)
                                {
                                    address = $"Blk {item.Block} Lot {item.Lot} {item.Street_Name} Street";
                                }

                                var request = new RequestDetails
                                {
                                    Request_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Address_ID = reader.GetInt32(2),
                                    RequestType = reader["request_type"].ToString() ?? string.Empty,
                                    DateCreated = reader.GetDateTime(4),
                                    Status = reader.GetInt32(5),
                                    StatusUpdated = reader.GetDateTime(6),
                                    Comments = reader["comments"].ToString() ?? string.Empty,
                                    Resident_Name = name,
                                    Address_Name = address
                                };
                                pendingAddresses.Add(request);
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

        private async Task<List<Address>> GetAddressById(string adddressID)
        {
            try
            {
                var addressList = new List<Address>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT * FROM address_tb WHERE is_verified = 'true'AND addr_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", adddressID);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int addr_id = Convert.ToInt32(reader["addr_id"].ToString());
                                int res_id = Convert.ToInt32(reader["res_id"].ToString());

                                var address = new Address
                                {
                                    Address_ID = addr_id,
                                    Resident_ID = res_id,
                                    Block = reader["block"].ToString() ?? String.Empty,
                                    Lot = reader["lot"].ToString() ?? String.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString()),
                                    Remove_Request_Token = reader["remove_request_token"].ToString() ?? string.Empty,
                                    Street_Name = await _streetRepository.GetStreetName(reader["st_id"].ToString())
                                };

                                addressList.Add(address);
                            }
                        }
                        return addressList;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        //GET NAME BY ID
        private async Task<string> GetNameByID(string residentID)
        {
            try
            {
                var name = "";
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password
                    using (var command = new SqlCommand("SELECT lname,fname,mname FROM resident_tb WHERE res_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", residentID);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var lname = reader["lname"].ToString() ?? String.Empty;
                                var fname = reader["fname"].ToString() ?? String.Empty;
                                var mname = reader["mname"].ToString() ?? String.Empty;

                                name = string.Join(", ", lname, fname, mname);
                            }
                            return name;
                        }

                    }
                }
            }
            catch (Exception)
            {
                // Handle exception (logging, etc.)
                return null;
            }
        }
    }
}
