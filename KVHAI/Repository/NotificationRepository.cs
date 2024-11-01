using KVHAI.Hubs;
using KVHAI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
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

        public NotificationRepository(DBConnect dBConnect, IHubContext<NotificationHub> hubContext,IHubContext<StaffNotificationHub> staffHubContext, HubConnectionRepository connectionRepository, ListRepository listRepo)
        {
            _dbConnect = dBConnect;
            _hubContext = hubContext;
            _staffHubContext = staffHubContext;
            _connectionRepository = connectionRepository;
            _listRepo = listRepo;
        }



        public async Task<int> InsertNotificationPersonal(Notification notification)
        {
            try
            {
                int result = 0;
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var url = string.IsNullOrEmpty(notification.Url) ? DBNull.Value.ToString() : notification.Url;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO notification_tb(res_id,title,message,url,created_at,message_type) VALUES(@res_id,@title,@message,@url,@date,@type)", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", notification.Resident_ID);
                        command.Parameters.AddWithValue("@title", notification.Title);
                        command.Parameters.AddWithValue("@message", notification.Message);
                        command.Parameters.AddWithValue("@url", url);
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@type", notification.Message_Type);

                        result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            if (notification.Title.Contains("Announcement"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Water Reading"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedReadingNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Water Billing"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedBillingNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Register Address"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedAddressNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("My Address"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedMyAddressNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Request Action"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedRequestPageNotificationToMyAddress", notification.Title, notification.Resident_ID);
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
                    using (var command = new SqlCommand("INSERT INTO notification_tb(res_id,title,message,url,created_at,message_type) VALUES(@res_id,@title,@message,@url,@date,@type)", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", notification.Resident_ID);
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
                                await _hubContext.Clients.All.SendAsync("ReceivedBillingNotification", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Register Address"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedAddressNotificationToAdmin", notification.Title, notification.Resident_ID);
                            }
                            else if (notification.Title.Contains("Request Action"))
                            {
                                await _hubContext.Clients.All.SendAsync("ReceivedRequestPageNotificationToAdmin", notification.Title, notification.Resident_ID);
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

        public async Task<int> SendNotificationPersonal(Notification notification)
        {
            try
            {
                int result = 0;
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var url = string.IsNullOrEmpty(notification.Url) ? DBNull.Value.ToString() : notification.Url;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO notification_tb(res_id,title,message,url,created_at,message_type) VALUES(@res_id,@title,@message,@url,@date,@type)", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", notification.Resident_ID);
                        command.Parameters.AddWithValue("@title", notification.Title);
                        command.Parameters.AddWithValue("@message", notification.Message);
                        command.Parameters.AddWithValue("@url", url);
                        command.Parameters.AddWithValue("@date", date);
                        command.Parameters.AddWithValue("@type", notification.Message_Type);

                        result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            //await _hubContext.Clients.All.SendAsync("ReceivedPersonalNotification", notification.Title, notification.Resident_ID);

                            var hubConnections = await _connectionRepository.SelectHubConnection(notification.Resident_ID);

                            if (hubConnections != null && hubConnections.Count > 0)
                            {
                                foreach (var hubConnection in hubConnections)
                                {
                                    int res_id = Convert.ToInt32(notification.Resident_ID);

                                    await _hubContext.Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedPersonalNotification", notification.Message, res_id);
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

        public async Task<List<Notification>> GetNotificationByResident(string resident_id)
        {
            try
            {
                var notifList = new List<Notification>();
                int result = 0;
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT * FROM notification_tb WHERE (res_id = @res_id OR message_type = 'all') AND is_read = 0", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", resident_id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var timeDiff = await TimeDifference(reader.GetDateTime(reader.GetOrdinal("created_at")));

                                notifList.Add(new Notification
                                {
                                    Notification_ID = reader.GetInt32(reader.GetOrdinal("notif_id")),
                                    Resident_ID = reader.GetString(reader.GetOrdinal("res_id")),
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

                        return notifList;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<int> UpdateReadNotification(string notification_id)
        {
            try
            {
                int result = 0;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("UPDATE notification_tb SET is_read = @read WHERE notif_id = @notif_id", connection))
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
                    using (var command = new SqlCommand("INSERT INTO notification_tb(res_id,title,message,url,created_at,message_type) VALUES(@res_id,@title,@message,@url,@date,@type)", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", notification.Resident_ID);
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
                var notifications = await _listRepo.NotificationList();

                var notifList = new List<Notification>();

                var filteredNotifications = notifications
                    .Where(m => m.Message_Type.ToLower() == role && m.Is_Read == false)
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