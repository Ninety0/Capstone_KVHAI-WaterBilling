using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class ResidentAddressRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public ResidentAddressRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
        }

        public async Task<List<ResidentAddress>> GetRentalApplication(string resident_id = "")
        {
            try
            {
                /// STATUS -  0 (Pending), 1 (Accepted), 2 (Rejected)
                var addressRequest = new List<ResidentAddress>();
                string query = string.IsNullOrEmpty(resident_id) ?
                    "Select * From resident_address_tb WHERE is_owner = 'false'" :
                    "Select * From resident_address_tb WHERE res_id = @res AND is_owner = 'false'";
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(resident_id))
                        {
                            command.Parameters.AddWithValue("res", resident_id);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var residentAddress = new ResidentAddress
                                {
                                    Resident_Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Address_ID = reader.GetInt32(2),
                                    Is_Owner = reader.GetInt32(3),
                                    Status = reader.GetInt32(4),
                                    Request_Date = reader.GetDateTime(5),
                                    Decision_Date = reader.GetDateTime(6)
                                };

                                addressRequest.Add(residentAddress);
                            }
                        }
                    }
                }
                return addressRequest;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public async Task<List<ResidentAddress>> GetRentalApplicationForRenter(string resident_id = "")
        {
            try
            {
                /// STATUS -  0 (Pending), 1 (Accepted), 2 (Rejected)
                var addressRequest = new List<ResidentAddress>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    Select resident_addr_id, ra.res_id,a.addr_id ,CONCAT(r.lname,', ',r.fname,', ',r.mname)as Tenant, a.block,a.lot,s.st_name, is_owner, ra.status, ra.request_date, ra.decision_date from address_tb a
                    JOIN street_tb s ON a.st_id = s.st_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    JOIN resident_address_tb ra ON a.addr_id = ra.addr_id
                    WHERE ra.res_id = @res AND is_owner = 'false'
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@res", resident_id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var residentAddress = new ResidentAddress
                                {
                                    Resident_Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Address_ID = reader.GetInt32(2),
                                    Name = reader.GetString(3),
                                    Block = reader.GetString(4),
                                    Lot = reader.GetString(5),
                                    Street = reader.GetString(6),
                                    Is_Owner = reader.GetInt32(7),
                                    Status = reader.GetInt32(8),
                                    Request_Date = reader.GetDateTime(9),
                                };

                                addressRequest.Add(residentAddress);
                            }
                        }
                    }
                }
                return addressRequest;
            }
            catch (Exception)
            {

                return null;
            }
        }

        //GET NAME BY BLOCK AND LOT
        public async Task<List<ResidentAddress>> GetName(ResidentAddress residentAddress)
        {
            try
            {
                var resident = new List<ResidentAddress>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
            SELECT 
	            COALESCE((
			            SELECT CONCAT(r.lname,', ',r.fname,', ',r.mname)
			            FROM resident_tb r
			            WHERE r.res_id = ra.res_id 
			
		            ), 'No name') as full_name,
                a.*,
                ra.*, 
                s.*,
                COALESCE((
                    SELECT SUM(CAST(amount as decimal(10,2)))
                    FROM water_billing_tb wb
                    WHERE wb.addr_id = a.addr_id 
                    AND wb.status = 'unpaid'
                ), 0) as total_unpaid_amount
            FROM resident_address_tb ra
            JOIN address_tb a ON ra.addr_id = a.addr_id
            JOIN street_tb s ON a.st_id = s.st_id
            WHERE is_owner = @owner 
            AND a.block = @blk 
            AND a.lot = @lot 
            AND st_name LIKE @st", connection))
                    {
                        command.Parameters.AddWithValue("@owner", string.IsNullOrEmpty(residentAddress.Is_Owner.ToString()) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Is_Owner.ToString()));
                        command.Parameters.AddWithValue("@blk", string.IsNullOrEmpty(residentAddress.Block) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Block));
                        command.Parameters.AddWithValue("@lot", string.IsNullOrEmpty(residentAddress.Lot) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Lot));
                        command.Parameters.AddWithValue("@st", string.IsNullOrEmpty(residentAddress.Street) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Street));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var bill = Convert.ToDecimal(reader["total_unpaid_amount"]).ToString("F2");
                                var bills = Convert.ToDouble(reader["total_unpaid_amount"]).ToString("F2");
                                var _resident = new ResidentAddress
                                {
                                    Address_ID = Convert.ToInt32(reader["addr_id"]),
                                    Resident_ID = Convert.ToInt32(reader["res_id"]),
                                    Name = reader["full_name"].ToString(),
                                    TotalAmount = Convert.ToDecimal(bills)
                                };
                                resident.Add(_resident);
                            }
                        }
                    }
                }
                return resident;
            }
            catch (Exception ex)
            {
                var err = ex.Message;
                throw;
            }
        }
    }
}
