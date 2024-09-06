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
                            int waterbillID;

                            // Check if there is already a waterbill_no for the current month
                            using (var command = new SqlCommand("SELECT TOP 1 bill_no FROM waterbill_cycle_tb WHERE created_at LIKE @currentMonth", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@currentMonth", "%" + currentMonth + "%");
                                var result = await command.ExecuteScalarAsync();

                                if (result != null)
                                {
                                    waterbillID = (int)result;
                                }
                                else
                                {
                                    // If no waterbill_no exists for the current month, create a new one
                                    using (var insertCommand = new SqlCommand("INSERT INTO waterbill_cycle_tb (created_at) OUTPUT INSERTED.bill_no VALUES(@dateNow)", connection, transaction))
                                    {
                                        insertCommand.Parameters.AddWithValue("@dateNow", dateNow);
                                        waterbillID = (int)await insertCommand.ExecuteScalarAsync();
                                    }
                                }
                            }

                            foreach (var item in waterBilling)
                            {
                                using (var command = new SqlCommand("INSERT INTO water_billing_tb (addr_id, amount, date_issue_from, date_issue_to,due_date_from,due_date_to,status,waterbill_no) VALUES(@addr, @amount,@issueFrom,@issueTo,@dueFrom,@dueTo,@status,@bill)", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@addr", item.Address_ID);
                                    command.Parameters.AddWithValue("@amount", item.Amount);
                                    command.Parameters.AddWithValue("@issueFrom", item.Date_Issue_From);
                                    command.Parameters.AddWithValue("@issueTo", item.Date_Issue_To);
                                    command.Parameters.AddWithValue("@dueFrom", item.Due_Date_From);
                                    command.Parameters.AddWithValue("@dueTo", item.Due_Date_To);
                                    command.Parameters.AddWithValue("@status", item.Status);
                                    command.Parameters.AddWithValue("@bill", waterbillID);

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
        public async Task<ModelBinding> GetPreviousReading(string location, string date = "")
        {
            var prevDate = string.IsNullOrEmpty(date) ? DateTime.Now.AddMonths(-1).ToString("yyyy-MM") : date;
            var waterReading = new List<WaterReading>();
            var residentAddress = new List<ResidentAddress>();
            var models = new ModelBinding();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                select * from resident_tb r 
                JOIN address_tb a ON r.res_id = a.res_id
                JOIN water_reading_tb wr ON a.addr_id = wr.addr_id
                JOIN water_billing_tb wb ON a.addr_id = wb.addr_id
                WHERE wr.date_reading LIKE @date AND a.location LIKE @location 
                ORDER BY CAST(block as INT);
                ", connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    command.Parameters.AddWithValue("@location", "%" + location + "%");
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

            return models;
        }

        public async Task<ModelBinding> GetCurrentReading(string location, string date = "", string num = "")
        {
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
                    WHERE wr.date_reading LIKE @date AND a.location LIKE @location AND wb.waterbill_no LIKE @num
                    ORDER BY CAST(block as INT);
                ", connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    command.Parameters.AddWithValue("@location", "%" + location + "%");
                    command.Parameters.AddWithValue("@num", "%" + num + "%");

                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading
                            {
                                Consumption = reader["consumption"].ToString() ?? string.Empty,

                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
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
                        WHERE wr.date_reading LIKE @date AND waterbill_id = @id AND wb.waterbill_no = @num
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
                        WHERE wr.date_reading LIKE @date AND waterbill_id = @id AND wb.waterbill_no = @num
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
                                        Consumption = reader["consumption"].ToString() ?? string.Empty,

                                        Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
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

        public async Task<List<WaterBilling>> GetUnpaidWaterBill(List<WaterReading> waterReading)
        {
            try
            {
                var wbList = new List<WaterBilling>();
                var ids = "";
                var dateFrom = "";
                var dateTo = "";
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    if (waterReading.Count > 0)
                    {
                        foreach (var item in waterReading)
                        {
                            var id = item.Address_ID;
                            if (DateTime.TryParse(item.Date, out DateTime output))
                            {
                                var lastDate = DateTime.DaysInMonth(DateTime.Now.Year, Convert.ToInt32(output.ToString("MM"))).ToString();
                                dateFrom = output.AddMonths(-2).ToString("yyyy-MM-01");
                                dateTo = output.AddMonths(-1).ToString($"yyyy-MM-{lastDate}");
                            }

                            using (var command = new SqlCommand(@"
                                SELECT * 
                                FROM water_billing_tb w
                                JOIN address_tb a ON w.addr_id = a.addr_id
                                JOIN water_reading_tb r ON a.addr_id = r.addr_id
                                WHERE a.addr_id = @id 
                                    AND status = 'unpaid' 
                                    AND date_reading BETWEEN @from AND @to
                            ", connection))
                            {
                                command.Parameters.AddWithValue("@id", id);
                                command.Parameters.AddWithValue("@from", id);
                                command.Parameters.AddWithValue("@to", id);

                                using (var reader = await command.ExecuteReaderAsync())
                                {
                                    var waterBillIds = new List<string>();
                                    var totalAmount = 0.0;

                                    while (await reader.ReadAsync())
                                    {
                                        var waterBillId = "WB" + reader["waterbill_id"].ToString();
                                        var amount = Convert.ToDouble(reader["amount"]);

                                        // Add the waterbill_id to the list
                                        waterBillIds.Add(waterBillId);

                                        // Sum the amount
                                        totalAmount += amount;
                                    }

                                    if (waterBillIds.Count > 0)
                                    {
                                        var wb = new WaterBilling()
                                        {
                                            PreviousWaterBill = string.Join("-", waterBillIds),  // Concatenate IDs with "-"
                                            PreviousWaterBillAmount = totalAmount.ToString("F2") // Format total amount as a string with 2 decimal places
                                        };

                                        wbList.Add(wb);
                                    }
                                }
                            }
                        }
                    }

                }
                return wbList;
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

        public async Task<WaterBilling> GetDateBilling(string date)
        {
            string DateFrom = date;
            //if (DateTime.TryParse(DateFrom, out DateTime result))
            //{
            //    DateFrom = result.ToString("yyyy-MM-dd");
            //}

            //string DateTo = DateTime.Now.ToString("yyyy-MM-dd");

            //var wb = new WaterBilling()
            //{
            //    Date_Issue_From = DateFrom,
            //    Date_Issue_To = DateTo
            //};
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

    }
}

//MALE LOGIC KO PAANO KAPAG MULTIPLE DATES NA LAGI LANG MAKUKUHA YUNG UNANG ENTRY HOW ABOUT THE OTHER MONTS 
//LOOK GPT TOMMOROW.
