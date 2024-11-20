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
        private readonly NotificationRepository _notificationRepository;
        private readonly ResidentAddressRepository _residentAddressRepository;

        public RequestDetailsRepository(DBConnect dBConnect, InputSanitize inputSanitize, StreetRepository streetRepository, NotificationRepository notificationRepository, ResidentAddressRepository residentAddressRepository)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _streetRepository = streetRepository;
            _notificationRepository = notificationRepository;
            _residentAddressRepository = residentAddressRepository;
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

        public async Task<List<RequestDetails>> GetPendingRemovalRequests(string status = "", string date = "")
        {
            var pendingAddresses = new List<RequestDetails>();
            string query = "";
            if (string.IsNullOrEmpty(date))
            {
                date = DateTime.Now.ToString("yyyy-MM-dd");
            }


            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT * 
                        FROM request_tb 
                        WHERE status LIKE @status AND CAST(date_created AS DATE) = @date", connection))
                    {
                        command.Parameters.AddWithValue("@status", "%" + status + "%");
                        command.Parameters.AddWithValue("@date", date);

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

        public async Task<List<RequestDetails>> GetPendingRemovalRequestsDateFilter(string date = "")
        {
            var pendingAddresses = new List<RequestDetails>();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT * FROM request_tb WHERE status LIKE @status
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

        public async Task<int> UpdateRequestStatus(RequestDetails request)
        {
            try
            {
                int result = 0;
                string message = "";

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var command = new SqlCommand(@"
                                UPDATE request_tb set status= @status, status_updated = @update, comments = @comment  WHERE addr_id = @aid AND res_id = @rid AND request_id = @req_id", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@aid", request.Address_ID);
                                command.Parameters.AddWithValue("@rid", request.Resident_ID);
                                command.Parameters.AddWithValue("@req_id", request.Request_ID);
                                command.Parameters.AddWithValue("@status", request.Status);
                                command.Parameters.AddWithValue("@update", DateTime.Now);
                                command.Parameters.AddWithValue("@comment", "Approve by the admin");

                                result = await command.ExecuteNonQueryAsync();

                                if (result > 0)
                                {
                                    //APPROVE
                                    if (request.Status == 1 && request.RequestType.Contains("Removal of address"))
                                    {
                                        int confirmRequest = await ConfirmRequestAction(request, connection, transaction);

                                        message = "Your address was removed.";
                                        if (confirmRequest < 1)
                                        {
                                            throw new Exception();
                                        }
                                    }
                                    //REJECT
                                    else if (request.Status == 2 && request.RequestType.Contains("Removal of address"))
                                    {
                                        var rejectStatus = await RejectRequest(request, connection, transaction);
                                        message = "Your request was rejected.";

                                        if (rejectStatus < 1)
                                        {
                                            throw new Exception();
                                        }
                                    }
                                }



                                // Commit the transaction only if everything succeeded
                                transaction.Commit();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Rollback if any error occurs
                            transaction.Rollback();
                            Console.WriteLine("Transaction rolled back due to: " + ex.Message);
                            return 0;
                        }
                    }
                }

                var residentIDS = await _residentAddressRepository.GetResidentID(request.Address_ID.ToString());

                var notif = new Notification
                {
                    Address_ID = request.Address_ID.ToString(),
                    Resident_ID = request.Resident_ID.ToString(),
                    Title = "Request Action",
                    Message = message,
                    Url = "/kvhai/resident/my-address",
                    Message_Type = "Personal",
                    ListResident_ID = residentIDS
                };

                await _notificationRepository.InsertNotificationPersonal(notif);
                return result;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // UPDATED ConfirmRequestAction
        public async Task<int> ConfirmRequestAction(RequestDetails request, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                int deleteImg = 0;
                int raRequestResult = 0;
                int requestResult = 0;
                if (request.Status == 1) // REQUEST APPROVE DELETE
                {
                    using (var deleteCommand = new SqlCommand("DELETE FROM proof_img_tb WHERE addr_id = @aid", connection, transaction))
                    {
                        deleteCommand.Parameters.AddWithValue("@aid", request.Address_ID);
                        deleteImg = await deleteCommand.ExecuteNonQueryAsync();
                    }

                    if (deleteImg > 0)
                    {
                        using (var deleteCommand = new SqlCommand("DELETE FROM resident_address_tb WHERE addr_id = @aid AND res_id = @rid", connection, transaction))
                        {
                            deleteCommand.Parameters.AddWithValue("@aid", request.Address_ID);
                            deleteCommand.Parameters.AddWithValue("@rid", request.Resident_ID);

                            raRequestResult = await deleteCommand.ExecuteNonQueryAsync();
                        }
                    }

                    if (raRequestResult > 0 && deleteImg > 0)
                    {
                        using (var deleteCommand = new SqlCommand("DELETE FROM address_tb WHERE addr_id = @aid AND res_id = @rid", connection, transaction))
                        {
                            deleteCommand.Parameters.AddWithValue("@aid", request.Address_ID);
                            deleteCommand.Parameters.AddWithValue("@rid", request.Resident_ID);

                            requestResult = await deleteCommand.ExecuteNonQueryAsync();

                        }



                        //var notif = new Notification
                        //{
                        //    Address_ID = request.Address_ID.ToString(),
                        //    Resident_ID = request.Resident_ID.ToString(),
                        //    Title = "Request Action",
                        //    Message = "Your address was removed!",
                        //    Url = "/kvhai/resident/my-address",
                        //    Message_Type = "Personal",
                        //};

                        //await _notificationRepository.InsertNotificationPersonal(notif);

                        return requestResult;

                    }

                }
                else if (request.Status == 2) // REQUEST REJECT
                {
                    var rejectStatus = await RejectRequest(request, connection, transaction);
                    return rejectStatus;
                }

                return 0;
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine(ex.Message);
                return 0;
            }
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

        // UPDATED CancelRequestRemoveTokenUpdate
        public async Task<int> RejectRequest(RequestDetails request, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                using (var command = new SqlCommand(@"
                    UPDATE address_tb 
                    SET remove_request_token = @token  
                    WHERE addr_id = @aid AND res_id = @rid", connection, transaction))
                {
                    command.Parameters.AddWithValue("@aid", request.Address_ID);
                    command.Parameters.AddWithValue("@rid", request.Resident_ID);
                    command.Parameters.AddWithValue("@token", DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }

                using (var updateRequestCommand = new SqlCommand(@"
                    UPDATE request_tb 
                    SET status = @status, comments = @comments, status_updated = @statusUpdated
                    WHERE request_id = @rid", connection, transaction))
                {
                    int rejectStatus = 2;
                    string comments = "Reject by the admin";
                    DateTime statusUpdated = DateTime.Now;

                    updateRequestCommand.Parameters.AddWithValue("@rid", request.Request_ID);
                    updateRequestCommand.Parameters.AddWithValue("@status", rejectStatus);
                    updateRequestCommand.Parameters.AddWithValue("@comments", comments);
                    updateRequestCommand.Parameters.AddWithValue("@statusUpdated", statusUpdated);

                    await updateRequestCommand.ExecuteNonQueryAsync();
                }

                //var residentIDS = await _residentAddressRepository.GetResidentID(request.Address_ID.ToString());

                //var notif = new Notification
                //{
                //    Address_ID = request.Address_ID.ToString(),
                //    Resident_ID = request.Resident_ID.ToString(),
                //    Title = "Request Action",
                //    Message = "Your requst was rejected.",
                //    Url = "/kvhai/resident/my-address",
                //    Message_Type = "Personal",
                //    ListResident_ID = residentIDS
                //};

                //await _notificationRepository.InsertNotificationPersonalToUser(notif);

                return 1;
            }
            catch (Exception)
            {
                // No rollback here because it will be handled in the main method
                return 0;
            }
        }
    }
}
