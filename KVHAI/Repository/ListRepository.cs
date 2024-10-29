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

        public async Task<List<Payment>> PayList(SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var payList = new List<Payment>();
                using (var command = new SqlCommand("select * from payment_tb", connection, transaction))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var payment = new Payment
                            {
                                Payment_ID = reader.GetInt32(0),
                                Emp_ID = reader.GetInt32(1),
                                Address_ID = reader.GetInt32(2),
                                Resident_ID = reader.GetInt32(3),
                                Bill = reader.GetDecimal(4),
                                Paid_Amount = reader.GetDecimal(5),
                                Remaining_Balance = reader.GetDecimal(6),
                                Payment_Method = reader.GetString(7),
                                Payment_Status = reader.GetString(8),
                                Payment_Date = reader.GetDateTime(9).ToString("yyyy-MM-dd HH:mm:ss"),
                                Paid_By = reader.GetString(10),
                            };

                            payList.Add(payment);
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
                                    Lname = reader.GetString(1),
                                    Fname = reader.GetString(2),
                                    Mname = reader.GetString(3),
                                    Phone = reader.GetString(4),
                                    Email = reader.GetString(5),
                                    Username = reader.GetString(6),
                                    Password = reader.GetString(7),
                                    Occupancy = reader.GetString(8),
                                    Verification_Token = reader.GetString(9),
                                    Verified_At = reader.IsDBNull(10) ? string.Empty : reader.GetDateTime(10).ToString("yyyy-MM-dd HH:mm:ss"),
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
                                    Is_Verified = reader.GetString(6),
                                    Register_At = reader.GetDateTime(7),
                                    Remove_Request_Token = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                    Remove_Token_Date = reader.IsDBNull(9) ? string.Empty : reader.GetDateTime(9).ToString("yyyy-MM-dd HH:mm:ss"),

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
                                    Street_Name = reader.GetString(0),
                                };
                                streets.Add(st);
                            }
                        }
                    }
                }
                return streets;
            }
            catch (Exception)
            {
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
                                    Previous_Reading = reader.GetInt32(3).ToString(),
                                    Current_Reading = reader.GetInt32(4).ToString(),
                                    Cubic_Meter = reader.GetString(5),
                                    Bill_For = reader.GetDateTime(6).ToString("yyyy-MM-dd"),
                                    Bill_Date_Created = reader.GetDateTime(7).ToString("yyyy-MM-dd"),
                                    Amount = reader.GetString(8),
                                    Date_Issue_From = reader.GetDateTime(9).ToString("yyyy-MM-dd"),
                                    Date_Issue_To = reader.GetDateTime(10).ToString("yyyy-MM-dd"),
                                    Due_Date_From = reader.GetDateTime(11).ToString("yyyy-MM-dd"),
                                    Due_Date_To = reader.GetDateTime(12).ToString("yyyy-MM-dd"),
                                    Status = reader.GetString(13),
                                    WaterBill_No = reader.GetInt32(14).ToString(),
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
    }
}
