using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class WaterBillRepository
    {
        private readonly DBConnect _dbConnect;
        //private readonly WaterBillingFunction _waterBillingFunction;
        private readonly AddressRepository _addressRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly ResidentAddressRepository _residentAddressRepository;

        public WaterBillRepository(DBConnect dBConnect, AddressRepository addressRepository, NotificationRepository notificationRepository, ResidentAddressRepository residentAddressRepository)//, WaterBillingFunction waterBillingFunction)
        {
            _dbConnect = dBConnect;
            _addressRepository = addressRepository;
            _notificationRepository = notificationRepository;
            _residentAddressRepository = residentAddressRepository;
            //_waterBillingFunction = waterBillingFunction;
        }

        public async Task<List<WaterBilling>> WaterBillingList()
        {
            try
            {
                var wbList = new List<WaterBilling>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("select * from water_billing_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var wb = new WaterBilling
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

                                wbList.Add(wb);
                            }
                        }
                    }
                }

                return wbList;
            }
            catch (Exception)
            {
                return null;
            }
        }

        ////////////
        // CREATE //
        ////////////
        public async Task<int> CreateWaterBill(WaterBilling waterBilling)
        {
            try
            {
                var dateBilling = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var dueDate = GetDueDate();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO water_billing_tb (reading_id, amount, date_issue_from, date_issue_to,due_date_from,due_date_to,status) VALUES(@read, @amount, @bill, @due,@status)", connection))
                    {
                        command.Parameters.AddWithValue("@read", waterBilling.Address_ID);
                        command.Parameters.AddWithValue("@amount", waterBilling.Amount);
                        command.Parameters.AddWithValue("@bill", dateBilling);
                        command.Parameters.AddWithValue("@due", dueDate);
                        command.Parameters.AddWithValue("@status", waterBilling.Status);

                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        //CREATE WATERBILLING
        public async Task<int> CreateWaterBill(List<WaterBilling> waterBilling)
        {
            var ref_no = await GetReferenceNumber();
            var dateBilling = DateTime.Now.ToString("yyyy-MM-dd");
            var dueDate = GetDueDate();
            var currentMonth = DateTime.Now.ToString("yyyy-MM");

            int resident_id = 0;
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        int waterBillNo = 1;

                        try
                        {
                            // Check if there is already a waterbill_no for the current month
                            var query = @"
                        SELECT TOP 1 waterbill_no 
                        FROM water_billing_tb 
                        WHERE date_issue_from LIKE @currentMonth 
                        ORDER BY waterbill_no DESC";

                            using (var command = new SqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@currentMonth", currentMonth + "%");
                                var result = await command.ExecuteScalarAsync();

                                // If a bill number exists for the current month, use it; otherwise, get the latest one and increment
                                if (result != null)
                                {
                                    waterBillNo = (int)result;
                                }
                                else
                                {
                                    // If no bill number exists for the current month, find the latest waterbill_no from the table
                                    var latestBillQuery = @"
                                SELECT TOP 1 waterbill_no 
                                FROM water_billing_tb 
                                ORDER BY waterbill_no DESC";

                                    using (var latestBillCommand = new SqlCommand(latestBillQuery, connection, transaction))
                                    {
                                        var latestBillResult = await latestBillCommand.ExecuteScalarAsync();
                                        if (latestBillResult != null)
                                        {
                                            waterBillNo = (int)latestBillResult + 1;
                                        }
                                        else
                                        {
                                            waterBillNo = 1;
                                        }
                                    }
                                }
                            }

                            // Loop through each WaterBilling object and insert into the database

                            foreach (var item in waterBilling)
                            {
                                using (var insertCommand = new SqlCommand(@"
                            INSERT INTO water_billing_tb (reference_no, addr_id, location ,prev_reading ,curr_reading, cubic_meter, bill_for ,bill_date_created , amount, date_issue_from, date_issue_to, due_date_from, due_date_to, status, waterbill_no)
                            VALUES (@ref, @addr, @loc, @prev, @current, @cubic, @bill_for, @bill_date, @amount, @issueFrom, @issueTo, @dueFrom, @dueTo, @status, @billno)", connection, transaction))
                                {
                                    insertCommand.Parameters.AddWithValue("@ref", ref_no);
                                    insertCommand.Parameters.AddWithValue("@addr", item.Address_ID);
                                    insertCommand.Parameters.AddWithValue("@loc", item.Location);
                                    insertCommand.Parameters.AddWithValue("@prev", item.Previous_Reading);
                                    insertCommand.Parameters.AddWithValue("@current", item.Current_Reading);
                                    insertCommand.Parameters.AddWithValue("@cubic", item.Cubic_Meter);
                                    insertCommand.Parameters.AddWithValue("@bill_for", item.Bill_For);
                                    insertCommand.Parameters.AddWithValue("@bill_date", dateBilling);
                                    insertCommand.Parameters.AddWithValue("@amount", item.Amount);
                                    insertCommand.Parameters.AddWithValue("@issueFrom", item.Date_Issue_From);
                                    insertCommand.Parameters.AddWithValue("@issueTo", item.Date_Issue_To);
                                    insertCommand.Parameters.AddWithValue("@dueFrom", item.Due_Date_From);
                                    insertCommand.Parameters.AddWithValue("@dueTo", item.Due_Date_To);
                                    insertCommand.Parameters.AddWithValue("@status", item.Status);
                                    insertCommand.Parameters.AddWithValue("@billno", waterBillNo);

                                    await insertCommand.ExecuteNonQueryAsync();

                                    //Will fetch resident id
                                    //resident_id = await _addressRepository.GetResidentIdByAddressId(item.Address_ID);
                                    var residentIDS = await _residentAddressRepository.GetResidentID(item.Address_ID);


                                    // Create notification for the resident
                                    var notif = new Notification
                                    {
                                        Address_ID = item.Address_ID,
                                        Resident_ID = resident_id.ToString(),
                                        Title = "Water Billing",
                                        Message = "You have a new bill",
                                        Url = "/kvhai/resident/billing",
                                        Message_Type = "Personal",
                                        ListResident_ID = residentIDS
                                    };

                                    await _notificationRepository.InsertNotificationPersonalToUser(notif);
                                }
                            }

                            // Commit the transaction if everything is successful
                            transaction.Commit();
                            return 1;
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction if any error occurs
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception rollbackEx)
                            {
                                Console.WriteLine("Rollback Exception: " + rollbackEx.Message);
                            }

                            Console.WriteLine("Transaction Exception: " + ex.Message);
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection Exception: " + ex.Message);
                return 0;
            }
        }

        //GET REFERENCE NUMBER AND INCREMENT
        private async Task<int> GetReferenceNumber()
        {
            try
            {
                var referenceNumber = 0;
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT TOP 1 reference_no FROM water_billing_tb ORDER BY reference_no DESC", connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            referenceNumber = (int)result + 1;
                        }
                        else
                        {
                            referenceNumber = 1;
                        }
                    }
                }
                return referenceNumber;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        ///////////
        // READ ///
        //////////

        public async Task<string> GetWaterBillNumber()
        {
            try
            {
                var _date = DateTime.Now.ToString("yyyy-MM");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                SELECT TOP 1 waterbill_no FROM address_tb a 
                JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                WHERE CONVERT(VARCHAR, wr.date_reading, 23) LIKE @month
            ", connection))
                    {
                        command.Parameters.AddWithValue("@month", "%" + _date + "%");
                        var result = await command.ExecuteScalarAsync();

                        // Check if result is null before converting to string
                        return result != null ? result.ToString() : string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception details for debugging
                Console.WriteLine($"Error in GetWaterBillNumber: {ex.Message}");
                return null;
            }
        }

        public async Task<ModelBinding> GetPreviousReading(string location, string date = "", string wbNumber = "")
        {
            var WBNumber = string.IsNullOrEmpty(wbNumber) ? await GetWaterBillNumber() : wbNumber;
            var prevDate = string.IsNullOrEmpty(date) ? DateTime.Now.AddMonths(-1).ToString("yyyy-MM") : date;
            var waterReading = new List<WaterReading>();
            var residentAddress = new List<ResidentAddress>();
            var models = new ModelBinding();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    //WE ADD CONVERT(VARCHAR, wr.date_reading, 23 ) to able to search the month
                    using (var command = new SqlCommand(@"
                select * from resident_tb r 
                JOIN address_tb a ON r.res_id = a.res_id
                JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND a.location LIKE @location AND CAST(wb.waterbill_no AS varchar) LIKE @num
                ORDER BY CAST(block as INT);
                ", connection))
                    {
                        command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                        command.Parameters.AddWithValue("@location", "%" + location + "%");
                        command.Parameters.AddWithValue("@num", "%" + WBNumber + "%");

                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                var wr = new WaterReading
                                {
                                    Reading_ID = reader["reading_id"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Consumption = reader["consumption"].ToString() ?? string.Empty,

                                    Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
                                };

                                var address = new ResidentAddress
                                {
                                    Name = string.Concat(reader["lname"].ToString(), ", ", reader["fname"].ToString()) ?? string.Empty,
                                    Block = reader["block"].ToString() ?? string.Empty,
                                    Lot = reader["lot"].ToString() ?? string.Empty,
                                };

                                waterReading.Add(wr);
                                residentAddress.Add(address);

                            }
                        }
                    }
                }
                models.PreviousReading = waterReading;
                models.ResidentAddress = residentAddress;
            }
            catch (Exception)
            {
                return null;
            }

            return models;
        }

        public async Task<ModelBinding> GetCurrentReading(string location, string date = "", string wbNumber = "")
        {
            var prevDate = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("yyyy-MM") : date;
            var WBNumber = string.IsNullOrEmpty(wbNumber) ? await GetWaterBillNumber() : wbNumber;
            var waterReading = new List<WaterReading>();
            var waterBilling = new List<WaterBilling>();
            var models = new ModelBinding();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                    select * from resident_tb r 
                    JOIN address_tb a ON r.res_id = a.res_id
                    JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                    JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                    WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND a.location LIKE @location AND CAST(wb.waterbill_no AS varchar) LIKE @num
                    ORDER BY CAST(block as INT);
                ", connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    command.Parameters.AddWithValue("@location", "%" + location + "%");
                    command.Parameters.AddWithValue("@num", "%" + wbNumber + "%");

                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var id = reader["addr_id"].ToString() ?? string.Empty;
                            var wr = new WaterReading
                            {
                                Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                Consumption = reader["consumption"].ToString() ?? string.Empty,
                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty,
                                WaterBill_Number = reader["waterbill_no"].ToString() ?? string.Empty
                            };

                            var wb = new WaterBilling
                            {
                                WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                Cubic_Meter = reader["cubic_meter"].ToString() ?? string.Empty,
                                Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty

                            };

                            waterReading.Add(wr);
                            waterBilling.Add(wb);

                        }
                    }
                }
            }
            models.CurrentReading = waterReading;
            models.WBilling = waterBilling;

            return models;
        }

        public async Task<ModelBinding> GetWaterBilling(string location, string wbNumber)
        {
            try
            {
                var WBNumber = string.IsNullOrEmpty(wbNumber) ? await GetWaterBillNumber() : wbNumber;
                var address = new List<Address>();
                var waterBilling = new List<WaterBilling>();
                var models = new ModelBinding();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        select r.lname,r.fname,r.mname,s.st_name, a.*,wb.*
                        from water_billing_tb wb
                        JOIN address_tb a ON wb.addr_id = a.addr_id
                        JOIN street_tb s ON a.st_id = s.st_id
                        JOIN resident_tb r ON a.res_id = r.res_id
                        WHERE waterbill_no = @num AND wb.location = @location", connection))//, r.account_number  
                    {
                        command.Parameters.AddWithValue("@num", WBNumber ?? "1");
                        command.Parameters.AddWithValue("@location", location);

                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                var lname = reader.GetString(0);
                                var fname = reader.GetString(1);
                                var mname = reader.GetString(2);
                                var _address = new Address
                                {
                                    Address_ID = reader.GetInt32(4),
                                    Resident_ID = reader.GetInt32(5),
                                    Block = reader.GetString(6),
                                    Lot = reader.GetString(7),
                                    Street_Name = reader.GetString(3),
                                    Resident_Name = string.Join(", ", lname, fname, mname),
                                };
                                var wb = new WaterBilling
                                {
                                    WaterBill_ID = reader.GetInt32(13).ToString(),
                                    Reference_No = reader.GetInt32(14).ToString(),
                                    Address_ID = reader.GetInt32(15).ToString(),
                                    Location = reader.GetInt32(16).ToString(),
                                    Previous_Reading = reader.GetInt32(17).ToString(),
                                    Current_Reading = reader.GetInt32(18).ToString(),
                                    Cubic_Meter = reader.GetString(19),
                                    Bill_For = reader.GetDateTime(20).ToString("MMMM yyyy"),
                                    Bill_Date_Created = reader.GetDateTime(21).ToString("MMMM yyyy"),
                                    Amount = reader.GetString(22),
                                    Date_Issue_From = reader.GetDateTime(23).ToString("MMMM yyyy"),
                                    Date_Issue_To = reader.GetDateTime(24).ToString("MMMM yyyy"),
                                    Due_Date_From = reader.GetDateTime(25).ToString("MMMM yyyy"),
                                    Due_Date_To = reader.GetDateTime(26).ToString("MMMM yyyy"),
                                    Status = reader.GetString(27),
                                    WaterBill_No = reader.GetInt32(28).ToString(),
                                    //Account_Number = reader.GetString(29)


                                };

                                address.Add(_address);
                                waterBilling.Add(wb);

                            }
                        }
                    }
                }
                models.ListAddress = address;
                models.WBilling = waterBilling;

                return models;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<List<WaterBilling>> GetUnpaidWaterBills(List<WaterBilling> waterReading)
        {
            try
            {
                var waterBillIArrears = new List<WaterBilling>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    if (waterReading.Count > 0)
                    {
                        foreach (var item in waterReading)
                        {
                            var id = item.Address_ID;
                            var billNo = item.WaterBill_No;

                            using (var command = new SqlCommand(@"
                                SELECT * FROM water_billing_tb 
                                WHERE addr_id = @id 
                                    AND status = 'unpaid'
                                    AND waterbill_no != @billnum
                            ", connection))//

                            {
                                command.Parameters.AddWithValue("@id", id);
                                command.Parameters.AddWithValue("@billnum", billNo);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    double totalAmount = 0.0;
                                    var WBNumbers = new List<string>();
                                    while (await reader.ReadAsync())
                                    {
                                        var waterBillNumber = reader["waterbill_no"].ToString();

                                        var waterBillNumberFormatted = "WB" + waterBillNumber;
                                        var amount = Convert.ToDouble(reader["amount"]);


                                        // Check if this water bill number has already been processed to avoid duplicate entries.
                                        var billNumberExist = WBNumbers.Any(num => num == waterBillNumberFormatted);

                                        if (billNumberExist)//it means it already exist
                                        {
                                            // Skip this iteration since the water bill number already exists in the list.
                                            continue;
                                        }
                                        else //if does not exist
                                        {
                                            totalAmount += amount;

                                            WBNumbers.Add(waterBillNumberFormatted);
                                        }
                                    }
                                    if (WBNumbers.Count > 0)
                                    {
                                        waterBillIArrears.Add(new WaterBilling()
                                        {
                                            PreviousWaterBill = string.Join("-", WBNumbers),
                                            PreviousWaterBillAmount = totalAmount.ToString("F2") // Format as string
                                        });
                                    }
                                }
                            }
                        }
                    }

                }
                return waterBillIArrears;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        ////////////////////////
        // FUNCTIONS OVERLOAD //
        ///////////////////////
        public async Task<ModelBinding> GetPreviousReadingReport(ReportWaterBilling reportWaterBilling)
        {
            //var prevDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM");
            var waterReading = new List<WaterReading>();
            var residentAddress = new List<ResidentAddress>();
            var models = new ModelBinding();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                if (reportWaterBilling != null)
                {
                    var waterBillID = reportWaterBilling.WaterBill_ID;
                    var waterBillNo = reportWaterBilling.WaterBill_Number;
                    var date = "";
                    if (DateTime.TryParse(reportWaterBilling.DateBillMonth, out DateTime result))
                    {
                        date = result.ToString("yyyy-MM");
                    }

                    using (var command = new SqlCommand(@"
                        select * from resident_tb r 
                        JOIN address_tb a ON r.res_id = a.res_id
                        JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                        JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                        WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND waterbill_id = @id AND wb.waterbill_no = @num
                        ORDER BY CAST(waterbill_id as INT)
                        ", connection))
                    {
                        command.Parameters.AddWithValue("@date", "%" + date + "%");
                        command.Parameters.AddWithValue("@id", waterBillID);
                        command.Parameters.AddWithValue("@num", waterBillNo);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            if (await reader.ReadAsync())
                            {
                                var wr = new WaterReading
                                {
                                    Reading_ID = reader["reading_id"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Consumption = reader["consumption"].ToString() ?? string.Empty,

                                    Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
                                };

                                var address = new ResidentAddress
                                {
                                    Name = string.Concat(reader["lname"].ToString(), ", ", reader["fname"].ToString()) ?? string.Empty,
                                    Block = reader["block"].ToString() ?? string.Empty,
                                    Lot = reader["lot"].ToString() ?? string.Empty,
                                };

                                waterReading.Add(wr);
                                residentAddress.Add(address);

                            }
                        }
                    }
                }

            }
            models.PreviousReading = waterReading;
            models.ResidentAddress = residentAddress;

            return models;
        }

        public async Task<ModelBinding> GetCurrentReadingReport(ReportWaterBilling reportWaterBilling)
        {
            var prevDate = DateTime.Now.ToString("yyyy-MM");
            var waterReading = new List<WaterReading>();
            var waterBilling = new List<WaterBilling>();
            var models = new ModelBinding();

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    if (reportWaterBilling != null)
                    {
                        var waterBillID = reportWaterBilling.WaterBill_ID;
                        var waterBillNo = reportWaterBilling.WaterBill_Number;
                        var date = "";
                        if (DateTime.TryParse(reportWaterBilling.DateBillMonth, out DateTime result))
                        {
                            date = result.AddMonths(1).ToString("yyyy-MM");
                        }

                        using (var command = new SqlCommand(@"
                        select * from resident_tb r 
                        JOIN address_tb a ON r.res_id = a.res_id
                        JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                        JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                        WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND waterbill_id = @id AND wb.waterbill_no = @num
                        ORDER BY CAST(waterbill_id as INT)
                        ", connection))
                        {
                            command.Parameters.AddWithValue("@date", "%" + date + "%");
                            command.Parameters.AddWithValue("@id", waterBillID);
                            command.Parameters.AddWithValue("@num", waterBillNo);
                            using (var reader = await command.ExecuteReaderAsync())
                            {

                                if (await reader.ReadAsync())
                                {
                                    var wr = new WaterReading
                                    {
                                        Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                        Consumption = reader["consumption"].ToString() ?? string.Empty,
                                        Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty,
                                        WaterBill_Number = reader["waterbill_no"].ToString() ?? string.Empty

                                    };

                                    var wb = new WaterBilling
                                    {
                                        WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                        Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                        Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                        Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                        Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                        Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                        WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty
                                    };

                                    waterReading.Add(wr);
                                    waterBilling.Add(wb);

                                }
                            }
                        }
                    }

                }
                models.CurrentReading = waterReading;
                models.WBilling = waterBilling;
            }
            catch (Exception ex)
            {
                throw;
            }

            return models;
        }

        public async Task<ModelBinding> GetWaterBilling(ReportWaterBilling reportWaterBilling)
        {
            try
            {
                var address = new List<Address>();
                var waterBilling = new List<WaterBilling>();
                var models = new ModelBinding();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    if (reportWaterBilling != null)
                    {
                        var waterBillID = reportWaterBilling.WaterBill_ID;
                        var waterBillNo = reportWaterBilling.WaterBill_Number;

                        using (var command = new SqlCommand(@"
                    select r.lname,r.fname,r.mname,s.st_name, a.*,wb.*,r.account_number  
                    from water_billing_tb wb
                    JOIN address_tb a ON wb.addr_id = a.addr_id
                    JOIN street_tb s ON a.st_id = s.st_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    WHERE waterbill_id = @id AND waterbill_no = @num", connection))
                        {
                            command.Parameters.AddWithValue("@id", waterBillID);
                            command.Parameters.AddWithValue("@num", waterBillNo);

                            using (var reader = await command.ExecuteReaderAsync())
                            {

                                while (await reader.ReadAsync())
                                {
                                    DateTime date = reader.GetDateTime(21);

                                    var lname = reader.GetString(0);
                                    var fname = reader.GetString(1);
                                    var mname = reader.GetString(2);

                                    var dayFrom = DateTime.Now.ToString("dd");
                                    var dayTo = DateTime.Now.AddDays(-1).ToString("dd");
                                    var periodFrom = reader.GetDateTime(21).ToString($"MMM {dayFrom}, yyyy"); // july 01 2024
                                    var periodTo = date.AddMonths(1).ToString($"MMM {dayTo}, yyyy");

                                    var periodCoverDate = string.Join(" - ", periodFrom, periodTo);

                                    var _address = new Address
                                    {
                                        Address_ID = reader.GetInt32(4),
                                        Resident_ID = reader.GetInt32(5),
                                        Block = reader.GetString(6),
                                        Lot = reader.GetString(7),
                                        Street_Name = reader.GetString(3),
                                        Resident_Name = string.Join(", ", lname, fname, mname)
                                    };
                                    var wb = new WaterBilling
                                    {
                                        WaterBill_ID = reader.GetInt32(13).ToString(),
                                        Reference_No = reader.GetInt32(14).ToString(),
                                        Address_ID = reader.GetInt32(15).ToString(),
                                        Location = reader.GetInt32(16).ToString(),
                                        Previous_Reading = reader.GetInt32(17).ToString(),
                                        Current_Reading = reader.GetInt32(18).ToString(),
                                        Cubic_Meter = reader.GetString(19),
                                        Bill_For = reader.GetDateTime(20).ToString("MMMM yyyy"),
                                        Bill_Date_Created = reader.GetDateTime(21).ToString("MMMM yyyy"),
                                        Amount = reader.GetString(22),
                                        Date_Issue_From = reader.GetDateTime(23).ToString("MMMM yyyy"),
                                        Date_Issue_To = reader.GetDateTime(24).ToString("MMMM yyyy"),
                                        Due_Date_From = reader.GetDateTime(25).ToString("MMMM yyyy"),
                                        Due_Date_To = reader.GetDateTime(26).ToString("MMMM yyyy"),
                                        Status = reader.GetString(27),
                                        WaterBill_No = reader.GetInt32(28).ToString(),
                                        Account_Number = reader.GetString(29),

                                        DatePeriodCovered = periodCoverDate

                                    };

                                    address.Add(_address);
                                    waterBilling.Add(wb);

                                }
                            }
                        }
                    }

                }
                models.ListAddress = address;
                models.WBilling = waterBilling;

                return models;
            }
            catch (Exception)
            {

                return null;
            }
        }


        ///////////////////////
        // CUSTOM FUNCTIONS //
        /////////////////////

        /// <summary>
        /// Retrieves unpaid water bills for a list of addresses, excluding the current water bill number.
        /// </summary>
        /// <param name="waterReading">List of water readings containing address IDs and bill numbers.</param>
        /// <returns>List of unpaid water bills with accumulated amounts.</returns>
        public async Task<List<WaterBilling>> UnpaidWaterBill(List<WaterReading> waterReading)
        {
            try
            {
                var waterBillIArrears = new List<WaterBilling>();
                var dateFrom = "";
                var dateTo = "";
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    if (waterReading.Count > 0)
                    {
                        foreach (var item in waterReading)
                        {
                            var id = item.Address_ID;
                            var billNo = item.WaterBill_Number;



                            using (var command = new SqlCommand(@"
                                SELECT * 
                                FROM water_billing_tb w
                                JOIN address_tb a ON w.addr_id = a.addr_id
                                JOIN water_reading_tb r ON a.addr_id = r.addr_id
                                WHERE a.addr_id = @id 
                                    AND status = 'unpaid'
                                    AND waterbill_no != @billnum
                            ", connection))//

                            {
                                command.Parameters.AddWithValue("@id", id);
                                command.Parameters.AddWithValue("@billnum", billNo);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    double totalAmount = 0.0;
                                    var WBNumbers = new List<string>();
                                    while (await reader.ReadAsync())
                                    {
                                        var waterBillNumber = reader["waterbill_no"].ToString();

                                        var waterBillNumberFormatted = "WB" + waterBillNumber;
                                        var amount = Convert.ToDouble(reader["amount"]);


                                        // Check if this water bill number has already been processed to avoid duplicate entries.
                                        var billNumberExist = WBNumbers.Any(num => num == waterBillNumberFormatted);

                                        if (billNumberExist)//it means it already exist
                                        {
                                            // Skip this iteration since the water bill number already exists in the list.
                                            continue;
                                        }
                                        else //if does not exist
                                        {
                                            totalAmount += amount;

                                            WBNumbers.Add(waterBillNumberFormatted);
                                        }
                                    }
                                    if (WBNumbers.Count > 0)
                                    {
                                        waterBillIArrears.Add(new WaterBilling()
                                        {
                                            PreviousWaterBill = string.Join("-", WBNumbers),
                                            PreviousWaterBillAmount = totalAmount.ToString("F2") // Format as string
                                        });
                                    }
                                }
                            }
                        }
                    }

                }
                return waterBillIArrears;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<WaterBilling> GetDueDate()
        {
            string dueDateFrom = DateTime.Now.AddMonths(1).ToString("yyyy-MM-");
            string dueDateTo = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");

            var wb = new WaterBilling()
            {
                Due_Date_From = dueDateFrom + "1",
                Due_Date_To = dueDateTo
            };
            return wb;
        }

        //DATE ISSUE OF WATER BILLING
        public async Task<WaterBilling> GetDateBilling()
        {
            var dateFrom = DateTime.Now.ToString("yyyy-MM-dd");
            var dateTo = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

            var wb = new WaterBilling()
            {
                Date_Issue_From = dateFrom,
                Date_Issue_To = dateTo
            };
            return wb;
        }

        public async Task<bool> CheckExistingWaterBilling(List<WaterBilling> waterBilling)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    foreach (var item in waterBilling)
                    {
                        using (var command = new SqlCommand(@"
                            SELECT COUNT(*) FROM water_billing_tb 
                            WHERE bill_for = @bill AND location = @loc", connection))//AND status= @status
                        {
                            command.Parameters.AddWithValue("@bill", item.Bill_For);
                            command.Parameters.AddWithValue("@loc", item.Location);
                            //command.Parameters.AddWithValue("@status", item.Status);

                            var count = (int)await command.ExecuteScalarAsync();

                            if (count > 0)
                            {
                                return true; // Record exists
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

        public async Task<List<string>> WaterBillNumberList()
        {
            var billNumberList = new List<string>();
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT DISTINCT CAST(waterbill_no as INT)as bill_no FROM water_billing_tb ORDER BY bill_no DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                billNumberList.Add(reader["bill_no"].ToString() ?? string.Empty);
                            }
                        }
                    }
                }
                return billNumberList;
            }
            catch (Exception ex)
            {
                billNumberList.Add(ex.Message);
                return billNumberList;
            }
        }

        private (string dateFrom, string dateTo) CalculateDateRange(DateTime output)
        {
            var lastDate = DateTime.DaysInMonth(DateTime.Now.Year, Convert.ToInt32(output.ToString("MM"))).ToString();
            string dateFrom = output.AddMonths(-2).ToString("yyyy-MM-01");
            string dateTo = output.AddMonths(-1).ToString($"yyyy-MM-{lastDate}");
            return (dateFrom, dateTo);
        }

        public async Task<List<WaterBillWithAddress>> UnpaidResidentWaterBilling(string residentID, string addressID)
        {
            try
            {
                var model = new ModelBinding();
                var wbList = new List<WaterBilling>();
                var addrList = new List<Address>();
                var wbaList = new List<WaterBillWithAddress>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    select * from water_billing_tb wb
                    JOIN address_tb a ON wb.addr_id = a.addr_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    JOIN street_tb s ON a.st_id = s.st_id
                    WHERE  status = 'unpaid' AND r.res_id  = @res_id AND a.addr_id = @addr_id", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", residentID);
                        command.Parameters.AddWithValue("@addr_id", addressID);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            double totalAmount = 0.00;
                            while (await reader.ReadAsync())
                            {
                                totalAmount += Convert.ToDouble(reader["amount"].ToString());
                                var wba = new WaterBillWithAddress
                                {
                                    WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                    Amount = reader["amount"].ToString() ?? string.Empty,
                                    Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                    Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                    Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                    Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                    Status = reader["status"].ToString() ?? string.Empty,
                                    WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Block = reader["block"].ToString() ?? string.Empty,
                                    Lot = reader["lot"].ToString() ?? string.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString() ?? string.Empty),
                                    Street_Name = reader["st_name"].ToString() ?? string.Empty,
                                    Account_Number = reader["account_number"].ToString() ?? string.Empty,

                                    TotalAmount = totalAmount.ToString("F2")
                                };

                                wbaList.Add(wba);

                                //wbList.Add(
                                //    new WaterBilling
                                //    {
                                //        WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                //        Amount = reader["amount"].ToString() ?? string.Empty,
                                //        Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                //        Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                //        Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                //        Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                //        Status = reader["status"].ToString() ?? string.Empty,
                                //        WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty
                                //    }
                                //);

                                //addrList.Add(
                                //    new Address
                                //    {
                                //        Address_ID = Convert.ToInt32(reader["addr_id"].ToString() ?? string.Empty),
                                //        Block = reader["block"].ToString() ?? string.Empty,
                                //        Lot = reader["lot"].ToString() ?? string.Empty,
                                //        Street_ID = Convert.ToInt32(reader["st_id"].ToString() ?? string.Empty),
                                //        Street_Name = reader["st_name"].ToString() ?? string.Empty
                                //    }
                                //);
                            }
                        }
                    }
                }

                return wbaList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<WaterBillWithAddress>> GetPaidResidentWaterBilling(string residentID, string addressID)
        {
            try
            {
                var model = new ModelBinding();
                var wbList = new List<WaterBilling>();
                var addrList = new List<Address>();
                var wbaList = new List<WaterBillWithAddress>();
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    select * from water_billing_tb wb
                    JOIN address_tb a ON wb.addr_id = a.addr_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    JOIN street_tb s ON a.st_id = s.st_id
                    WHERE  status = 'paid' AND r.res_id  = @res_id AND a.addr_id = @addr_id", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", residentID);
                        command.Parameters.AddWithValue("@addr_id", addressID);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            double totalAmount = 0.00;
                            while (await reader.ReadAsync())
                            {
                                totalAmount += Convert.ToDouble(reader["amount"].ToString());
                                var wba = new WaterBillWithAddress
                                {
                                    WaterBill_ID = reader["waterbill_id"].ToString() ?? string.Empty,
                                    Amount = reader["amount"].ToString() ?? string.Empty,
                                    Date_Issue_From = reader["date_issue_from"].ToString() ?? string.Empty,
                                    Date_Issue_To = reader["date_issue_to"].ToString() ?? string.Empty,
                                    Due_Date_From = reader["due_date_from"].ToString() ?? string.Empty,
                                    Due_Date_To = reader["due_date_to"].ToString() ?? string.Empty,
                                    Status = reader["status"].ToString() ?? string.Empty,
                                    WaterBill_No = reader["waterbill_no"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Block = reader["block"].ToString() ?? string.Empty,
                                    Lot = reader["lot"].ToString() ?? string.Empty,
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString() ?? string.Empty),
                                    Street_Name = reader["st_name"].ToString() ?? string.Empty,
                                    Account_Number = reader["account_number"].ToString() ?? string.Empty,

                                    TotalAmount = totalAmount.ToString("F2")
                                };

                                wbaList.Add(wba);

                            }
                        }
                    }
                }

                return wbaList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}

//MALE LOGIC KO PAANO KAPAG MULTIPLE DATES NA LAGI LANG MAKUKUHA YUNG UNANG ENTRY HOW ABOUT THE OTHER MONTS 
//LOOK GPT TOMMOROW.
