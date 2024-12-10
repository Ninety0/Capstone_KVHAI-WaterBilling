using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace KVHAI.Repository
{
    public class AddressRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;
        private readonly StreetRepository _streetRepository;
        private readonly ImageUploadRepository _uploadRepository;
        private readonly RequestDetailsRepository _requestDetailsRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly ListRepository _listRepository;
        private readonly ResidentAddressRepository _residentAddressRepository;


        private bool DataExist = false;
        private int CounterDataExistence = 0;

        public AddressRepository(DBConnect dBConnect, InputSanitize inputSanitize, StreetRepository streetRepository, ImageUploadRepository uploadRepository, RequestDetailsRepository requestDetailsRepository, NotificationRepository notificationRepository, ListRepository listRepository, ResidentAddressRepository residentAddressRepository)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _streetRepository = streetRepository;
            _uploadRepository = uploadRepository;
            _requestDetailsRepository = requestDetailsRepository;
            _notificationRepository = notificationRepository;
            _listRepository = listRepository;
            _residentAddressRepository = residentAddressRepository;
        }

        //READ
        public async Task<List<ResidentAddress>> ResidentAddressList()
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


        //CREATE
        public async Task<List<Address>> CreateAddress(string res_id, List<Address> addressess, SqlTransaction transaction, SqlConnection connection)
        {
            try
            {
                var addressIDList = new List<Address>();
                var addressID = 0;

                foreach (var address in addressess)
                {
                    // Use the same connection and transaction for checking if the address exists
                    var isExist = await IsAddressExist(res_id, address.Block, address.Lot, address.Street_ID, connection, transaction);

                    if (isExist)
                    {
                        CounterDataExistence++;
                        continue; // Skip this address and continue to the next
                    }

                    using (var command = new SqlCommand(@"
                        INSERT INTO address_tb (res_id,block,lot,st_id,location, is_verified, register_at) 
                        VALUES(@res,@blk,@lot,@st,@location,'false', @date);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                    {
                        int _location = 0;
                        int _block;
                        // Safely try to parse the block value, defaulting to 0 if it fails
                        if (int.TryParse(address.Block, out _block))
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
                        command.Parameters.AddWithValue("@blk", address.Block ?? "");
                        command.Parameters.AddWithValue("@lot", address.Lot ?? "");
                        command.Parameters.AddWithValue("@st", address.Street_ID);
                        command.Parameters.AddWithValue("@location", _location);
                        command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        addressID = (int?)(await command.ExecuteScalarAsync()) ?? 0;
                        //intDict.Add("Location", _location);
                        //intDict.Add("ID", resID);
                        var addressModel = new Address { Address_ID = addressID };

                        addressIDList.Add(addressModel);
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

                        await command.ExecuteNonQueryAsync();
                    }
                }
                return addressIDList;

            }
            catch (Exception)
            {
                return null;
            }

        }
        #region FOR RENTER
        public async Task<int> InsertRenterAddress(string res_id, string address_id)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    //OWNET DATA TYPE BIT
                    //1 - homeowner
                    //0 - renter

                    //STATUS - 0 (Pending), 1 (Accepted), or 2 (Rejected) 
                    using (var command = new SqlCommand(@"
                        INSERT INTO resident_address_tb (res_id,addr_id,is_owner,status,request_date) 
                        VALUES(@res,@addr,@owner,@status,@req);", connection))
                    {
                        command.Parameters.AddWithValue("@res", res_id);
                        command.Parameters.AddWithValue("@addr", address_id);
                        command.Parameters.AddWithValue("@owner", 0);//renter
                        command.Parameters.AddWithValue("@status", 0);//pending
                        command.Parameters.AddWithValue("@req", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception)
            {

                return 0;
            }
        }

        public async Task<int> GetAddressIDForRenter(ResidentAddress address)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT addr_id FROM address_tb 
                        WHERE block = @blk AND lot = @lot AND st_id = @st", connection)) // Removed extra parenthesis
                    {
                        command.Parameters.AddWithValue("@blk", address.Block);
                        command.Parameters.AddWithValue("@lot", address.Lot);
                        command.Parameters.AddWithValue("@st", address.ID);

                        int result = (int)await command.ExecuteScalarAsync();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"Error: {ex.Message}");
                return 0; // Return false in case of an error
            }
        }
        #endregion


        // CHECK IF EXIST
        public async Task<bool> IsAddressExist(string res_id, string block, string lot, int st_id, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                using (var command = new SqlCommand(@"
            SELECT COUNT(*) 
            FROM address_tb 
            WHERE res_id = @id AND block = @blk AND lot = @lot AND st_id = @st", connection, transaction)) // Removed extra parenthesis
                {
                    command.Parameters.AddWithValue("@id", res_id);
                    command.Parameters.AddWithValue("@blk", block);
                    command.Parameters.AddWithValue("@lot", lot);
                    command.Parameters.AddWithValue("@st", st_id);

                    int result = (int)await command.ExecuteScalarAsync();
                    return result > 0; // Return true if the address exists, otherwise false
                }
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"Error: {ex.Message}");
                return false; // Return false in case of an error
            }
        }

        // CHECK IF EXIST
        public async Task<bool> IsAddressExist(string block, string lot)
        {
            try
            {
                var addressList = await _listRepository.AddressList();
                return addressList.Any(a =>
                    a.Block == block &&
                    a.Lot == lot);
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }


        public async Task<int> GetResidentIdByAddressId(string address_id)
        {
            int id = 0;
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT res_id FROM address_tb WHERE addr_id = @addr_id", connection))
                    {
                        command.Parameters.AddWithValue("@addr_id", address_id);

                        var result = await command.ExecuteScalarAsync();
                        id = Convert.ToInt32(result);
                    }
                }
                return id;
            }
            catch (Exception)
            {
                return 0;
            }

        }

        //not used


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

        //GET NAME BY BLOCK AND LOT
        public async Task<List<ResidentAddress>> GetName(ResidentAddress residentAddress)
        {
            try
            {
                var resident = new List<ResidentAddress>();
                string street = string.IsNullOrEmpty(residentAddress.Street) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Street);

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        select r.res_id,addr_id, lname,fname,mname, st_name from address_tb a
                        JOIN resident_tb r ON a.res_id = r.res_id
                        JOIN street_tb s ON a.st_id = s.st_id
                        WHERE block= @block AND lot= @lot AND st_name = @st", connection))
                    {
                        command.Parameters.AddWithValue("@block", string.IsNullOrEmpty(residentAddress.Block) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Block));

                        command.Parameters.AddWithValue("@lot", string.IsNullOrEmpty(residentAddress.Lot) ? "" : await _sanitize.HTMLSanitizerAsync(residentAddress.Lot));

                        command.Parameters.AddWithValue("@st", street);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string name = string.Join(", ", reader["lname"].ToString(), reader["fname"].ToString(), reader["mname"].ToString());
                                var _resident = new ResidentAddress
                                {
                                    ID = Convert.ToInt32(reader["res_id"].ToString()),
                                    Address_ID = Convert.ToInt32(reader["addr_id"].ToString()),
                                    Resident_ID = Convert.ToInt32(reader["res_id"].ToString()),
                                    Name = name
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

        public async Task<int> GetCountByLocationWithReading(string location = "")
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

        public async Task<int> CreateAddressandUploadImage(string residentID, List<Address> addressess, List<IFormFile> file, string webRootPath)
        {
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var streetResult = await _streetRepository.GetStreetID(addressess);

                        if (streetResult?.Any() == true)
                        {
                            var modelAddressList = new List<Address>();//create new model address list
                            for (int i = 0; i < addressess.Count; i++)//loop them to combine
                            {
                                var _address = new Address
                                {
                                    Block = addressess[i].Block,
                                    Lot = addressess[i].Lot,
                                    Street_ID = streetResult[i].Street_ID
                                };
                                modelAddressList.Add(_address);//add the model in the list
                            }

                            var addressResult = await CreateAddress(residentID, modelAddressList, transaction, connection);

                            if (CounterDataExistence == modelAddressList.Count)
                            {
                                return 2;
                            }

                            if (addressResult == null || addressResult.Count < 1)
                            {
                                throw new Exception("Error Saving Address");
                            }

                            if (addressResult.Count > 0)
                            {
                                int imageResult = await _uploadRepository.ImageUpload(file, webRootPath, addressResult, transaction, connection, residentID);


                                if (imageResult == 0)
                                {
                                    throw new Exception("Image upload failed");
                                }


                            }
                            else
                            {
                                throw new Exception("There was an error registering address");
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }


                        //int location = residentValues["Location"];
                        // Create notification for the resident

                        var emp = await _listRepository.EmployeeList();
                        var emp_id = emp.Where(r => r.Role == "admin").Select(e => e.Emp_ID).ToList();
                        var notif = new Notification
                        {
                            Title = "Register Address",
                            Message = "New address need to verify",
                            Url = "/kvhai/staff/resident-address/",
                            Message_Type = "Admin",
                            ListEmployee_ID = emp_id
                        };

                        await _notificationRepository.SendNotificationToAdmin(notif);
                        //await _hubContext.Clients.All.SendAsync("ReceivedAddressNotificationToAdmin", notification.Title, notification.Resident_ID);

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

        public async Task<int> UpdateAddressStatus(int addresID, string status)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("UPDATE address_tb set is_verified =@status WHERE addr_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", addresID);
                        command.Parameters.AddWithValue("@status", status);

                        int residentID = await GetResidentIdByAddressId(addresID.ToString());

                        var residentIDS = await _residentAddressRepository.GetResidentID(addresID.ToString());
                        var notif = new Notification
                        {
                            Address_ID = addresID.ToString(),
                            Resident_ID = residentID.ToString(),
                            Title = "My Address",
                            Message = "Your address was verified!",
                            Url = "/kvhai/resident/my-address",
                            Message_Type = "Personal",
                            ListResident_ID = residentIDS
                        };

                        await _notificationRepository.InsertNotificationPersonalToUser(notif);
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
                    using (var command = new SqlCommand("SELECT * FROM address_tb WHERE res_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", resID);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int addr_id = Convert.ToInt32(reader["addr_id"].ToString());
                                int res_id = Convert.ToInt32(reader["res_id"].ToString());
                                int request_id = await _requestDetailsRepository.GetRequestID(addr_id.ToString(), resID.ToString());

                                var address = new Address
                                {
                                    Address_ID = addr_id,
                                    Resident_ID = res_id,
                                    Block = reader["block"].ToString() ?? String.Empty,
                                    Lot = reader["lot"].ToString() ?? String.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString()),
                                    Remove_Request_Token = reader["remove_request_token"].ToString() ?? string.Empty,
                                    Request_ID = request_id,
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

        public async Task<int> GetAddressIDByResidentID(string resident_id, string role)
        {
            try
            {
                var address_id = 0;
                var address = await _listRepository.AddressList();
                var resident_address = await _listRepository.ResidentAddressList();

                if (role == "1")
                {
                    address_id = address
                                .Where(ra => ra.Resident_ID.ToString() == resident_id)
                                .Select(ra => ra.Address_ID)
                                .FirstOrDefault();
                }
                else
                {
                    address_id = resident_address
                                .Where(ra => ra.Resident_ID.ToString() == resident_id)
                                .Select(ra => ra.Address_ID)
                                .FirstOrDefault();
                }

                return address_id;
            }
            catch (Exception ex)
            {
                // Log exception for debugging
                Console.WriteLine($"Error: {ex.Message}");
                return 0; // Return false in case of an error
            }
        }

        public async Task<int> RequestRemoveTokenUpdate(string addresID, string residentID)
        {
            /*
             aid = address id
             rid = resident id
             */
            try
            {
                var result = 0;
                var token = await CreateRandomToken();
                var currentDateTime = DateTime.Now;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Step 1: Update address table to include remove_request_token
                            using (var updateCommand = new SqlCommand(@"
                        UPDATE address_tb 
                        SET remove_request_token = @token 
                        WHERE addr_id = @aid 
                        AND res_id = @rid", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@aid", addresID);
                                updateCommand.Parameters.AddWithValue("@rid", residentID);
                                updateCommand.Parameters.AddWithValue("@token", token);

                                await updateCommand.ExecuteNonQueryAsync();

                                // If the token was successfully added
                                if (!string.IsNullOrEmpty(token))
                                {
                                    /* status type:
                                     0 = pending
                                     1 = approved
                                     2 = rejected
                                     3 = cancel
                                     */
                                    string requestType = "Removal of address";
                                    int status = 0;
                                    string comments = "";  // Initially no comments

                                    // Step 2: Insert new request into request_tb
                                    using (var insertCommand = new SqlCommand(@"
                                INSERT INTO request_tb(res_id,addr_id, request_type, date_created, status, status_updated)   VALUES(@res_id, @addr_id, @type, @date_created, @status, @status_updated);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                                    {
                                        insertCommand.Parameters.AddWithValue("@res_id", residentID);
                                        insertCommand.Parameters.AddWithValue("@addr_id", addresID);
                                        insertCommand.Parameters.AddWithValue("@type", requestType);
                                        insertCommand.Parameters.AddWithValue("@date_created", currentDateTime);
                                        insertCommand.Parameters.AddWithValue("@status", status.ToString());
                                        insertCommand.Parameters.AddWithValue("@status_updated", currentDateTime); // Track when status was set
                                        result = (int)await insertCommand.ExecuteScalarAsync();
                                    }
                                }
                            }

                            // Step 3: Commit the transaction if everything is successful
                            transaction.Commit();
                            return result;
                        }
                        catch (Exception)
                        {
                            // If something goes wrong, roll back the transaction
                            transaction.Rollback();
                            return 0;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }


        public async Task<int> CancelRequestRemoveTokenUpdate(string addresID, string residentID, string request_id = "")
        {
            try
            {
                /* status type:
                    0 = pending
                    1 = approved
                    2 = rejected
                    3 = cancel
                    */
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update the address_tb to nullify the token and token date
                            using (var command = new SqlCommand(@"
                                UPDATE address_tb 
                                SET remove_request_token = @token  
                                WHERE addr_id = @aid AND res_id = @rid", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@aid", addresID);
                                command.Parameters.AddWithValue("@rid", residentID);
                                command.Parameters.AddWithValue("@token", DBNull.Value);

                                await command.ExecuteNonQueryAsync();
                            }

                            // Update the request_tb to set the status as 'Canceled' (e.g., status = 2) and add comments
                            using (var updateRequestCommand = new SqlCommand(@"
                                UPDATE request_tb 
                                SET status = @status, comments = @comments, status_updated = @statusUpdated
                                WHERE request_id = @rid", connection, transaction)) // Assuming '0' is for pending
                            {
                                int canceledStatus = 3; // Assuming '2' is the status for canceled
                                string comments = "Request canceled by resident";
                                DateTime statusUpdated = DateTime.Now;

                                updateRequestCommand.Parameters.AddWithValue("@rid", request_id);
                                updateRequestCommand.Parameters.AddWithValue("@status", canceledStatus);
                                updateRequestCommand.Parameters.AddWithValue("@comments", comments);
                                updateRequestCommand.Parameters.AddWithValue("@statusUpdated", statusUpdated);

                                await updateRequestCommand.ExecuteNonQueryAsync();
                            }

                            // Commit the transaction if both updates succeed
                            transaction.Commit();
                            return 1;
                        }
                        catch (Exception)
                        {
                            // Rollback the transaction if something goes wrong
                            transaction.Rollback();
                            return 0;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }


        public async Task<bool> CheckRemoveTokenExist(string addresID, string residentID)
        {
            /*
             aid = address id
             rid = resident id
             */
            try
            {
                var token = await CreateRandomToken();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT remove_request_token FROM address_tb WHERE addr_id = @aid AND res_id =@rid", connection))
                    {
                        command.Parameters.AddWithValue("@aid", addresID);
                        command.Parameters.AddWithValue("@rid", residentID);

                        var result = await command.ExecuteScalarAsync();
                        if (string.IsNullOrEmpty(result?.ToString()))
                        {
                            return false;//if empty, the existence are none 
                        }
                    }
                }
                return true;//if has value, the existence are there
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<string> CreateRandomToken(string tableName = "")
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
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


        //APPROVE REQUEST DELETE
        public async Task<int> ConfirmAndRemoveAddress(string addressID, string residentID, string token)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check if token matches
                            using (var checkCommand = new SqlCommand("SELECT remove_request_token FROM address_tb WHERE addr_id = @aid AND res_id = @rid", connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@aid", addressID);
                                checkCommand.Parameters.AddWithValue("@rid", residentID);

                                var tokenInDB = (string)await checkCommand.ExecuteScalarAsync();

                                if (tokenInDB != token)
                                {
                                    return -1; // Token mismatch
                                }
                            }

                            // Token matches, proceed with deletion
                            using (var deleteCommand = new SqlCommand("DELETE FROM address_tb WHERE addr_id = @aid AND res_id = @rid", connection, transaction))
                            {
                                deleteCommand.Parameters.AddWithValue("@aid", addressID);
                                deleteCommand.Parameters.AddWithValue("@rid", residentID);

                                int result = await deleteCommand.ExecuteNonQueryAsync();
                                transaction.Commit();
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine(ex.Message);
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        //GET NAME BY ID
        public async Task<string> GetNameByID(string id)
        {
            try
            {
                var name = "";
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    // Query only by username to get the hashed password
                    using (var command = new SqlCommand("SELECT lname,fname,mname FROM resident_tb WHERE res_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

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

        public async Task<List<ResidentAddress>> GetNewRegisteringAddress()
        {
            try
            {
                var residentAddress = new List<ResidentAddress>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    select a.addr_id,r.res_id,s.st_id,a.block,a.lot,s.st_name,CONCAT(r.lname,', ',r.fname,', ',r.mname) as name, a.register_at from address_tb a 
                    JOIN resident_tb r ON a.res_id = r.res_id
                    JOIN street_tb s ON a.st_id = s.st_id
                    WHERE CONVERT(VARCHAR, a.register_at, 23) LIKE '%2024-01%' ORDER BY a.register_at DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var _residentAddress = new ResidentAddress
                                {
                                    Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Street_ID = reader.GetInt32(2),
                                    Block = reader.GetString(3),
                                    Lot = reader.GetString(4),
                                    Street = reader.GetString(5),
                                    Name = reader.GetString(6),
                                    Register_At = reader.GetDateTime(7).ToString("MMM dd yyyy hh:mm t"),
                                };
                                residentAddress.Add(_residentAddress);
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
    }
}
