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
        private readonly LoginRepository _loginRepository;

        public ResidentRepository(DBConnect dbconn, Hashing hash, InputSanitize sanitize, ImageUploadRepository uploadRepository, StreetRepository streetRepository, AddressRepository addressRepository, LoginRepository loginRepository)
        {
            _dbConnect = dbconn;
            _hash = hash;
            _sanitize = sanitize;
            _uploadRepository = uploadRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _loginRepository = loginRepository;
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
                            resident.Add(_resident);

                        }
                    }
                }
            }

            return resident;
        }

        public async Task<string> GetImagePathAsync(string addressID)
        {
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT path_file FROM proof_img_tb WHERE addr_id = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", addressID);
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

        //READ
        public async Task<int> GetResidentID(Resident resident)
        {
            try
            {
                string passFromDB = string.Empty;
                string resIDFromDB = string.Empty;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password
                    using (var command = new SqlCommand("SELECT * FROM resident_tb WHERE username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@user", resident.Username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Retrieve the hashed password from the database
                                passFromDB = reader["password"].ToString() ?? string.Empty;
                                resIDFromDB = reader["res_id"].ToString() ?? string.Empty;
                            }
                        }

                        // If no password was found, return false
                        if (string.IsNullOrEmpty(passFromDB))
                        {
                            return 0; // Username not found or password missing
                        }

                        // Verify the input password with the hashed password from the database
                        if (_hash.VerifyPassword(passFromDB, resident.Password))
                        {
                            if (!string.IsNullOrEmpty(resIDFromDB))
                            {
                                return Convert.ToInt32(resIDFromDB);
                            } // Password is correct
                        }

                        return 0;
                        // Password is incorrect
                    }
                }
            }
            catch (Exception)
            {
                // Handle exception (logging, etc.)
                return 0;
            }
        }


        //REGION START
        #region CODE NEED TO BE MIGRATE
        //VALIDATE ACCOUNT
        public async Task<bool> ValidateAccount(Resident resident)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query to get the hashed password based on the username
                    using (var command = new SqlCommand("SELECT COUNT(username) FROM resident_tb WHERE username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@user", resident.Username);

                        // Execute the query and get the hashed password
                        var count = await command.ExecuteScalarAsync();

                        return Convert.ToInt32(count) > 0;
                    }
                }
            }
            catch (Exception)
            {
                // Handle any exception as needed (logging, etc.)
                return false;
            }
        }

        //VERIFY PASSWORD
        public async Task<bool> ValidatePassword(Resident resident)
        {
            try
            {
                string passFromDB = string.Empty;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password
                    using (var command = new SqlCommand("SELECT password FROM resident_tb WHERE username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@user", resident.Username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Retrieve the hashed password from the database
                                passFromDB = reader["password"].ToString() ?? string.Empty;
                            }
                        }

                        // If no password was found, return false
                        if (string.IsNullOrEmpty(passFromDB))
                        {
                            return false; // Username not found or password missing
                        }

                        // Verify the input password with the hashed password from the database
                        if (_hash.VerifyPassword(passFromDB, resident.Password))
                            return true; // Password is correct
                        else
                            return false; // Password is incorrect
                    }
                }
            }
            catch (Exception)
            {
                // Handle exception (logging, etc.)
                return false;
            }
        }

        //VERIFY PASSWORD
        public async Task<List<Resident>> VerifiedAt(Resident resident)
        {
            try
            {
                var resList = new List<Resident>();
                string passFromDB = string.Empty;
                string verified_At = string.Empty;
                string token = string.Empty;
                string email = string.Empty;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password and verification status
                    using (var command = new SqlCommand("SELECT * FROM resident_tb WHERE username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@user", resident.Username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                passFromDB = reader["password"].ToString() ?? string.Empty;
                                verified_At = reader["verified_at"].ToString() ?? string.Empty;
                                token = reader["verification_token"].ToString() ?? string.Empty;
                                email = reader["email"].ToString() ?? string.Empty;
                            }
                        }

                        // If no password was found, return null
                        if (string.IsNullOrEmpty(passFromDB))
                        {
                            return null; // User not found
                        }

                        // Verify the input password with the hashed password from the database
                        if (_hash.VerifyPassword(passFromDB, resident.Password))
                        {
                            var credentials = new Resident
                            {
                                Verified_At = verified_At,
                                Verification_Token = token,
                                Email = email
                            };

                            resList.Add(credentials);
                            return resList;
                        }
                        else
                        {
                            return null; // Password incorrect
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

        public async Task<string> GetEmailByToken(string token)
        {
            string email = string.Empty;
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT email FROM resident_tb WHERE verification_token = @token", connection))
                    {
                        command.Parameters.AddWithValue("@token", token);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                email = reader["email"].ToString() ?? string.Empty;
                            }
                        }

                        return email;
                    }
                }
            }
            catch (Exception)
            {
                return email;
            }
        }

        //VALIDATE TOKEN EXISTENCE
        public async Task<bool> IsTokenExist(string token)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM resident_tb WHERE verification_token = @token", connection))
                    {
                        command.Parameters.AddWithValue("@token", token);
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

        //EMAIL TOKEN
        public async Task<int> AddVerification(string email)
        {
            /*
             return type int
                0 fail
                1 success
                2 code already sent and still valid
             */
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Check if a valid token already exists
                    using (var command = new SqlCommand("SELECT reset_token_expire FROM resident_tb WHERE email = @email", connection))
                    {
                        command.Parameters.AddWithValue("@email", email);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            var expiryDate = (DateTime)result;
                            if (expiryDate > DateTime.Now)
                            {
                                return 2; // Code already sent and still valid
                            }
                        }
                    }

                    // Generate new verification code
                    var codeGenerate = await _loginRepository.GenerateVerificationCode(4);
                    if (string.IsNullOrEmpty(codeGenerate[0].Code))
                    {
                        return 0; // Failed to generate code
                    }

                    // Update resident_tb with new token and expiration
                    using (var command = new SqlCommand("UPDATE resident_tb SET password_reset_token = @token, reset_token_expire = @time WHERE email = @email", connection))
                    {
                        command.Parameters.AddWithValue("@token", codeGenerate[0].Code);
                        command.Parameters.AddWithValue("@time", codeGenerate[0].Expiration);
                        command.Parameters.AddWithValue("@email", email);
                        var updateResult = await command.ExecuteNonQueryAsync();

                        if (updateResult > 0)
                        {
                            var sendEmail = new EmailDto
                            {
                                To = email
                            };
                            await _loginRepository.SendEmail(sendEmail, codeGenerate[0].Code);
                            return 1; // Success
                        }
                    }
                }

                return 0; // Fail (no rows updated)
            }
            catch (Exception)
            {
                // Consider logging the exception here
                return 0; // Fail
            }
        }

        public async Task<int> IsCodeTheSame(string userProvidedCode, string email)
        {
            /*
             return type int
                0 date expires
                1 success
                2 code not match
             */
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Retrieve the stored code and expiration date from the database
                    using (var command = new SqlCommand("SELECT password_reset_token, reset_token_expire FROM resident_tb WHERE email = @email", connection))
                    {
                        command.Parameters.AddWithValue("@email", email);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var storedCode = reader["password_reset_token"].ToString();
                                var expiryDate = (DateTime)reader["reset_token_expire"];

                                // Check if the provided code matches the stored code and if it's still valid
                                if (!string.IsNullOrEmpty(storedCode) && expiryDate > DateTime.Now && storedCode == userProvidedCode)
                                {
                                    // Close the reader before executing the update
                                    reader.Close();

                                    // Update the verified_at column
                                    using (var updateCommand = new SqlCommand("UPDATE resident_tb SET verified_at = @verifiedAt WHERE email = @email", connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@verifiedAt", DateTime.Now);
                                        updateCommand.Parameters.AddWithValue("@email", email);
                                        var updateResult = await updateCommand.ExecuteNonQueryAsync();
                                        if (updateResult > 0)
                                        {
                                            // Clear the password_reset_token and reset_token_expire columns
                                            using (var clearCommand = new SqlCommand("UPDATE resident_tb SET password_reset_token = @token, reset_token_expire = @expire WHERE email = @email", connection))
                                            {
                                                clearCommand.Parameters.AddWithValue("@token", DBNull.Value);
                                                clearCommand.Parameters.AddWithValue("@expire", DBNull.Value);
                                                clearCommand.Parameters.AddWithValue("@email", email);
                                                await clearCommand.ExecuteNonQueryAsync();
                                                return 1; // Success
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    return 0; // Code is not valid
                                }
                            }
                        }
                    }
                }

                return 0; // Fail (no matching code found)
            }
            catch (Exception)
            {
                // Consider logging the exception here
                return 0; // Fail
            }
        }

        #endregion
        //END REGION

        public async Task<int> UpdatePassword(string password, string email)
        {
            var _password = await _sanitize.HTMLSanitizerAsync(password);
            var _email = await _sanitize.HTMLSanitizerAsync(email);
            var hashPassword = _hash.HashPassword(_password);

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("UPDATE resident_tb set password = @pass where email =@email", connection))
                    {
                        command.Parameters.AddWithValue("@pass", hashPassword);
                        command.Parameters.AddWithValue("@email", _email);

                        return await command.ExecuteNonQueryAsync();
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
        public async Task<List<AddressWithResident>> GetAllResidentAsync(int offset, int limit, string verified = "true")
        {
            var residents = new List<AddressWithResident>();
            var resID = "";
            var streetID = "";

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($@"
            SELECT r.res_id, r.lname, r.fname, r.mname, r.phone, r.email, r.username, r.password, r.occupancy, r.verified_at,
                   a.addr_id, a.block, a.lot, a.st_id, a.is_verified
            FROM resident_tb r
            JOIN address_tb a ON r.res_id = a.res_id  
            WHERE r.verified_at IS NOT NULL AND a.is_verified = @verify
            ORDER BY r.res_id 
            OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY", connection))
                {
                    command.Parameters.AddWithValue("@verify", verified);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resID = reader["res_id"]?.ToString() ?? string.Empty;
                            streetID = reader["st_id"]?.ToString() ?? string.Empty;
                            var st_name = await _streetRepository.GetStreetName(streetID);

                            // Check if the resident is already in the list
                            var existingResident = residents.FirstOrDefault(r => r.Res_ID == resID);

                            if (existingResident != null)
                            {
                                // Add the address-related details to the existing resident
                                existingResident.AddressID?.Add(reader["addr_id"]?.ToString() ?? string.Empty);
                                existingResident.Block?.Add(reader["block"]?.ToString() ?? string.Empty);
                                existingResident.Lot?.Add(reader["lot"]?.ToString() ?? string.Empty);
                                existingResident.Street_Name?.Add(st_name ?? string.Empty);
                                existingResident.Is_Verified?.Add(reader["is_verified"]?.ToString() ?? string.Empty);
                            }
                            else
                            {
                                // Create a new resident object and add the first address details
                                var _resident = new AddressWithResident
                                {
                                    Res_ID = reader["res_id"]?.ToString() ?? string.Empty,
                                    Lname = reader["lname"]?.ToString() ?? string.Empty,
                                    Fname = reader["fname"]?.ToString() ?? string.Empty,
                                    Mname = reader["mname"]?.ToString() ?? string.Empty,
                                    Phone = reader["phone"]?.ToString() ?? string.Empty,
                                    Email = reader["email"]?.ToString() ?? string.Empty,
                                    Username = reader["username"]?.ToString() ?? string.Empty,
                                    Password = reader["password"]?.ToString() ?? string.Empty,
                                    Occupancy = Convert.ToInt32(reader["occupancy"]) == 1 ? "Owner" : "Renter",
                                    AddressID = new List<string> { reader["addr_id"]?.ToString() ?? string.Empty },
                                    Block = new List<string> { reader["block"]?.ToString() ?? string.Empty },
                                    Lot = new List<string> { reader["lot"]?.ToString() ?? string.Empty },
                                    Street_Name = new List<string> { st_name ?? string.Empty },
                                    Is_Verified = new List<string> { reader["is_verified"]?.ToString() ?? string.Empty }
                                };

                                residents.Add(_resident);
                            }
                        }
                    }
                }
            }

            return residents;
        }


        //WITH SEARCH
        public async Task<List<AddressWithResident>> GetAllResidentAsync(int offset, int limit, string is_verified, string category, string? search = "")
        {
            //var residents = new List<Resident>();
            string query = "";
            var residents = new List<AddressWithResident>();
            var resID = "";
            var streetID = "";

            if (category == "name")
            {
                /*
                 * SELECT * FROM resident_tb 
                    WHERE(lname like @search OR fname like @search OR mname like @search) AND activated = @active
                    ORDER BY res_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY

                SELECT * FROM resident_tb 
                    WHERE concat(block,' ',lot) like @search AND activated = @active
                    ORDER BY res_id OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
                 */
                query = $@"
                    SELECT r.res_id, r.lname, r.fname, r.mname, r.phone, r.email, r.username, r.password, r.occupancy, r.verified_at,
                           a.addr_id, a.block, a.lot, a.st_id, a.is_verified
                    FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id  
                    WHERE r.verified_at IS NOT NULL AND a.is_verified = @verify AND concat(r.lname,' ',fname,' ',mname) like @search
                    ORDER BY r.res_id 
                    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            }
            else
            {
                query = $@"
                    SELECT r.res_id, r.lname, r.fname, r.mname, r.phone, r.email, r.username, r.password, r.occupancy, r.verified_at,
                           a.addr_id, a.block, a.lot, a.st_id, a.is_verified
                    FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id  
                    WHERE r.verified_at IS NOT NULL AND a.is_verified = @verify
                    ORDER BY r.res_id 
                    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            }


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@verify", is_verified);
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resID = reader["res_id"]?.ToString() ?? string.Empty;
                            streetID = reader["st_id"]?.ToString() ?? string.Empty;
                            var st_name = await _streetRepository.GetStreetName(streetID);

                            // Check if the resident is already in the list
                            var existingResident = residents.FirstOrDefault(r => r.Res_ID == resID);

                            if (existingResident != null)
                            {
                                // Add the address-related details to the existing resident
                                existingResident.AddressID?.Add(reader["addr_id"]?.ToString() ?? string.Empty);
                                existingResident.Block?.Add(reader["block"]?.ToString() ?? string.Empty);
                                existingResident.Lot?.Add(reader["lot"]?.ToString() ?? string.Empty);
                                existingResident.Street_Name?.Add(st_name ?? string.Empty);
                                existingResident.Is_Verified?.Add(reader["is_verified"]?.ToString() ?? string.Empty);
                            }
                            else
                            {
                                // Create a new resident object and add the first address details
                                var _resident = new AddressWithResident
                                {
                                    Res_ID = reader["res_id"]?.ToString() ?? string.Empty,
                                    Lname = reader["lname"]?.ToString() ?? string.Empty,
                                    Fname = reader["fname"]?.ToString() ?? string.Empty,
                                    Mname = reader["mname"]?.ToString() ?? string.Empty,
                                    Phone = reader["phone"]?.ToString() ?? string.Empty,
                                    Email = reader["email"]?.ToString() ?? string.Empty,
                                    Username = reader["username"]?.ToString() ?? string.Empty,
                                    Password = reader["password"]?.ToString() ?? string.Empty,
                                    Occupancy = Convert.ToInt32(reader["occupancy"]) == 1 ? "Owner" : "Renter",
                                    AddressID = new List<string> { reader["addr_id"]?.ToString() ?? string.Empty },
                                    Block = new List<string> { reader["block"]?.ToString() ?? string.Empty },
                                    Lot = new List<string> { reader["lot"]?.ToString() ?? string.Empty },
                                    Street_Name = new List<string> { st_name ?? string.Empty },
                                    Is_Verified = new List<string> { reader["is_verified"]?.ToString() ?? string.Empty }
                                };

                                residents.Add(_resident);
                            }
                        }
                    }
                }
            }

            return residents;
        }

        //COUNT RESIDENT W/O Search
        public async Task<int> CountResidentData(string active)
        {
            int result = 0;
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($"SELECT COUNT(*) FROM resident_tb WHERE verified_at IS NOT NULL", connection))
                {
                    result = (int)command.ExecuteScalar();

                    return result;
                }
            }
        }

        //COUNT DATA W/ SEARCH
        public async Task<int> CountResidentData(string active, string category, string search = "")
        {
            int result = 0;

            string query = "";
            if (category == "name")
            {
                /*
                 * SELECT COUNT(*) FROM resident_tb 
                    WHERE(lname like @search OR fname like @search OR mname like @search) AND activated = @active"

                SELECT COUNT(*) FROM resident_tb 
                    WHERE concat(block,' ',lot) like @search AND activated = @active

                 */
                query = $@"
                     SELECT Count(*)
                    FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id  
                    WHERE r.verified_at IS NOT NULL AND a.is_verified = @verify AND concat(r.lname,' ',fname,' ',mname) like @search";
            }
            else
            {
                query = $@"
                    SELECT count(*)
                    FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id  
                    WHERE r.verified_at IS NOT NULL AND a.is_verified = @verify AND concat(block,' ',lot) like @search
                    ";
            }

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@verify", active);
                    var count = await command.ExecuteScalarAsync();

                    return Convert.ToInt32(count);
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
