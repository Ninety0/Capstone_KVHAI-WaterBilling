using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class ListRepository
    {
        private readonly DBConnect _dBConnect;
        private readonly InputSanitize _sanitize;

        public ListRepository(DBConnect dBConnect, InputSanitize sanitize)
        {
            _dBConnect = dBConnect;
            _sanitize = sanitize;
        }

        public async Task<List<Payment>> PayList()
        {
            try
            {
                var payList = new List<Payment>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("select * from payment_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var payment = new Payment
                                {
                                    Payment_ID = reader.GetInt32(0),
                                    Address_ID = reader.GetInt32(1),
                                    Resident_ID = reader.GetInt32(2),
                                    Bill = reader.GetDecimal(3),
                                    Paid_Amount = reader.GetDecimal(4),
                                    Remaining_Balance = reader.GetDecimal(5),
                                    Payment_Method = reader.GetString(6),
                                    Payment_Status = reader.GetString(7),
                                    Payment_Date = reader.GetDateTime(8).ToString("yyyy-MM-dd HH:mm:ss"),
                                    Paid_By = reader.GetString(9),
                                    PayPal_TransactionId = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                    PayPal_PayerId = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                    PayPal_PayerEmail = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                                };

                                payList.Add(payment);
                            }
                        }
                    }

                }


                return payList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<AnnouncementImage>> ImageAnnouncementList()
        {
            try
            {
                var announcement = new List<AnnouncementImage>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"select * from announcement_img_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var announce = new AnnouncementImage
                                {
                                    Image_ID = reader.GetInt32(0),
                                    Announcement_ID = reader.GetInt32(1),
                                    Image_Url = reader.GetString(2),
                                };
                                announcement.Add(announce);
                            }
                        }
                    }
                }
                return announcement;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Notification>> NotificationList()
        {
            try
            {
                var notifications = new List<Notification>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"select * from notification_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {

                                var announce = new Notification
                                {
                                    Notification_ID = reader.GetInt32(reader.GetOrdinal("notif_id")),
                                    Resident_ID = reader.GetString(reader.GetOrdinal("uid")),
                                    Title = reader.GetString(reader.GetOrdinal("title")),
                                    Message = reader.GetString(reader.GetOrdinal("message")),
                                    Url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
                                    Created_At = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    Message_Type = reader.GetString(reader.GetOrdinal("message_type")),
                                    //Is_Read = reader.GetBoolean(reader.GetOrdinal("is_read")),
                                };
                                notifications.Add(announce);
                            }
                        }
                    }
                }
                return notifications;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Notification>> NotificationListEmployee()
        {
            try
            {
                var notifications = new List<Notification>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"select * from notification_emp_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {

                                var announce = new Notification
                                {
                                    Notification_ID = reader.GetInt32(reader.GetOrdinal("notif_id")),
                                    Resident_ID = reader.GetString(reader.GetOrdinal("uid")),
                                    Title = reader.GetString(reader.GetOrdinal("title")),
                                    Message = reader.GetString(reader.GetOrdinal("message")),
                                    Url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
                                    Created_At = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                    Message_Type = reader.GetString(reader.GetOrdinal("message_type")),
                                    Is_Read = reader.GetBoolean(reader.GetOrdinal("is_read")),
                                };
                                notifications.Add(announce);
                            }
                        }
                    }
                }
                return notifications;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ResidentAddress>> ResidentAddressList()
        {
            try
            {
                var address = new List<ResidentAddress>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"Select * From resident_address_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var residentAddress = new ResidentAddress
                                {
                                    Resident_Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Address_ID = reader.GetInt32(2),
                                    Is_Owner = reader.GetBoolean(3) ? 1 : 0,
                                    Status = reader.IsDBNull(4) ? string.Empty : reader.GetInt32(4).ToString(),
                                    Request_Date = reader.IsDBNull(5) ? string.Empty : reader.GetDateTime(5).ToString("yyyy-MM-dd HH:mm:ss"),
                                    Decision_Date = reader.IsDBNull(6) ? string.Empty : reader.GetDateTime(6).ToString("yyyy-MM-dd HH:mm:ss")
                                };

                                address.Add(residentAddress);
                            }
                        }
                    }
                }
                return address;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Announcement>> AnnouncementList()
        {
            try
            {
                var announcement = new List<Announcement>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"select * from announcement_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var announce = new Announcement
                                {
                                    Announcement_ID = reader.GetInt32(0),
                                    Employee_ID = reader.GetInt32(1),
                                    Title = reader.GetString(2),
                                    Contents = reader.GetString(3),
                                    Date_Created = reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm:"),

                                };
                                announcement.Add(announce);
                            }
                        }
                    }
                }
                return announcement;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Resident>> ResidentList()
        {
            try
            {
                var resident = new List<Resident>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("select * from resident_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var res = new Resident
                                {
                                    Res_ID = reader.GetInt32(0).ToString(),
                                    Lname = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                    Fname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                    Mname = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                    Email = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                    Username = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    Password = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                    Verified_At = reader.IsDBNull(8) ? string.Empty : reader.GetDateTime(8).ToString("yyyy-MM-dd HH:mm:ss"),
                                    Is_Activated = reader.GetBoolean(9),
                                    Account_Token = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                    Password_Reset_Token = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                    Reset_Token_Expire = reader.IsDBNull(12) ? string.Empty : reader.GetDateTime(12).ToString("yyyy-MM-dd HH:mm:ss"),
                                };

                                resident.Add(res);
                            }
                        }
                    }
                }

                return resident;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Renter>> RenterList()
        {
            try
            {
                var renterList = new List<Renter>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("select * from renter_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var renter = new Renter
                                {
                                    Renter_ID = reader.GetInt32(0).ToString(),
                                    Tentant_ID = reader.GetInt32(1).ToString(),
                                    Address_ID = reader.GetInt32(2).ToString(),
                                    Lname = reader.GetString(3),
                                    Fname = reader.GetString(4),
                                    Mname = reader.GetString(5),
                                    Phone = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    Email = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                    Username = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                    Password = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    Date_Created = reader.GetDateTime(10).ToString("yyyy-MM-dd hh:mm tt"),
                                };

                                renterList.Add(renter);
                            }
                        }
                    }
                }

                return renterList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Employee>> EmployeeList()
        {
            try
            {
                var empList = new List<Employee>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("select * from employee_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var emp = new Employee
                                {
                                    Emp_ID = reader.GetInt32(0).ToString(),
                                    Lname = reader.GetString(1),
                                    Fname = reader.GetString(2),
                                    Mname = reader.GetString(3),
                                    Phone = reader.GetString(4),
                                    Email = reader.GetString(5),
                                    Username = reader.GetString(6),
                                    Password = reader.GetString(7),
                                    Role = reader.GetString(8),
                                    Created_At = reader.GetDateTime(9).ToString("yyyy-MM-dd"),
                                };

                                empList.Add(emp);
                            }
                        }
                    }
                }

                return empList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Address>> AddressList()
        {
            try
            {
                var address = new List<Address>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"select * from address_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var _address = new Address
                                {
                                    Address_ID = reader.GetInt32(0),
                                    Resident_ID = reader.GetInt32(1),
                                    Block = reader.GetString(2),
                                    Lot = reader.GetString(3),
                                    Street_ID = reader.GetInt32(4),
                                    Location = reader.GetString(5),
                                    Account_Number = reader.GetString(6),
                                    Date_Residency = reader.GetDateTime(7).ToString(),
                                    Register_At = reader.GetDateTime(8),
                                    Remove_Request_Token = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    Remove_Token_Date = reader.IsDBNull(10) ? string.Empty : reader.GetDateTime(10).ToString("yyyy-MM-dd HH:mm:ss"),

                                };
                                address.Add(_address);
                            }
                        }
                    }
                }
                return address;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<Streets>> StreetList()
        {
            try
            {
                var streets = new List<Streets>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"select * from street_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var st = new Streets
                                {
                                    Street_ID = reader.GetInt32(0).ToString(),
                                    Street_Name = reader.GetString(1),
                                };
                                streets.Add(st);
                            }
                        }
                    }
                }
                return streets;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<List<WaterBilling>> WaterBillingList()
        {
            try
            {
                var waterBill = new List<WaterBilling>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"SELECT * FROM water_billing_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var _wb = new WaterBilling
                                {
                                    WaterBill_ID = reader.GetInt32(0).ToString(),
                                    Reference_No = reader.GetInt32(1).ToString(),
                                    Address_ID = reader.GetInt32(2).ToString(),
                                    Location = reader.GetInt32(3).ToString(),
                                    Previous_Reading = reader.GetInt32(4).ToString(),
                                    Current_Reading = reader.GetInt32(5).ToString(),
                                    Cubic_Meter = reader.GetString(6),
                                    Bill_For = reader.GetDateTime(7).ToString("yyyy-MM-dd"),
                                    Bill_Date_Created = reader.GetDateTime(8).ToString("yyyy-MM-dd"),
                                    Amount = reader.GetString(9),
                                    Date_Issue_From = reader.GetDateTime(10).ToString("yyyy-MM-dd"),
                                    Date_Issue_To = reader.GetDateTime(11).ToString("yyyy-MM-dd"),
                                    Due_Date_From = reader.GetDateTime(12).ToString("yyyy-MM-dd"),
                                    Due_Date_To = reader.GetDateTime(13).ToString("yyyy-MM-dd"),
                                    Status = reader.GetString(14),
                                    WaterBill_No = reader.GetInt32(15).ToString(),
                                };

                                waterBill.Add(_wb);
                            }
                        }
                    }
                }
                return waterBill;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<WaterReading>> WaterReadingList()
        {
            try
            {
                var waterRead = new List<WaterReading>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"SELECT * FROM water_reading_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var _wr = new WaterReading
                                {
                                    Reading_ID = reader.GetInt32(0).ToString(),
                                    Emp_ID = reader.GetInt32(1).ToString(),
                                    Address_ID = reader.GetInt32(2).ToString(),
                                    Consumption = reader.GetInt32(3).ToString(),
                                    Date = reader.GetDateTime(4).ToString("yyyy-MM-dd"),
                                };
                                waterRead.Add(_wr);
                            }
                        }
                    }
                }
                return waterRead;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ActualData>> ActualDataList()
        {
            try
            {
                var actualData = new List<ActualData>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"SELECT * FROM actual_data_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var ad = new ActualData
                                {
                                    CID = reader.GetInt32(0).ToString(),
                                    Address_ID = reader.GetInt32(1).ToString(),
                                    Actual_Data = reader.GetDouble(2).ToString(),
                                    Date = reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                                };
                                actualData.Add(ad);
                            }
                        }
                    }
                }
                return actualData;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ForecastData>> ForecastDataList()
        {
            try
            {
                var forecastData = new List<ForecastData>();
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"SELECT * FROM forecast_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fd = new ForecastData
                                {
                                    FID = reader.GetInt32(0).ToString(),
                                    Address_ID = reader.GetInt32(1).ToString(),
                                    Forecast_Data = reader.GetDouble(2).ToString(),
                                    Date = reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                                };
                                forecastData.Add(fd);
                            }
                        }
                    }
                }
                return forecastData;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
