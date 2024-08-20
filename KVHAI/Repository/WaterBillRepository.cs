using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class WaterBillRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly WaterBillingFunction _waterBillingFunction;

        public WaterBillRepository(DBConnect dBConnect, WaterBillingFunction waterBillingFunction)
        {
            _dbConnect = dBConnect;
            _waterBillingFunction = waterBillingFunction;
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
                        command.Parameters.AddWithValue("@read", waterBilling.Reading_ID);
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
                                using (var command = new SqlCommand("INSERT INTO water_billing_tb (reading_id, amount, date_issue_from, date_issue_to,due_date_from,due_date_to,status,waterbill_no) VALUES(@read, @amount,@issueFrom,@issueTo,@dueFrom,@dueTo,@status,@bill)", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@read", item.Reading_ID);
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

        ///////////////////////
        // CUSTOM FUNCTIONS //
        /////////////////////
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
            if (DateTime.TryParse(DateFrom, out DateTime result))
            {
                DateFrom = result.ToString("yyyy-MM-dd");
            }

            string DateTo = DateTime.Now.ToString("yyyy-MM-dd");

            var wb = new WaterBilling()
            {
                Date_Issue_From = DateFrom,
                Date_Issue_To = DateTo
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
                        WHERE reading_id = @read AND amount = @amount AND date_issue_from = @issueFrom AND date_issue_to = @issueTo AND due_date_from = @dueFrom AND due_date_to = @dueTo AND status= @status", connection))
                        {
                            command.Parameters.AddWithValue("@read", item.Reading_ID);
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

    }
}

//MALE LOGIC KO PAANO KAPAG MULTIPLE DATES NA LAGI LANG MAKUKUHA YUNG UNANG ENTRY HOW ABOUT THE OTHER MONTS 
//LOOK GPT TOMMOROW.
