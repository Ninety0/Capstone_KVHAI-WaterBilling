using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class WaterBillRepository
    {
        private readonly DBConnect _dbConnect;
        //private readonly WaterBillingFunction _waterBillingFunction;

        public WaterBillRepository(DBConnect dBConnect)//, WaterBillingFunction waterBillingFunction)
        {
            _dbConnect = dBConnect;
            //_waterBillingFunction = waterBillingFunction;
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
            try
            {
                var dateBilling = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var dueDate = GetDueDate();
                var dateNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var currentMonth = DateTime.Now.ToString("yyyy-MM");

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int waterBillNo = 1;

                            // Check if there is already a waterbill_no for the current month
                            using (var command = new SqlCommand(@"
                                SELECT TOP 1 waterbill_no FROM water_billing_tb WHERE date_issue_from LIKE @currentMonth", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@currentMonth", "%" + currentMonth + "%");
                                var result = await command.ExecuteScalarAsync();

                                if (result != null)
                                {
                                    waterBillNo = (int)result;
                                }
                            }

                            foreach (var item in waterBilling)
                            {
                                using (var command = new SqlCommand("INSERT INTO water_billing_tb (addr_id, amount, date_issue_from, date_issue_to,due_date_from,due_date_to,status,waterbill_no) VALUES(@addr, @amount,@issueFrom,@issueTo,@dueFrom,@dueTo,@status,@billno)", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@addr", item.Address_ID);
                                    command.Parameters.AddWithValue("@amount", item.Amount);
                                    command.Parameters.AddWithValue("@issueFrom", item.Date_Issue_From);
                                    command.Parameters.AddWithValue("@issueTo", item.Date_Issue_To);
                                    command.Parameters.AddWithValue("@dueFrom", item.Due_Date_From);
                                    command.Parameters.AddWithValue("@dueTo", item.Due_Date_To);
                                    command.Parameters.AddWithValue("@status", item.Status);
                                    command.Parameters.AddWithValue("@billno", waterBillNo);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return 1;
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception rollbackEx)
                            {
                                // Log rollback exception
                                Console.WriteLine("Rollback Exception: " + rollbackEx.Message);
                            }

                            // Log the original exception
                            Console.WriteLine("Transaction Exception: " + ex.Message);
                            return 0;
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                var date = DateTime.Now.ToString("yyyy-MM");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT TOP 1 waterbill_no FROM address_tb a 
                        JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                        JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                        WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @month
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@month", "%" + date + "%");
                        var result = await command.ExecuteScalarAsync();

                        return result.ToString();
                    }
                }
            }
            catch (Exception)
            {
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
            var WBNumber = string.IsNullOrEmpty(wbNumber) ? await GetWaterBillNumber() : wbNumber;
            var prevDate = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("yyyy-MM") : date;
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
                    WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND a.location LIKE location AND CAST(wb.waterbill_no AS varchar) LIKE @num
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

        ///////////////////////
        // CUSTOM FUNCTIONS //
        /////////////////////

        /// <summary>
        /// Retrieves unpaid water bills for a list of addresses, excluding the current water bill number.
        /// </summary>
        /// <param name="waterReading">List of water readings containing address IDs and bill numbers.</param>
        /// <returns>List of unpaid water bills with accumulated amounts.</returns>
        public async Task<List<WaterBilling>> GetUnpaidWaterBill(List<WaterReading> waterReading)
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
                        using (var command = new SqlCommand(@"SELECT COUNT(*) FROM water_billing_tb 
                        WHERE addr_id = @addr AND amount = @amount AND date_issue_from = @issueFrom AND date_issue_to = @issueTo AND due_date_from = @dueFrom AND due_date_to = @dueTo AND status= @status", connection))
                        {
                            command.Parameters.AddWithValue("@addr", item.Address_ID);
                            command.Parameters.AddWithValue("@amount", item.Amount);
                            command.Parameters.AddWithValue("@issueFrom", item.Date_Issue_From);
                            command.Parameters.AddWithValue("@issueTo", item.Date_Issue_To);
                            command.Parameters.AddWithValue("@dueFrom", item.Due_Date_From);
                            command.Parameters.AddWithValue("@dueTo", item.Due_Date_To);
                            command.Parameters.AddWithValue("@status", item.Status);

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
                    using (var command = new SqlCommand("SELECT DISTINCT CAST(waterbill_no as INT)as bill_no FROM water_billing_tb", connection))
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

        public async Task<ModelBinding> UnpaidResidentWaterBilling(string residentID)
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
                    WHERE  status = 'unpaid' AND r.res_id  = @res_id AND is_verified = 'true'", connection))
                    {
                        command.Parameters.AddWithValue("@res_id", residentID);

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
                model = new ModelBinding
                {
                    WaterBillAddress = wbaList
                };

                return model;
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
