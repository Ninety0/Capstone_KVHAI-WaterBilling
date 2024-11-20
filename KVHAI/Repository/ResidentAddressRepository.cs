using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class ResidentAddressRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;
        private readonly ListRepository _listRepository;
        private readonly StreetRepository _streetRepository;


        public ResidentAddressRepository(DBConnect dBConnect, InputSanitize inputSanitize, ListRepository listRepository, StreetRepository streetRepository)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _listRepository = listRepository;
            _streetRepository = streetRepository;

        }

        public async Task<List<int>> GetResidentID(string address_id)
        {
            var resAddress = await _listRepository.ResidentAddressList();
            var addridList = resAddress.Where(a => a.Address_ID.ToString() == address_id).Select(r => r.Resident_ID).ToList();

            return addridList;
        }

        public async Task<List<int>> GetResidentIDByAddressList(string address_id)
        {
            var resAddress = await _listRepository.AddressList();
            var addridList = resAddress.Where(a => a.Address_ID.ToString() == address_id).Select(r => r.Resident_ID).ToList();

            return addridList;
        }

        public async Task<List<ResidentAddress>> GetRentalApplication(string address_id = "")
        {
            try
            {
                var residentAddress = new List<ResidentAddress>();
                /// STATUS -  0 (Pending), 1 (Accepted), 2 (Rejected)
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        Select ra.*,lname,fname,mname,phone,email, block,lot,st_name From resident_address_tb ra
                        JOIN resident_tb r ON ra.res_id = r.res_id
                        JOIN address_tb a ON ra.addr_id = a.addr_id
                        JOIN street_tb s ON s.st_id = a.st_id
                        WHERE a.addr_id = @id AND is_owner = 0 AND status = 0", connection))
                    {
                        command.Parameters.AddWithValue("@id", address_id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var ra = new ResidentAddress
                                {
                                    Resident_Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Address_ID = reader.GetInt32(2),
                                    Is_Owner = reader.GetBoolean(3) ? 1 : 0,
                                    Status = reader.IsDBNull(4) ? string.Empty : reader.GetInt32(4).ToString(),
                                    Request_Date = reader.IsDBNull(5) ? string.Empty : reader.GetDateTime(5).ToString("yyyy-MM-dd HH:mm:ss"),
                                    Decision_Date = reader.IsDBNull(6) ? string.Empty : reader.GetDateTime(6).ToString("yyyy-MM-dd HH:mm:ss"),
                                    Name = string.Join(", ", reader.GetString(7), reader.GetString(8), reader.GetString(9)),
                                    Phone = reader.GetString(10),
                                    Email = reader.GetString(11),
                                    Block = reader.GetString(12),
                                    Lot = reader.GetString(13),
                                    Street = reader.GetString(14)
                                };

                                residentAddress.Add(ra);
                            }
                        }
                    }
                }
                return residentAddress;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public async Task<List<ResidentAddress>> GetRenter(string address_id = "")
        {
            try
            {
                var residentAddress = new List<ResidentAddress>();
                /// STATUS -  0 (Pending), 1 (Accepted), 2 (Rejected)
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        select resident_addr_id, r.res_id, lname,fname,mname,phone,email,decision_date  from resident_address_tb ra 
                        JOIN resident_tb r ON ra.res_id = r.res_id
                        WHERE addr_id = @id AND is_owner = 0  AND ra.status = 1", connection))
                    {
                        command.Parameters.AddWithValue("@id", address_id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var ra = new ResidentAddress
                                {
                                    Resident_Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Name = string.Join(", ", reader.GetString(2), reader.GetString(3), reader.GetString(4)),
                                    Phone = reader.GetString(5),
                                    Email = reader.GetString(6),
                                    Decision_Date = reader.IsDBNull(7) ? string.Empty : reader.GetDateTime(7).ToString("yyyy-MM-dd HH:mm:ss")
                                };

                                residentAddress.Add(ra);
                            }
                        }
                    }
                }
                return residentAddress;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public async Task<int> UpdateStatus(string residentAddress_id, string address_id, string resident_id, string status)
        {
            try
            {
                /// STATUS -  0 (Pending), 1 (Accepted), 2 (Rejected)

                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        UPDATE resident_address_tb set status = @status, decision_date = @date
                        WHERE resident_addr_id = @raid AND addr_id = @aid AND res_id = @rid", connection))
                    {
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@raid", residentAddress_id);
                        command.Parameters.AddWithValue("@aid", address_id);
                        command.Parameters.AddWithValue("@rid", resident_id);
                        command.Parameters.AddWithValue("@date", date);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> CancelRequest(string residentAddress_id)
        {
            try
            {
                /// STATUS -  0 (Pending), 1 (Accepted), 2 (Rejected)
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        DELETE FROM resident_address_tb
                        WHERE resident_addr_id = @raid ", connection))
                    {
                        command.Parameters.AddWithValue("@raid", residentAddress_id);
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<Address>> GetAddressessByResId(string resID)
        {
            try
            {
                var addressList = new List<Address>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        select a.* FROM resident_address_tb ra
                        JOIN address_tb a ON ra.addr_id = a.addr_id
                        WHERE is_verified = 'true'AND ra.res_id = @id AND ra.status = @status", connection))
                    {
                        command.Parameters.AddWithValue("@id", resID);
                        command.Parameters.AddWithValue("@status", "1");
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

        public async Task<List<WaterBillWithAddress>> UnpaidResidentWaterBilling(string residentID, string addressID)
        {
            try
            {
                var model = new ModelBinding();
                var wbList = new List<WaterBilling>();
                var addrList = new List<Address>();
                var wbaList = new List<WaterBillWithAddress>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    select * from water_billing_tb wb
                    JOIN address_tb a ON wb.addr_id = a.addr_id
                    JOIN resident_address_tb ra ON a.addr_id = ra.addr_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    JOIN street_tb s ON a.st_id = s.st_id
                    WHERE  wb.status = 'unpaid'  AND is_verified = 'true' 
                    AND ra.res_id  = @res_id AND ra.status = 1 AND a.addr_id = @addr_id", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", residentID);
                        command.Parameters.AddWithValue("@addr_id", addressID);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            double totalAmount = 0.00;
                            while (await reader.ReadAsync())
                            {
                                totalAmount += Convert.ToDouble(reader["amount"].ToString());
                                var wba = new WaterBillWithAddress
                                {
                                    WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                    Amount = reader["amount"].ToString() ?? string.Empty,
                                    Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                    Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                    Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                    Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                    Status = reader["status"].ToString() ?? string.Empty,
                                    WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Block = reader["block"].ToString() ?? string.Empty,
                                    Lot = reader["lot"].ToString() ?? string.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString() ?? string.Empty),
                                    Street_Name = reader["st_name"].ToString() ?? string.Empty,

                                    TotalAmount = totalAmount.ToString("F2")
                                };

                                wbaList.Add(wba);

                            }
                        }
                    }
                }

                return wbaList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<WaterBillWithAddress>> GetPaidResidentWaterBilling(string residentID, string addressID)
        {
            try
            {
                var model = new ModelBinding();
                var wbList = new List<WaterBilling>();
                var addrList = new List<Address>();
                var wbaList = new List<WaterBillWithAddress>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    SELECT DISTINCT wb.*, a.*,ra.*,r.*,s.*
                    FROM water_billing_tb wb
                    JOIN address_tb a ON wb.addr_id = a.addr_id
                    JOIN resident_address_tb ra ON a.addr_id = ra.addr_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    JOIN street_tb s ON a.st_id = s.st_id
                    WHERE wb.status = 'paid' AND is_verified = 'true'
                    AND ra.res_id  = @res_id AND ra.status = 1 AND a.addr_id = @addr_id", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", residentID);
                        command.Parameters.AddWithValue("@addr_id", addressID);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            double totalAmount = 0.00;
                            while (await reader.ReadAsync())
                            {
                                totalAmount += Convert.ToDouble(reader["amount"].ToString());
                                var wba = new WaterBillWithAddress
                                {
                                    WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                    Amount = reader["amount"].ToString() ?? string.Empty,
                                    Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                    Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                    Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                    Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                    Status = reader["status"].ToString() ?? string.Empty,
                                    WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Block = reader["block"].ToString() ?? string.Empty,
                                    Lot = reader["lot"].ToString() ?? string.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString() ?? string.Empty),
                                    Street_Name = reader["st_name"].ToString() ?? string.Empty,

                                    TotalAmount = totalAmount.ToString("F2")
                                };

                                wbaList.Add(wba);

                            }
                        }
                    }
                }

                return wbaList;
            }
            catch (Exception ex)
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
                                    Is_Owner = reader.GetBoolean(7) ? 1 : 0,
                                    Status = reader.GetInt32(8).ToString(),
                                    Request_Date = reader.GetDateTime(9).ToString("yyyy-MM-dd HH:mm:ss"),
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
