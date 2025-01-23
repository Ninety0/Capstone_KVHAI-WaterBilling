using KVHAI.CustomClass;
using KVHAI.Hubs;
using KVHAI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
        private readonly IHubContext<StaffNotificationHub> _staffhubContext;
        private readonly NotificationRepository _notificationRepository;
        private readonly ListRepository _listRepository;

        public ResidentRepository(DBConnect dbconn, Hashing hash, InputSanitize sanitize, ImageUploadRepository uploadRepository, StreetRepository streetRepository, AddressRepository addressRepository, LoginRepository loginRepository, NotificationRepository notificationRepository, IHubContext<StaffNotificationHub> hubContext, ListRepository listRepository)
        {
            _dbConnect = dbconn;
            _hash = hash;
            _sanitize = sanitize;
            _uploadRepository = uploadRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _loginRepository = loginRepository;
            _notificationRepository = notificationRepository;
            _staffhubContext = hubContext;
            _listRepository = listRepository;
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

        //CREATE RESIDENT
        public async Task<int> CreateResident(Resident resident, List<Address> address)
        {
            SanitizeFormData(resident);
            var dt = GetTimeDate();

            var phone = "63" + resident.Phone;
            var token = await CreateRandomToken();
            var accountNumber = await AccountNumberCreation(address);


            int insertResidentResult = 0;
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var insertCommand = new SqlCommand(@"
                        INSERT INTO resident_tb (lname, fname, mname) 
                        VALUES(@lname, @fname, @mname);
                        SELECT SCOPE_IDENTITY();", connection, transaction))
                            {
                                //command.Parameters.AddWithValue("@id", res_id);
                                insertCommand.Parameters.AddWithValue("@lname", resident.Lname);
                                insertCommand.Parameters.AddWithValue("@fname", resident.Fname);
                                insertCommand.Parameters.AddWithValue("@mname", resident.Mname);

                                insertResidentResult = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

                                //return insertResidentResult;
                            }

                            if (insertResidentResult < 1)
                            {
                                throw new Exception();
                            }

                            var createAddress = await CreateAddress(insertResidentResult.ToString(), transaction, connection, address, accountNumber);

                            if (createAddress < 1)
                            {
                                throw new Exception();
                            }

                            transaction.Commit();

                            return 1;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            // Log the exception here
                            return 0;
                        }
                    }

                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        //CREATE ADDRESS
        public async Task<int> CreateAddress(string res_id, SqlTransaction transaction, SqlConnection connection, List<Address> address, List<string> accountNumbers)
        {
            try
            {
                var addressIDList = new List<Address>();
                var addressID = 0;
                var residentAddressResult = 0;

                for (int i = 0; i < address.Count(); i++)
                {
                    using (var command = new SqlCommand(@"
                        INSERT INTO address_tb (res_id,block,lot,st_id,location, account_number, date_residency, register_at) 
                        VALUES(@res,@blk,@lot,@st,@location, @accnum, @residency, @date);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                    {
                        int _location = 0;
                        int _block;
                        // Safely try to parse the block value, defaulting to 0 if it fails
                        if (int.TryParse(address[i].Block, out _block))
                        {
                            if (_block >= 51 && _block <= 143)
                            {
                                _location = 1;
                            }
                            else if (_block >= 41 && _block <= 50)
                            {
                                _location = 2;
                            }
                            else if (_block >= 24 && _block <= 40)
                            {
                                _location = 3;
                            }
                            else if (_block >= 1 && _block <= 23)
                            {
                                _location = 4;
                            }
                        }
                        else
                        {
                            // Handle the case where the block value is not valid (e.g., log it, skip, etc.)
                            _block = 0; // or handle as needed
                        }

                        command.Parameters.AddWithValue("@res", res_id);
                        command.Parameters.AddWithValue("@blk", address[i].Block ?? "");
                        command.Parameters.AddWithValue("@lot", address[i].Lot ?? "");
                        command.Parameters.AddWithValue("@st", address[i].Street_ID);
                        command.Parameters.AddWithValue("@location", _location);
                        command.Parameters.AddWithValue("@accnum", accountNumbers[i]);
                        command.Parameters.AddWithValue("@residency", address[i].Date_Residency);
                        command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        addressID = (int?)(await command.ExecuteScalarAsync()) ?? 0;

                        if (addressID < 1)
                        {
                            return 0;
                        }
                    }

                    //OWNET DATA TYPE BIT
                    //1 - homeowner
                    //0 - renter
                    using (var command = new SqlCommand(@"
                        INSERT INTO resident_address_tb (res_id,addr_id,is_owner) 
                        VALUES(@res,@addr,@owner);
                        ", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@res", res_id);
                        command.Parameters.AddWithValue("@addr", addressID);
                        command.Parameters.AddWithValue("@owner", 1);

                        residentAddressResult = await command.ExecuteNonQueryAsync();
                    }
                }

                return residentAddressResult < 0 ? 0 : 1;
            }
            catch (Exception)
            {
                return 0;
            }

        }

        //UPDATE ADDRESS AND RESIDENT
        public async Task<int> UpdateResident(Resident resident, List<Address> address)
        {
            SanitizeFormData(resident);
            var dt = GetTimeDate();

            var accountNumber = await AccountNumberCreation(address);


            int insertResidentResult = 0;
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var insertCommand = new SqlCommand(@"
                        UPDATE resident_tb set lname = @lname, fname = @fname, mname= @mname) 
                        ", connection, transaction))
                            {
                                //command.Parameters.AddWithValue("@id", res_id);
                                insertCommand.Parameters.AddWithValue("@lname", resident.Lname);
                                insertCommand.Parameters.AddWithValue("@fname", resident.Fname);
                                insertCommand.Parameters.AddWithValue("@mname", resident.Mname);

                                insertResidentResult = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

                                //return insertResidentResult;
                            }

                            if (insertResidentResult < 1)
                            {
                                throw new Exception();
                            }

                            var createAddress = await CreateAddress(insertResidentResult.ToString(), transaction, connection, address, accountNumber);

                            if (createAddress < 1)
                            {
                                throw new Exception();
                            }

                            transaction.Commit();

                            return 1;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            // Log the exception here
                            return 0;
                        }
                    }

                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        //UPDATE RESIDENT
        public async Task<string> UpdateResidentDetails(Resident resident)
        {
            SanitizeFormData(resident);
            var dt = GetTimeDate();
            var hashPassword = _hash.HashPassword(resident.Password);
            var phone = "63" + resident.Phone;
            var token = await CreateRandomToken();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var updateCommand = new SqlCommand(@"
                        UPDATE resident_tb set phone = @phone, email = @email, username = @user, password=@pass, token = @token
                        FROM resident_tb r
                        JOIN address_tb a ON r.res_id = a.res_id
                        WHERE account_number = @accnum", connection))
                    {
                        //command.Parameters.AddWithValue("@id", res_id);
                        updateCommand.Parameters.AddWithValue("@phone", phone);
                        updateCommand.Parameters.AddWithValue("@email", resident.Email);
                        updateCommand.Parameters.AddWithValue("@user", resident.Username);
                        updateCommand.Parameters.AddWithValue("@pass", hashPassword);
                        updateCommand.Parameters.AddWithValue("@token", token);
                        updateCommand.Parameters.AddWithValue("@accnum", resident.Account_Number);

                        int insertResidentResult = await updateCommand.ExecuteNonQueryAsync();

                        if (insertResidentResult < 1)
                        {
                            return null;
                        }

                        return token;

                    }

                }

            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<int> GetLatestResidentID()
        {
            try
            {
                var residentList = await _listRepository.ResidentList();

                // Use TryParse to safely convert string to int
                int resID = residentList
                    .Select(r => int.TryParse(r.Res_ID, out int parsedId) ? parsedId : 0)
                    .OrderByDescending(id => id)
                    .FirstOrDefault();

                // If no valid IDs found or list is empty, start from 1
                return resID > 0 ? resID + 1 : 1;
            }
            catch (Exception ex)
            {
                // Log the exception if possible
                return 1; // Return 1 instead of 0 to ensure a valid ID
            }
        }

        private async Task<string> AccountNumberCreation(string block, string lot, string residency)
        {
            try
            {
                string accountNumber = "";
                int residentId = await GetLatestResidentID();
                string date_residency = Regex.Replace(residency, "-", "");

                accountNumber = $"{block}{lot}{date_residency}{residentId}";

                return accountNumber;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<List<string>> AccountNumberCreation(List<Address> address)
        {
            try
            {
                List<string> accountNumber = new List<string>();
                int residentId = await GetLatestResidentID();
                foreach (var item in address)
                {
                    string date_residency = Regex.Replace(item.Date_Residency, "-", "");
                    accountNumber.Add($"{item.Block}{item.Lot}{date_residency}{residentId}");

                }

                return accountNumber;
            }
            catch (Exception)
            {
                return null;
            }
        }


        //READ
        public async Task<AuthClaims> GetResidentID(Resident resident)
        {
            try
            {
                string passFromDB = string.Empty;
                string resIDFromDB = string.Empty;
                string occupancy = string.Empty;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password
                    using (var command = new SqlCommand(@"
                        select * from resident_address_tb ra
                        JOIN resident_tb r ON ra.res_id = r.res_id
                        WHERE username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@user", await _sanitize.HTMLSanitizerAsync(resident.Username));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Retrieve the hashed password from the database
                                passFromDB = reader["password"].ToString() ?? string.Empty;
                                resIDFromDB = reader["res_id"].ToString() ?? string.Empty;
                                occupancy = reader.GetBoolean(3) ? "1" : "0";
                            }
                        }

                        // If no password was found, return false
                        if (string.IsNullOrEmpty(passFromDB))
                        {
                            return null; // Username not found or password missing
                        }

                        // Verify the input password with the hashed password from the database
                        if (_hash.VerifyPassword(passFromDB, resident.Password))
                        {
                            if (!string.IsNullOrEmpty(resIDFromDB) && !string.IsNullOrEmpty(occupancy))
                            {
                                AuthClaims claims = new AuthClaims
                                {
                                    ID = resIDFromDB,
                                    Role = occupancy
                                };
                                return claims;
                            } // Password is correct
                        }

                        // Password is incorrect
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                // Handle exception (logging, etc.)
                return null;
            }
        }

        //FETCH RENTER
        public async Task<List<Resident>> GetResidentAccount(string resident_id)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();


                var renter_list = residentList.Where(a => a.Res_ID == resident_id).ToList();

                return renter_list;

            }
            catch (Exception)
            {
                return null;
            }
        }

        //UPDATE INSIDE SYSTEM
        public async Task<int> UpdateResidentInformation(string resident_id, Resident resident)
        {
            try
            {
                var query = new StringBuilder();
                var updateColumns = new List<string>();
                var parameters = new List<SqlParameter>();

                // Sanitize and prepare parameters conditionally
                if (!string.IsNullOrEmpty(resident.Phone))
                {
                    var sanitizedPhone = "63" + await _sanitize.HTMLSanitizerAsync(resident.Phone);
                    updateColumns.Add("phone = @phone");
                    parameters.Add(new SqlParameter("@phone", sanitizedPhone));
                }

                if (!string.IsNullOrEmpty(resident.Email))
                {
                    var sanitizedEmail = await _sanitize.HTMLSanitizerAsync(resident.Email);
                    updateColumns.Add("email = @email");
                    parameters.Add(new SqlParameter("@email", sanitizedEmail));
                }

                if (!string.IsNullOrEmpty(resident.Username))
                {
                    var sanitizedUsername = await _sanitize.HTMLSanitizerAsync(resident.Username);
                    updateColumns.Add("username = @user");
                    parameters.Add(new SqlParameter("@user", sanitizedUsername));
                }

                if (!string.IsNullOrEmpty(resident.Password))
                {
                    var hashedPassword = _hash.HashPassword(await _sanitize.HTMLSanitizerAsync(resident.Password));
                    updateColumns.Add("password = @pass");
                    parameters.Add(new SqlParameter("@pass", hashedPassword));
                }

                // Check if there are any updates to make
                if (updateColumns.Count == 0)
                {
                    return 0; // No updates to perform
                }

                // Build the query
                query.AppendLine("UPDATE resident_tb SET");
                query.AppendLine(string.Join(", ", updateColumns));
                query.AppendLine("WHERE res_id = @id");
                parameters.Add(new SqlParameter("@id", resident_id));

                // Execute the update
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Consider logging the exception
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
                        command.Parameters.AddWithValue("@user", await _sanitize.HTMLSanitizerAsync(resident.Username));

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
                var username = await _sanitize.HTMLSanitizerAsync(resident.Username);
                var password = await _sanitize.HTMLSanitizerAsync(resident.Password);

                string passFromDB = string.Empty;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password
                    using (var command = new SqlCommand("SELECT password FROM resident_tb WHERE username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@user", username);

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
                        if (_hash.VerifyPassword(passFromDB, password))
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
        public async Task<string> GetEmailByToken(string token)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();

                var email = residentList.Where(t => t.Account_Token == token).Select(e => e.Email).FirstOrDefault();

                return email;
                //using (var connection = await _dbConnect.GetOpenConnectionAsync())
                //{
                //    using (var command = new SqlCommand("SELECT email FROM resident_tb WHERE verification_token = @token", connection))
                //    {
                //        command.Parameters.AddWithValue("@token", token);

                //        using (var reader = await command.ExecuteReaderAsync())
                //        {
                //            if (await reader.ReadAsync())
                //            {
                //                email = reader["email"].ToString() ?? string.Empty;
                //            }
                //        }

                //        return email;
                //    }
                //}
            }
            catch (Exception)
            {
                return null;
            }
        }

        //CHECK ACCOUNT NUMBER EXIST
        public async Task<bool> AccountNumberExist(Resident resident)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();
                var addressList = await _listRepository.AddressList();

                return addressList.Any(
                    a => a.Account_Number == resident.Account_Number);
            }
            catch (Exception)
            {
                return false;
            }
        }

        ////CHECK ACCOUNT NUMBER EXIST
        //public async Task<bool> IsNameExist(Resident resident)
        //{
        //    try
        //    {
        //        var residentList = await _listRepository.ResidentList();

        //        return residentList.Any(
        //            a.Lname.ToLower() == resident.Lname.ToLower()
        //            && a.Fname.ToLower() == resident.Fname.ToLower()
        //            && a.Mname.ToLower() == resident.Mname.ToLower());
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //VALIDATE TOKEN EXISTENCE
        public async Task<bool> IsTokenExist(string token)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();

                return residentList.Any(t => t.Account_Token == token);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> IsAccountVerified(Resident resident)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();
                var addressList = await _listRepository.AddressList();

                if (addressList.Any(a => a.Account_Number == resident.Account_Number))
                {
                    return residentList.Where(a => a.Lname.ToLower() == resident.Lname.ToLower()
                            && a.Fname.ToLower() == resident.Fname.ToLower()
                            && a.Mname.ToLower() == resident.Mname.ToLower()).Any(s => s.Is_Activated == true);
                }

                return false;

            }
            catch (Exception)
            {

                return false;
            }
        }

        public async Task<bool> IsUsernameExist(string username)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();

                return residentList.Any(a => a.Username == username);

            }
            catch (Exception)
            {

                return false;
            }
        }

        public async Task<bool> IsEmailExist(string email)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();

                return residentList.Any(a => a.Username == email);

            }
            catch (Exception)
            {

                return false;
            }
        }

        public async Task<bool> IsPasswordCorrect(string resident_id, string password)
        {
            try
            {
                var residentList = await _listRepository.ResidentList();
                var passFromDB = residentList.Where(r => r.Res_ID == resident_id).Select(p => p.Password).FirstOrDefault();

                return _hash.VerifyPassword(passFromDB, password);

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

                    using (var transaction = connection.BeginTransaction())
                    {
                        // Generate new verification code
                        var codeGenerate = await _loginRepository.GenerateVerificationCode(4);
                        if (string.IsNullOrEmpty(codeGenerate[0].Code))
                        {
                            return 0; // Failed to generate code
                        }

                        // Update resident_tb with new token and expiration
                        using (var command = new SqlCommand("UPDATE resident_tb SET password_reset_token = @token, reset_token_expire = @time WHERE email = @email", connection, transaction))
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

                                try
                                {
                                    int emailResult = await _loginRepository.SendEmail(sendEmail, codeGenerate[0].Code);

                                    // Log email sending status
                                    if (emailResult > 0)
                                    {
                                        Console.WriteLine($"Email sent successfully to {email}");

                                        transaction.Commit();
                                        return 1; // Success
                                    }
                                    else
                                    {
                                        throw new Exception();
                                    }
                                }
                                catch (Exception)
                                {
                                    transaction.Rollback();
                                    return 0;
                                }
                            }
                        }
                    }



                }

                return 0; // Fail (no rows updated)
            }
            catch (Exception ex)
            {
                // Consider logging the exception here
                Console.WriteLine(ex.Message);
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
                                    using (var updateCommand = new SqlCommand("UPDATE resident_tb SET verified_at = @verifiedAt, is_activated = @activate WHERE email = @email", connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@verifiedAt", DateTime.Now);
                                        updateCommand.Parameters.AddWithValue("@activate", 1);
                                        updateCommand.Parameters.AddWithValue("@email", email);
                                        var updateResult = await updateCommand.ExecuteNonQueryAsync();
                                        if (updateResult > 0)
                                        {
                                            // Clear the password_reset_token and reset_token_expire columns
                                            using (var clearCommand = new SqlCommand("UPDATE resident_tb SET token = @vtoken, password_reset_token = @token, reset_token_expire = @expire WHERE email = @email", connection))
                                            {
                                                clearCommand.Parameters.AddWithValue("@vtoken", DBNull.Value);
                                                clearCommand.Parameters.AddWithValue("@token", DBNull.Value);
                                                clearCommand.Parameters.AddWithValue("@expire", DBNull.Value);
                                                clearCommand.Parameters.AddWithValue("@email", email);
                                                await clearCommand.ExecuteNonQueryAsync();

                                                var notifStaff = new Notification
                                                {
                                                    //Address_ID = waterReading.Address_ID,
                                                    Title = "Register Account",
                                                    Message = "New account registered.",
                                                    Url = "/kvhai/staff/admin/accounts",
                                                    Message_Type = "admin"
                                                };

                                                //var notificationAdminResult = await _notificationRepository.SendNotificationToAdmin(notifStaff);
                                                //await _staffhubContext.Clients.All.SendAsync("ReceivedNewRegisterAccount");
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

                        //command.Parameters.AddWithValue("@occupy", resident.Occupancy);

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
        }
        #region Resident Address
        //WITHOUT SEARCH
        public async Task<List<AddressWithResident>> GetAllResidentAsync(int offset, int limit, string verified = "true")
        {
            try
            {
                var residents = new List<AddressWithResident>();
                var resID = "";
                var streetID = "";

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand($@"
            SELECT r.res_id, r.lname, r.fname, r.mname, r.phone, r.email, r.username, r.password, r.verified_at,
       a.addr_id, a.block, a.lot, a.st_id--, a.is_verified
FROM resident_tb r
JOIN address_tb a ON r.res_id = a.res_id  
--WHERE r.verified_at IS NOT NULL AND a.is_verified = @verify
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
                                    //existingResident.Is_Verified?.Add(reader["is_verified"]?.ToString() ?? string.Empty);
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
                                        AddressID = new List<string> { reader["addr_id"]?.ToString() ?? string.Empty },
                                        Block = new List<string> { reader["block"]?.ToString() ?? string.Empty },
                                        Lot = new List<string> { reader["lot"]?.ToString() ?? string.Empty },
                                        Street_Name = new List<string> { st_name ?? string.Empty },
                                        //Is_Verified = new List<string> { reader["is_verified"]?.ToString() ?? string.Empty }
                                    };

                                    residents.Add(_resident);
                                }
                            }
                        }
                    }
                }

                return residents;
            }
            catch (Exception)
            {

                return null;
            }

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
                    WHERE r.verified_at IS NOT NULL AND a.is_verified like @verify AND concat(r.lname,' ',fname,' ',mname) like @search
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
                    WHERE r.verified_at IS NOT NULL AND a.is_verified like @verify
                    ORDER BY r.res_id 
                    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
            }


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@verify", "%" + is_verified + "%");
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
        #endregion

        #region Accounts
        //WITHOUT SEARCH
        public async Task<List<AddressWithResident>> GetAllResidentAsyncAccount(int offset, int limit)
        {
            var residents = new List<AddressWithResident>();
            var resID = "";
            var streetID = "";

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand($@"
                    SELECT r.res_id, a.account_number, r.lname, r.fname, r.mname, r.phone, r.email, r.username, r.password,r.is_activated , 
a.block,a.lot,s.st_name, a.date_residency, r.verified_at
                    from resident_tb r 
                    JOIN address_tb a ON r.res_id = a.res_id
                    JOIN street_tb s ON a.st_id = s.st_id
                     ORDER BY res_id
                    OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY", connection))
                    //    WHERE verified_at IS NOT NULL
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Create a new resident object and add the first address details
                                var _resident = new AddressWithResident
                                {
                                    Res_ID = reader.GetInt32(0).ToString(),
                                    Account_Number = reader.GetString(1).ToString(),
                                    Lname = reader.GetString(2).ToString(),
                                    Fname = reader.GetString(3).ToString(),
                                    Mname = reader.GetString(4).ToString(),
                                    Phone = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                    Email = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    Username = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                    Password = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                    Is_Activated = reader.GetBoolean(9) ? "True" : "False",
                                    _Block = reader.GetString(10).ToString(),
                                    _Lot = reader.GetString(11).ToString(),
                                    _NameStreet = reader.GetString(12).ToString(),
                                    Date_Residency = reader.GetDateTime(13).ToString("dd MMM yyyy"),
                                    Verified_At = reader.IsDBNull(14) ? string.Empty : reader.GetDateTime(14).ToString("dd MMM yyyy"),
                                };

                                residents.Add(_resident);
                            }
                        }
                    }
                }

                return residents;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }


        //WITH SEARCH
        public async Task<List<AddressWithResident>> GetAllResidentAsyncAccount(int offset, int limit, string? search = "")
        {
            //var residents = new List<Resident>();
            string query = "";
            var residents = new List<AddressWithResident>();
            var resID = "";
            var streetID = "";

            query = $@"
                       SELECT r.res_id, a.account_number, r.lname, r.fname, r.mname, r.phone, r.email, r.username, r.password,r.is_activated , 
                     a.block,a.lot,s.st_name, a.date_residency, r.verified_at
                     from resident_tb r 
                     JOIN address_tb a ON r.res_id = a.res_id
                     JOIN street_tb s ON a.st_id = s.st_id
                    WHERE  concat(r.lname,' ',r.fname,' ',r.mname) like @search
                    ORDER BY r.res_id 
                    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@offset", offset);
                    command.Parameters.AddWithValue("@limit", limit);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            // Create a new resident object and add the first address details
                            var _resident = new AddressWithResident
                            {
                                Res_ID = reader.GetInt32(0).ToString(),
                                Account_Number = reader.GetString(1).ToString(),
                                Lname = reader.GetString(2).ToString(),
                                Fname = reader.GetString(3).ToString(),
                                Mname = reader.GetString(4).ToString(),
                                Phone = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Email = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                Username = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                Password = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                Is_Activated = reader.GetBoolean(9) ? "True" : "False",
                                _Block = reader.GetString(10).ToString(),
                                _Lot = reader.GetString(11).ToString(),
                                _NameStreet = reader.GetString(12).ToString(),
                                Date_Residency = reader.GetDateTime(13).ToString("dd MMM yyyy"),
                                Verified_At = reader.IsDBNull(14) ? string.Empty : reader.GetDateTime(14).ToString("dd MMM yyyy"),
                            };

                            residents.Add(_resident);
                        }
                    }
                }
            }

            return residents;
        }

        //COUNT DATA W/ SEARCH
        public async Task<int> CountResidentDataAccount(string search = "")
        {
            int result = 0;

            string query = "";
            query = $@"
                     SELECT Count(*)
                    FROM resident_tb r
                    WHERE concat(r.lname,' ',fname,' ',mname) like @search";


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    var count = await command.ExecuteScalarAsync();

                    return Convert.ToInt32(count);
                }
            }
        }

        #endregion

        //COUNT RESIDENT W/O Search
        public async Task<int> CountResidentData(string active)
        {
            int result = 0;
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand($"SELECT COUNT(*) FROM resident_tb", connection))
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
                    WHERE r.verified_at IS NOT NULL AND a.is_verified like @verify AND concat(r.lname,' ',fname,' ',mname) like @search";
            }
            else
            {
                query = $@"
                    SELECT count(*)
                    FROM resident_tb r
                    JOIN address_tb a ON r.res_id = a.res_id  
                    WHERE r.verified_at IS NOT NULL AND a.is_verified like @verify AND concat(block,' ',lot) like @search
                    ";
            }

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@search", "%" + search + "%");
                    command.Parameters.AddWithValue("@verify", "%" + active + "%");
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

        private async Task<string> CreateRandomToken(string columnName = "token")
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
