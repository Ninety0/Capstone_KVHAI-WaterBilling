using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class AddressRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public AddressRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
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

        public async Task<List<ResidentAddress>> GetResidentAddressList()
        {
            var residentAddress = new List<ResidentAddress>();
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                    SELECT addr_id as ID, Name, block, lot, st_name as street
                    FROM (
                        SELECT 
		                    a.addr_id,
                            CONCAT(res.lname, ', ', res.fname, ', ', res.mname) AS Name, 
                            res.block, 
                            res.lot, 
                            st.st_name 
                        FROM address_tb a
                        JOIN street_tb st ON a.st_id = st.st_id
                        JOIN resident_tb res ON a.res_id = res.res_id
                    ) AS Combined", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var _residentAddress = new ResidentAddress
                            {
                                ID = Convert.ToInt32(reader["ID"].ToString()),
                                Name = reader["Name"].ToString() ?? string.Empty,
                                Block = reader["block"].ToString() ?? string.Empty,
                                Lot = reader["lot"].ToString() ?? string.Empty,
                                Street = reader["street"].ToString() ?? string.Empty,
                            };
                            residentAddress.Add(_residentAddress);
                        }
                    }
                }
            }
            return residentAddress;
        }

        public async Task<int> ValidateAddress(ResidentAddress residentAddress)
        {
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                    SELECT Name, block, lot, st_name AS street
                    FROM (
                    SELECT 
                        CONCAT(res.lname, ', ', res.fname, ', ', res.mname) AS Name, 
                        res.block, 
                        res.lot, 
                        st.st_name 
                    FROM address_tb a
                    JOIN street_tb st ON a.st_id = st.st_id
                    JOIN resident_tb res ON a.res_id = res.res_id
                    ) AS ResidentAddress
                    WHERE block= @block AND lot= @lot AND street = @st", connection))
                {
                    command.Parameters.AddWithValue("@block", residentAddress.Block);
                    command.Parameters.AddWithValue("@lot", residentAddress.Lot);
                    command.Parameters.AddWithValue("@st", "%" + residentAddress.Street + "%");

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0 ? 1 : 0;
                }
            }
        }

        public async Task<List<ResidentAddress>> GetName(ResidentAddress residentAddress)
        {
            try
            {
                var resident = new List<ResidentAddress>();
                string street = string.IsNullOrEmpty(residentAddress.Street) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Street);

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    SELECT addr_id as ID, Name, block, lot, st_name as street
                    FROM (
                        SELECT 
		                    a.addr_id,
                            CONCAT(res.lname, ', ', res.fname, ', ', res.mname) AS Name, 
                            res.block, 
                            res.lot, 
                            st.st_name 
                        FROM address_tb a
                        JOIN street_tb st ON a.st_id = st.st_id
                        JOIN resident_tb res ON a.res_id = res.res_id
                    ) AS ResidentAddress
                    WHERE block= @block AND lot= @lot AND st_name LIKE @st", connection))
                    {
                        command.Parameters.AddWithValue("@block", string.IsNullOrEmpty(residentAddress.Block) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Block));

                        command.Parameters.AddWithValue("@lot", string.IsNullOrEmpty(residentAddress.Lot) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Lot));

                        command.Parameters.AddWithValue("@st", "%" + street + "%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var _resident = new ResidentAddress();
                                _resident.ID = Convert.ToInt32(reader["ID"].ToString());
                                _resident.Name = reader["Name"].ToString() ?? string.Empty;

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

        public async Task<int> GetCountByLocation(string location = "")
        {
            int count = 0;
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                    SELECT Count(*) FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id
                    WHERE location LIKE @location;", connection))
                {
                    command.Parameters.AddWithValue("@location", "%" + location + "%");

                    count = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
            return count;
        }
    }
}
