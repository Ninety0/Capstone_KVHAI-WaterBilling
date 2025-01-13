using KVHAI.Hubs;
using KVHAI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Data;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class NotificationRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHubContext<StaffNotificationHub> _staffHubContext;

        private readonly HubConnectionRepository _connectionRepository;
        private readonly ListRepository _listRepo;

        public NotificationRepository(DBConnect dBConnect, IHubContext<NotificationHub> hubContext, IHubContext<StaffNotificationHub> staffHubContext, HubConnectionRepository connectionRepository, ListRepository listRepo)
        {
            _dbConnect = dBConnect;
            _hubContext = hubContext;
            _staffHubContext = staffHubContext;
            _connectionRepository = connectionRepository;
            _listRepo = listRepo;
        }


        //MAYBE NEED TO MODIFIED
        public async Task<int> InsertNotificationPersonal(Notification notification)
        {
            int result = 0;
            var date = DateTime.Now;
            var url = string.IsNullOrEmpty(notification.Url) ? DBNull.Value.ToString() : notification.Url;
            var residentIDS = new List<int>();

            if (notification.ListResident_ID.Count < 1)
            {
                var residentList = await _listRepo.ResidentList();
                residentIDS = residentList.Select(r => Convert.ToInt32(r.Res_ID)).ToList();
                notification.ListResident_ID = residentIDS;
            }

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Insert into notification_tb and get the notification ID
                        int notificationId;
                        using (var command = new SqlCommand(@"
                    INSERT INTO notification_tb(uid, title, message, url, created_at, message_type) 
                    VALUES(@uid, @title, @message, @url, @date, @type);
                    SELECT SCOPE_IDENTITY();", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@uid", notification.Resident_ID);
                            command.Parameters.AddWithValue("@title", notification.Title);
                            command.Parameters.AddWithValue("@message", notification.Message);
                            command.Parameters.AddWithValue("@url", url);
                            command.Parameters.AddWithValue("@date", date);
                            command.Parameters.AddWithValue("@type", notification.Message_Type);

                            notificationId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        if (notificationId > 0)
                        {
                            result++;

                            // Step 2: Insert into resident_notification_status_tb for each resident
                            using (var statusCommand = new SqlCommand(@"
                        INSERT INTO resident_notification_status_tb(notif_id, res_id, is_read) 
                        VALUES(@nid, @rid, @read);", connection, transaction))
                            {
                                statusCommand.Parameters.Add("@nid", SqlDbType.Int).Value = notificationId;
                                statusCommand.Parameters.Add("@rid", SqlDbType.Int);
                                statusCommand.Parameters.Add("@read", SqlDbType.Bit).Value = 0;

                                foreach (var residentId in notification.ListResident_ID)
                                {
                                    statusCommand.Parameters["@rid"].Value = residentId;
                                    await statusCommand.ExecuteNonQueryAsync();
                                }
                            }

                            // Step 3: Send SignalR notifications
                            var notificationMapping = new Dictionary<string, string>
                    {
                        { "Announcement", "ReceivedNotification" },
                        { "Water Reading", "ReceivedReadingNotification" },
                        { "Water Billing", "ReceivedBillingNotification" },
                        { "Register Address", "ReceivedAddressNotification" },
                        { "My Address", "ReceivedMyAddressNotification" },
                        { "Request Action", "ReceivedRequestPageNotificationToMyAddress" }
                    };

                            foreach (var key in notificationMapping.Keys)
                            {
                                if (notification.Title.Contains(key))
                                {
                                    await _hubContext.Clients.All.SendAsync(notificationMapping[key], notification.Title, notification.Resident_ID);
                                    break;
                                }
                            }

                            // Step 4: Send personal notifications to connected clients
                            var hubConnections = await _connectionRepository.SelectHubConnection(notification.ListResident_ID);
                            if (hubConnections != null && hubConnections.Count > 0)
                            {
                                foreach (var hubConnection in hubConnections)
                                {
                                    await _hubContext.Clients.Client(hubConnection.Connection_ID)
                                        .SendAsync("ReceivedPersonalNotification", notification.Message, notification.Resident_ID);
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Transaction failed: {ex.Message}");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Operation failed: {ex.Message}");
                return 0;
            }

            return result;
        }


        private async Task SendNotifications(Notification notification, int residentID)
        {
            // Send group notifications based on title content
            if (notification.Title.Contains("Announcement"))
                await _hubContext.Clients.All.SendAsync("ReceivedNotification", notification.Title, notification.Resident_ID);
            else if (notification.Title.Contains("Water Reading"))
                await _hubContext.Clients.All.SendAsync("ReceivedReadingNotification", notification.Title, notification.Resident_ID);
            else if (notification.Title.Contains("Water Billing"))
                await _hubContext.Clients.All.SendAsync("ReceivedBillingNotification", notification.Title, notification.Resident_ID);
            else if (notification.Title.Contains("Register Address"))
                await _hubContext.Clients.All.SendAsync("ReceivedAddressNotification", notification.Title, notification.Resident_ID);
            else if (notification.Title.Contains("My Address"))
                await _hubContext.Clients.All.SendAsync("ReceivedMyAddressNotification", notification.Title, notification.Resident_ID);
            else if (notification.Title.Contains("Request Action"))
                await _hubContext.Clients.All.SendAsync("ReceivedRequestPageNotificationToMyAddress", notification.Title, notification.Resident_ID);

            // Notify personal connections
            var hubConnections = await _connectionRepository.SelectHubConnection(residentID.ToString());

            if (hubConnections != null && hubConnections.Count > 0)
            {
                foreach (var hubConnection in hubConnections)
                {
                    await _hubContext.Clients.Client(hubConnection.Connection_ID)
                        .SendAsync("ReceivedPersonalNotification", notification.Message, residentID);
                }
            }
        }

        private async Task NotifyClients(Notification notification, int residentID)
        {
            if (notification.Title.Contains("Announcement"))
            {
                await _hubContext.Clients.All.SendAsync("ReceivedNotification", notification.Title, residentID);
            }
            else if (notification.Title.Contains("Water Reading"))
            {
                await _hubContext.Clients.All.SendAsync("ReceivedReadingNotification", notification.Title, residentID);
            }
            else if (notification.Title.Contains("Water Billing"))
            {
                await _hubContext.Clients.All.SendAsync("ReceivedBillingNotification", notification.Title, residentID);
            }
            else if (notification.Title.Contains("Register Address"))
            {
                await _hubContext.Clients.All.SendAsync("ReceivedAddressNotification", notification.Title, residentID);
            }
            else if (notification.Title.Contains("My Address"))
            {
                await _hubContext.Clients.All.SendAsync("ReceivedMyAddressNotification", notification.Title, residentID);
            }
            else if (notification.Title.Contains("Request Action"))
            {
                await _hubContext.Clients.All.SendAsync("ReceivedRequestPageNotificationToMyAddress", notification.Title, residentID);
            }
        }

        public async Task<int> InsertNotificationPersonalToUser(Notification notification)
        {
            try
            {
                int totalInserted = 0; // Accumulates the count of successful insertions
                var createdAt = DateTime.Now; // Single date-time variable for consistency
                var url = string.IsNullOrEmpty(notification.Url) ? DBNull.Value.ToString() : notification.Url;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var command = new SqlCommand(@"
                        INSERT INTO notification_tb(uid, title, message, url, created_at, message_type) 
                        VALUES(@uid, @title, @message, @url, @created_at, @message_type);
                        SELECT SCOPE_IDENTITY();", connection, transaction))
                            using (var statusCommand = new SqlCommand(@"
                        INSERT INTO resident_notification_status_tb(notif_id, res_id, is_read) 
                        VALUES(@nid, @rid, @read)", connection, transaction))
                            {
                                // Add parameters for commands once
                                command.Parameters.Add("@uid", SqlDbType.Int);
                                command.Parameters.Add("@title", SqlDbType.NVarChar, 100);
                                command.Parameters.Add("@message", SqlDbType.NVarChar);
                                command.Parameters.Add("@url", SqlDbType.NVarChar);
                                command.Parameters.Add("@created_at", SqlDbType.DateTime);
                                command.Parameters.Add("@message_type", SqlDbType.NVarChar, 50);

                                statusCommand.Parameters.Add("@nid", SqlDbType.Int);
                                statusCommand.Parameters.Add("@rid", SqlDbType.Int);
                                statusCommand.Parameters.Add("@read", SqlDbType.Bit);

                                foreach (var residentID in notification.ListResident_ID)
                                {
                                    // Clear parameters before reusing
                                    command.Parameters.Clear();
                                    statusCommand.Parameters.Clear();

                                    // Insert into notification_tb
                                    command.Parameters.AddWithValue("@uid", residentID);
                                    command.Parameters.AddWithValue("@title", notification.Title);
                                    command.Parameters.AddWithValue("@message", notification.Message);
                                    command.Parameters.AddWithValue("@url", url);
                                    command.Parameters.AddWithValue("@created_at", createdAt);
                                    command.Parameters.AddWithValue("@message_type", notification.Message_Type);

                                    //var notificationID = await command.ExecuteScalarAsync(); // Returns the inserted ID
                                    // Safely convert to int
                                    var notificationId = Convert.ToInt32(await command.ExecuteScalarAsync());

                                    if (notificationId > 0)
                                    {
                                        totalInserted++;

                                        // Insert into resident_notification_status_tb
                                        statusCommand.Parameters.AddWithValue("@nid", notificationId);
                                        statusCommand.Parameters.AddWithValue("@rid", residentID);
                                        statusCommand.Parameters.AddWithValue("@read", 0); // Default unread

                                        await statusCommand.ExecuteNonQueryAsync();

                                        // Notify SignalR clients
                                        await NotifyClients(notification, residentID);

                                        var hubConnections = await _connectionRepository.SelectHubConnection(residentID.ToString());
                                        if (hubConnections?.Count > 0)
                                        {
                                            foreach (var hubConnection in hubConnections)
                                            {
                                                await _hubContext.Clients.Client(hubConnection.Connection_ID)
                                                    .SendAsync("ReceivedPersonalNotification", notification.Message, residentID);
                                            }
                                        }
                                    }
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            //_logger.LogError(ex, "Transaction failed while inserting notifications.");
                            return 0;
                        }
                    }
                }

                return totalInserted; // Return the total number of successful insertions
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "An error occurred while processing notifications.");
                return 0; // Return 0 in case of failure
            }
        }



        //NOTIFICATION IN ADMIN END
        public async Task<int> SendNotificationToAdmin(Notification notification)
        {
            try
            {
                int result = 0;
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var url = string.IsNullOrEmpty(notification.Url) ? DBNull.Value.ToString() : notification.Url;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO notification_emp_tb(uid,title,message,url,created_at,message_type) VALUES(@uid,@title,@message,@url,@date,@type)", connection))
                    {
                        command.Parameters.AddWithValue("@uid", notification.Resident_ID);
                        command.Parameters.AddWithValue("@title", notification.Title);
                        command.Parameters.AddWithValue("@message", notification.Message);
                        command.Parameters.AddWithValue("@url", url);
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@type", notification.Message_Type);

                        result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            if (notification.Title.Contains("Water Reading"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedReadingNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Water Billing"))
                            {
                                await _staffHubContext.Clients.All.SendAsync("ReceiveOnlinePayment");
                            }
                            else if (notification.Title.Contains("Register Address"))
                            {
                                await _staffHubContext.Clients.All.SendAsync("ReceivedAddressNotificationToAdmin", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Request Action"))
                            {
                                await _staffHubContext.Clients.All.SendAsync("ReceivedRequestPageNotificationToAdmin", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Register Account"))
                            {
                                await _staffHubContext.Clients.All.SendAsync("ReceivedNewRegisterAccount", notification.Title, notification.Resident_ID);
                            }

                            //TO REFRESH DATA IN DASHBOARD
                            await _staffHubContext.Clients.All.SendAsync("ReceivedDataDashboard");

                            var hubConnections = await _connectionRepository.SelectHubConnectionEmployee(notification.ListEmployee_ID);

                            if (hubConnections != null && hubConnections.Count > 0)
                            {

                                foreach (var hubConnection in hubConnections)
                                {
                                    await _staffHubContext.Clients.Client(hubConnection.Connection_ID)
                                           .SendAsync("ReceivedPersonalNotification", notification.Message, notification.Resident_ID);
                                }
                            }


                            if (hubConnections != null && hubConnections.Count > 0)
                            {
                                var empIdList = notification.ListEmployee_ID;

                                foreach (var hubConnection in hubConnections)
                                {
                                    foreach (var residentId in empIdList)
                                    {
                                        await _staffHubContext.Clients.Client(hubConnection.Connection_ID)
                                            .SendAsync("ReceivedPersonalNotification", notification.Message, empIdList);
                                    }
                                }
                            }
                        }

                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<Notification>> GetNotificationByResident(string residentID)
        {
            try
            {
                var notifList = new List<Notification>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                SELECT n.*, rns.is_read 
                FROM notification_tb n
                INNER JOIN resident_notification_status_tb rns 
                    ON n.notif_id = rns.notif_id
                WHERE rns.res_id = @uid AND rns.is_read = 0", connection))
                    {
                        command.Parameters.AddWithValue("@uid", residentID);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var timeDiff = await TimeDifference(reader.GetDateTime(reader.GetOrdinal("created_at")));

                                notifList.Add(new Notification
                                {
                                    Notification_ID = reader.GetInt32(reader.GetOrdinal("notif_id")),
                                    Resident_ID = residentID,
                                    Title = reader.GetString(reader.GetOrdinal("title")),
                                    Message = reader.GetString(reader.GetOrdinal("message")),
                                    Is_Read = reader.GetBoolean(reader.GetOrdinal("is_read")),
                                    Created_At = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    Message_Type = reader.GetString(reader.GetOrdinal("message_type")),
                                    Url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
                                    Hours = timeDiff
                                });
                            }
                        }
                    }
                }
                return notifList;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<int> UpdateReadNotification(string notification_id, string resident_id)
        {
            try
            {
                int result = 0;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                UPDATE resident_notification_status_tb 
                SET is_read = @read 
                WHERE notif_id = @notif_id AND res_id = @res_id", connection))
                    {
                        command.Parameters.AddWithValue("@notif_id", notification_id);
                        command.Parameters.AddWithValue("@res_id", resident_id);
                        command.Parameters.AddWithValue("@read", 1);

                        result = await command.ExecuteNonQueryAsync();
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }


        public async Task<int> UpdateReadNotificationEmployee(string notification_id)
        {
            try
            {
                int result = 0;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("UPDATE notification_emp_tb SET is_read = @read WHERE notif_id = @notif_id", connection))
                    {
                        command.Parameters.AddWithValue("@notif_id", notification_id);
                        command.Parameters.AddWithValue("@read", 1);

                        result = await command.ExecuteNonQueryAsync();


                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> SendNotificationAdminToAdmin(Notification notification)
        {
            try
            {
                int result = 0;
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var url = string.IsNullOrEmpty(notification.Url) ? DBNull.Value.ToString() : notification.Url;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO notification_emp_tb(uid,title,message,url,created_at,message_type) VALUES(@uid,@title,@message,@url,@date,@type)", connection))
                    {
                        command.Parameters.AddWithValue("@uid", notification.Resident_ID);
                        command.Parameters.AddWithValue("@title", notification.Title);
                        command.Parameters.AddWithValue("@message", notification.Message);
                        command.Parameters.AddWithValue("@url", url);
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@type", notification.Message_Type);

                        result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            if (notification.Title.Contains("Water Reading"))
                            {
                                await _staffHubContext.Clients.All.SendAsync("ReceivedWaterReading");
                            }
                            else if (notification.Title.Contains("Water Billing"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedWaterBilling");
                            }
                            // else if (notification.Title.Contains("Register Address"))
                            // {
                            //     await _hubContext.Clients.All.SendAsync("ReceivedAddressNotificationToAdmin", notification.Title, notification.Resident_ID);
                            // }
                            // else if (notification.Title.Contains("Request Action"))
                            // {
                            //     await _hubContext.Clients.All.SendAsync("ReceivedRequestPageNotificationToAdmin", notification.Title, notification.Resident_ID);
                            // }
                        }

                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<Notification>> GetNotificationByStaff(string role)
        {
            try
            {
                var notifications = await _listRepo.NotificationListEmployee();

                var notifList = new List<Notification>();

                var filteredNotifications = notifications
                    .Where(m => m.Message_Type.ToLower() == role && m.Is_Read == false)
                    .OrderByDescending(m => m.Created_At)
                    .ToList();

                foreach (var n in filteredNotifications)
                {
                    notifList.Add(new Notification
                    {
                        Notification_ID = n.Notification_ID,
                        Resident_ID = n.Resident_ID,
                        Title = n.Title,
                        Message = n.Message,
                        Is_Read = n.Is_Read,
                        Created_At = n.Created_At,
                        Message_Type = n.Message_Type,
                        Url = n.Url,
                        Hours = await TimeDifference(n.Created_At) // Asynchronous call handled correctly here
                    });
                }

                return notifList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<string> TimeDifference(DateTime dateCreated)
        {
            // Initialize the return variable
            var time = "";

            // Calculate the difference between the current time and the parsed date
            TimeSpan duration = DateTime.Now - dateCreated;

            // Calculate days, hours, and minutes
            int days = (int)duration.TotalDays;
            int hours = (int)(duration.TotalHours % 24);  // Hours after removing full days
            int minutes = (int)(duration.TotalMinutes % 60);  // Minutes after removing full hours

            // Build the string based on the time difference
            if (days > 0)
                time = $"{days}d";
            else if (hours > 0)
                time = $"{hours}h";
            else if (minutes > 0)
                time = $"{minutes}m";
            else
                time = "Just now";  // In case it's less than a minute

            return time;
        }


    }
}
/*
 
 ILAGAY ANG LOGIC SA LOGGED IN CONTROLLER PARA DI MAG ERROR YUNG NOTIFICATION
 
 */