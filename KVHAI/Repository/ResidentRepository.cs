using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;
using System.Text;

namespace KVHAI.Repository
{
    public class ResidentRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly Hashing _hash;
        private readonly InputSanitize _sanitize;

        public ResidentRepository(DBConnect dbconn, Hashing hash, InputSanitize sanitize)
        {
            _dbConnect = dbconn;
            _hash = hash;
            _sanitize = sanitize;
        }

        public async Task<List<Resident>> GetAllEmployeesAsync()
        {
            var resident = new List<Resident>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT * FROM resident_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var _resident = new Resident();
                            _resident.Res_ID = reader[0]?.ToString() ?? string.Empty;
                            _resident.Lname = reader[1]?.ToString() ?? string.Empty;
                            _resident.Fname = reader[2]?.ToString() ?? string.Empty;
                            _resident.Mname = reader[3]?.ToString() ?? string.Empty;
                            _resident.Phone = reader[4]?.ToString() ?? string.Empty;
                            _resident.Email = reader[5]?.ToString() ?? string.Empty;
                            _resident.Block = reader[6]?.ToString() ?? string.Empty;
                            _resident.Lot = reader[7]?.ToString() ?? string.Empty;
                            _resident.Username = reader[8]?.ToString() ?? string.Empty;
                            _resident.Password = reader[9]?.ToString() ?? string.Empty;
                            _resident.Date_Residency = reader[10]?.ToString() ?? string.Empty;
                            _resident.Occupancy = reader[11]?.ToString() ?? string.Empty;
                            _resident.Created_At = reader[12]?.ToString() ?? string.Empty;
                            resident.Add(_resident);

                        }
                    }
                }
            }

            return resident;
        }

        //address id need to update the code
        public async Task<int> CreateEmployee(Resident resident)
        {
            SanitizeFormData(resident);
            var res_id = await GetEmployeeId();
            var pass = _hash.HashPassword(resident.Password);
            var dt = GetTimeDate();
            var phone = "63" + resident.Phone;

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO resident_tb (lname, fname, mname, phone, email, block, lot, username, password, date_residency, occupancy, created_at) VALUES(@lname, @fname, @mname, @phone, @email, @blk, @lot, @user, @pass, @residency, @occupy, @create)", connection))
                    {
                        //command.Parameters.AddWithValue("@id", res_id);
                        command.Parameters.AddWithValue("@lname", resident.Lname);
                        command.Parameters.AddWithValue("@fname", resident.Fname);
                        command.Parameters.AddWithValue("@mname", resident.Mname);
                        command.Parameters.AddWithValue("@phone", phone);
                        command.Parameters.AddWithValue("@email", resident.Email);
                        command.Parameters.AddWithValue("@blk", resident.Block);
                        command.Parameters.AddWithValue("@lot", resident.Lot);
                        command.Parameters.AddWithValue("@user", resident.Username);
                        command.Parameters.AddWithValue("@pass", pass);
                        command.Parameters.AddWithValue("@residency", resident.Date_Residency);
                        command.Parameters.AddWithValue("@occupy", resident.Occupancy);
                        command.Parameters.AddWithValue("@create", dt);

                        await command.ExecuteNonQueryAsync();

                        return 1;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task UpdateCategory(Resident resident)
        {
            var hasValue = !string.IsNullOrEmpty(resident.Password);

            var query = new StringBuilder("UPDATE resident_tb SET ");
            query.Append("lname = @lname, ");
            query.Append("fname = @fname, ");
            query.Append("mname = @mname, ");
            query.Append("phone = @phone, ");
            query.Append("email = @email, ");
            query.Append("username = @user, ");
            if (hasValue)
            {
                query.Append("password = @pass, ");
            }
            query.Append("occupancy = @occupy ");
            query.Append("WHERE res_id = @id");


            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        command.Parameters.AddWithValue("@id", resident.Res_ID);
                        command.Parameters.AddWithValue("@lname", resident.Lname);
                        command.Parameters.AddWithValue("@fname", resident.Fname);
                        command.Parameters.AddWithValue("@mname", resident.Mname);
                        command.Parameters.AddWithValue("@phone", resident.Phone);
                        command.Parameters.AddWithValue("@email", resident.Email);
                        command.Parameters.AddWithValue("@user", resident.Username);

                        if (hasValue)
                        {
                            var hashPass = _hash.HashPassword(resident.Password);
                            command.Parameters.AddWithValue("@pass", hashPass);
                        }

                        command.Parameters.AddWithValue("@occupy", resident.Occupancy);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteCategory(int id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("DELETE FROM Category WHERE res_id=@id", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> GetEmployeeId()
        {
            int new_id = 1;

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("select res_id from resident_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int id = Convert.ToInt32(reader[0].ToString());

                            new_id = id + 1;
                        }
                        else
                        {
                            new_id = 1;
                        }
                    }
                }
            }

            return new_id;
        }

        private string GetTimeDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void SanitizeFormData(Resident formData)
        {
            formData.Lname = _sanitize.HTMLSanitizer(formData.Lname);
            formData.Fname = _sanitize.HTMLSanitizer(formData.Fname);
            formData.Mname = _sanitize.HTMLSanitizer(formData.Mname);
            formData.Phone = _sanitize.HTMLSanitizer(formData.Phone);
            formData.Email = _sanitize.HTMLSanitizer(formData.Email);
            formData.Block = _sanitize.HTMLSanitizer(formData.Block);
            formData.Lot = _sanitize.HTMLSanitizer(formData.Lot);
            formData.Username = _sanitize.HTMLSanitizer(formData.Username);
            formData.Password = _sanitize.HTMLSanitizer(formData.Password);
            formData.Date_Residency = _sanitize.HTMLSanitizer(formData.Date_Residency);
            formData.Occupancy = _sanitize.HTMLSanitizer(formData.Occupancy);
        }
    }
}
