using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace KVHAI.Repository
{
    public class ResidentRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly Hashing _hash;
        private readonly InputSanitize _sanitize;
        private readonly ImageUploadRepository _uploadRepository;
        private readonly StreetRepository _streetRepository;
        private readonly AddressRepository _addressRepository;

        public ResidentRepository(DBConnect dbconn, Hashing hash, InputSanitize sanitize, ImageUploadRepository uploadRepository, StreetRepository streetRepository, AddressRepository addressRepository)
        {
            _dbConnect = dbconn;
            _hash = hash;
            _sanitize = sanitize;
            _uploadRepository = uploadRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
        }

        public async Task<List<Resident>> GetAllResidentAsync()
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
                            _resident.Username = reader[6]?.ToString() ?? string.Empty;
                            _resident.Password = reader[7]?.ToString() ?? string.Empty;
                            _resident.Date_Residency = reader[8]?.ToString() ?? string.Empty;
                            _resident.Occupancy = reader[9]?.ToString() ?? string.Empty;
                            _resident.Activated = (reader[11].ToString() == "false") ? "pending" : "activated";
                            resident.Add(_resident);

                        }
                    }
                }
            }

            return resident;
        }

        public async Task<string> GetImagePathAsync(string resId)
        {
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT path_file FROM proof_img_tb WHERE res_id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", resId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? string.Empty;
                }
            }
        }

        //CREATE
        public async Task<string> CreateResident(Resident resident)
        {
            SanitizeFormData(resident);
            int resID = 0;
            string verificationToken = "";
            var pass = _hash.HashPassword(resident.Password);
            var dt = GetTimeDate();
            var phone = "63" + resident.Phone;
            var token = await CreateRandomToken();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    INSERT INTO resident_tb (lname, fname, mname, phone, email, username, password, occupancy, verification_token) OUTPUT INSERTED.verification_token
                    VALUES(@lname, @fname, @mname, @phone, @email, @user, @pass, @occupy,@vtoken)", connection))
                    {
                        //command.Parameters.AddWithValue("@id", res_id);
                        command.Parameters.AddWithValue("@lname", resident.Lname);
                        command.Parameters.AddWithValue("@fname", resident.Fname);
                        command.Parameters.AddWithValue("@mname", resident.Mname);
                        command.Parameters.AddWithValue("@phone", phone);
                        command.Parameters.AddWithValue("@email", resident.Email);
                        command.Parameters.AddWithValue("@user", resident.Username);
                        command.Parameters.AddWithValue("@pass", pass);
                        command.Parameters.AddWithValue("@occupy", resident.Occupancy);
                        command.Parameters.AddWithValue("@vtoken", token);

                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        //CHECK EXIST
        public async Task<bool> UserExists(Resident resident)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM resident_tb WHERE email = @email OR username= @user", connection))
                    {
                        command.Parameters.AddWithValue("@email", resident.Email);
                        command.Parameters.AddWithValue("@user", resident.Username);
                        int count = (int)command.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
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

        public async Task<int> UpdateStatus(int res_id, string status)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("UPDATE resident_tb set activated =@status WHERE res_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", res_id);
                        command.Parameters.AddWithValue("@status", status);

                        return await command.ExecuteNonQueryAsync();
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

        public async Task<int> GetResidentId()
        {
            int id = 1;

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("select res_id from resident_tb ORDER BY res_id DESC", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            id = Convert.ToInt32(reader[0].ToString());
                        }
                    }
                }
            }
            return id;
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
            formData.Username = _sanitize.HTMLSanitizer(formData.Username);
            formData.Password = _sanitize.HTMLSanitizer(formData.Password);
            formData.Occupancy = _sanitize.HTMLSanitizer(formData.Occupancy);
        }

        //WITHOUT SEARCH
        public async Task<List<Resident>> GetAllResidentAsync(int offset, int limit, string active)
        {
            var residents = new List<Resident>();
            var resID = "";

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($@"
                    SELECT * FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id  
                    WHERE activated = @active ORDER BY r.res_id OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY", connection))
                {
                    command.Parameters.AddWithValue("@active", active);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            resID = reader["res_id"]?.ToString() ?? string.Empty;

                            var _resident = new Resident();
                            _resident.Res_ID = reader["res_id"]?.ToString() ?? string.Empty;
                            _resident.Lname = reader["lname"]?.ToString() ?? string.Empty;
                            _resident.Fname = reader["fname"]?.ToString() ?? string.Empty;
                            _resident.Mname = reader["mname"]?.ToString() ?? string.Empty;
                            _resident.Phone = reader["phone"]?.ToString() ?? string.Empty;
                            _resident.Email = reader["email"]?.ToString() ?? string.Empty;
                            _resident.Username = reader["username"]?.ToString() ?? string.Empty;
                            _resident.Password = reader["password"]?.ToString() ?? string.Empty;
                            _resident.Date_Residency = reader["date_residency"]?.ToString() ?? string.Empty;
                            _resident.Occupancy = reader["occupancy"]?.ToString() ?? string.Empty;
                            _resident.Activated = (reader["activated"].ToString() == "false") ? "pending" : "activated";

                            _resident.Block = reader["block"]?.ToString() ?? string.Empty;
                            _resident.Lot = reader["lot"]?.ToString() ?? string.Empty;


                            residents.Add(_resident);

                        }
                    }
                }
            }

            return residents;
        }

        //WITH SEARCH
        public async Task<List<Resident>> GetAllResidentAsync(int offset, int limit, string active, string category, string? search)
        {
            var residents = new List<Resident>();
            string query = "";

            if (category == "name")
            {
                query = $@"
                    SELECT * FROM resident_tb 
                    WHERE(lname like @search OR fname like @search OR mname like @search) AND activated = @active
                    ORDER BY res_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            }
            else
            {
                query = $@"
                    SELECT * FROM resident_tb 
                    WHERE concat(block,' ',lot) like @search AND activated = @active
                    ORDER BY res_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            }


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@active", active);
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);
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
                            _resident.Username = reader[6]?.ToString() ?? string.Empty;
                            _resident.Password = reader[7]?.ToString() ?? string.Empty;
                            _resident.Date_Residency = reader[8]?.ToString() ?? string.Empty;
                            _resident.Occupancy = reader[9]?.ToString() ?? string.Empty;
                            _resident.Activated = (reader[11].ToString() == "false") ? "pending" : "activated";
                            residents.Add(_resident);

                        }
                    }
                }
            }

            return residents;
        }


        public async Task<int> CountResidentData(string active)
        {
            int result = 0;
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($"SELECT COUNT(*) FROM resident_tb WHERE activated = @active", connection))
                {
                    command.Parameters.AddWithValue("@active", active);
                    result = (int)command.ExecuteScalar();

                    return result;
                }
            }
        }

        public async Task<int> CountResidentData(string active, string category, string search = "")
        {
            int result = 0;

            string query = "";
            if (category == "name")
            {
                query = $@"
                    SELECT COUNT(*) FROM resident_tb 
                    WHERE(lname like @search OR fname like @search OR mname like @search) AND activated = @active";
            }
            else
            {
                query = $@"
                    SELECT COUNT(*) FROM resident_tb 
                    WHERE concat(block,' ',lot) like @search AND activated = @active";
            }

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@active", active);
                    result = (int)command.ExecuteScalar();

                    return result;
                }
            }
        }

        public async Task VerifyToken()
        {
            //using (var connection = await _dbConnect.GetOpenConnectionAsync())
            //{
            //    while (true)
            //    {
            //        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

            //        using (var command = new SqlCommand($"SELECT COUNT(*) FROM resident_tb WHERE {columnName} = @token", connection))
            //        {
            //            command.Parameters.AddWithValue("@token", token);
            //            int count = (int)await command.ExecuteScalarAsync();

            //            if (count == 0)
            //            {
            //                return token; // Token is unique, return it
            //            }
            //            // If token exists, loop will continue and generate a new one
            //        }
            //    }
            //}
        }

        private async Task<string> CreateRandomToken(string columnName = "verification_token")
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    while (true)
                    {
                        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

                        using (var command = new SqlCommand($"SELECT COUNT(*) FROM resident_tb WHERE {columnName} = @token", connection))
                        {
                            command.Parameters.AddWithValue("@token", token);
                            int count = (int)await command.ExecuteScalarAsync();

                            if (count == 0)
                            {
                                return token; // Token is unique, return it
                            }
                            // If token exists, loop will continue and generate a new one
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // It's generally not a good practice to return exception messages as tokens
                // Instead, log the error and throw an exception or return a failure indicator
                //_logger.LogError(ex, "Error generating unique token");
                //throw new Exception("Failed to generate a unique token", ex);
                return null;
            }
        }
    }
}

/*
 

        public async Task<List<Resident>> GetBlockAndLot(SqlConnection connection, string id)
        {
            var blockLot = new List<Resident>();
            using (var command = new SqlCommand("SELECT * FROM address_tb where res_id =@id ", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var resident = new Resident
                        {
                            Block = reader["block"].ToString() ?? string.Empty,
                            Lot = reader["lot"].ToString() ?? string.Empty,
                        };
                        blockLot.Add(resident);
                    }
                }
            }
            return blockLot;
        }
 */
