using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class PaymentRepository
    {
        private readonly DBConnect _dBConnect;
        private readonly InputSanitize _sanitize;
        private readonly WaterBillRepository _waterBill;

        private List<Payment> PaymentList { get; set; }

        public PaymentRepository(DBConnect dBConnect, InputSanitize sanitize, WaterBillRepository waterBill)
        {
            _dBConnect = dBConnect;
            _sanitize = sanitize;
            _waterBill = waterBill;

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
                                Payment_Date = reader.GetDateTime(9),
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

        public async Task<bool> CheckPayment(string address_id)
        {
            try
            {
                //var payList = await PayList();

                //var bill = Convert.ToDouble(payList.Where(a => a.Address_ID.ToString() == address_id).Select(a => a.Bill));
                //var amountPaid = Convert.ToDouble(payList.Where(a => a.Address_ID.ToString() == address_id).Select(a => a.Paid_Amount));

                //if (amountPaid >= bill)
                //{

                //}

                var result = true;

                return result;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private async Task<int> UpdateBillsAfterPayment(int paymend_id, Payment payment, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                int result = 0;

                // Fetch the list of unpaid bills and the payments
                var bills = await _waterBill.WaterBillingList();
                var payList = await PayList(connection, transaction);

                // Find the total bill amount for the given address
                var totalBill = bills
                    .Where(b => b.Address_ID == payment.Address_ID.ToString() && b.Status == "unpaid")
                    .Sum(b => Convert.ToDouble(b.Amount));

                // Find the total amount paid for the given address
                var totalPaidAmount = payList //there is a hole in this code
                    .Where(p => p.Payment_ID == paymend_id)
                    .Sum(p => Convert.ToDouble(p.Paid_Amount));

                // Add any remaining balance from previous overpayments
                var remainingBalance = payList
                    .Where(p => p.Address_ID == payment.Address_ID)
                    .Sum(p => Convert.ToDouble(p.Remaining_Balance));
                //10000 + 0
                double availableAmount = totalPaidAmount + remainingBalance;

                // Case 1: Resident's payment covers all bills
                if (totalPaidAmount >= totalBill)
                {
                    using (var command = new SqlCommand(@"
                        UPDATE water_billing_tb 
                        SET status = 'paid' 
                        WHERE addr_id = @address AND status = 'unpaid' AND bill_date_created < GETDATE()", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@address", payment.Address_ID);
                        result = await command.ExecuteNonQueryAsync();
                    }

                    // Calculate overpayment
                    double overpayment = availableAmount - totalBill;//1000

                    // Store any remaining balance (overpayment) for future bills
                    if (overpayment > 0)
                    {
                        using (var command = new SqlCommand(@"
                    UPDATE payment_tb 
                    SET remaining_balance = @remainingBalance
                    WHERE addr_id = @address", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@remainingBalance", overpayment.ToString("F2"));
                            command.Parameters.AddWithValue("@address", payment.Address_ID);
                            result = await command.ExecuteNonQueryAsync();
                        }
                    }

                }
                // Case 2: Partial payment (Resident paid less than the total bill)
                else if (totalPaidAmount < totalBill)
                {
                    //bill = 9000
                    //paid = 3600
                    double remainingAmount = totalPaidAmount;//1. 3600 //2. 3000

                    // Get the unpaid bills, ordered by the oldest bill first
                    var unpaidBills = bills
                        .Where(b => b.Address_ID == payment.Address_ID.ToString() && b.Status == "unpaid")
                        .OrderBy(b => b.Bill_Date_Created)
                        .ToList();

                    foreach (var bill in unpaidBills)
                    {
                        double currentBillAmount = Convert.ToDouble(bill.Amount);//3600

                        if (remainingAmount >= currentBillAmount)
                        {
                            // Resident's payment fully covers this bill, mark it as paid
                            using (var command = new SqlCommand(@"
                                    UPDATE water_billing_tb 
                                    SET status = 'paid' 
                                    WHERE waterbill_id = @waterbillId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@waterbillId", bill.WaterBill_ID);
                                result = await command.ExecuteNonQueryAsync();
                            }
                            //4500 - 3600 = 900
                            remainingAmount -= currentBillAmount; // Subtract the bill amount from the remaining amount
                        }
                        else
                        {
                            // Resident's payment partially covers this bill, update the remaining amount
                            double newBillAmount = currentBillAmount - remainingAmount; //600

                            using (var command = new SqlCommand(@"
                        UPDATE water_billing_tb 
                        SET amount = @newBillAmount 
                        WHERE waterbill_id = @waterbillId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@newBillAmount", newBillAmount.ToString("F2"));
                                command.Parameters.AddWithValue("@waterbillId", bill.WaterBill_ID);
                                result = await command.ExecuteNonQueryAsync();
                            }

                            // After partially paying one bill, the remaining amount is 0
                            remainingAmount = 0;
                            break;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                throw ex;
            }


        }

        public async Task<int> InsertPayment(Payment payment)
        {
            try
            {
                int result = 0;
                string status = await GetStatus(payment.Paid_Amount, payment.Bill); // Get payment status based on the amount
                var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    // Start a SQL transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var command = new SqlCommand(@"INSERT INTO payment_tb (emp_id,addr_id,res_id,bill,paid_amount,payment_method,payment_status,payment_date,paid_by) VALUES(@emp,@address,@res,@bill,@amount,@method,@status,@date,@by);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@emp", 1); // Placeholder, change when login is implemented
                                command.Parameters.AddWithValue("@address", payment.Address_ID);
                                command.Parameters.AddWithValue("@res", payment.Resident_ID);
                                command.Parameters.AddWithValue("@bill", payment.Bill);
                                command.Parameters.AddWithValue("@amount", await _sanitize.HTMLSanitizerAsync(payment.Paid_Amount.ToString("F2")));
                                command.Parameters.AddWithValue("@method", "offline");
                                command.Parameters.AddWithValue("@status", status);
                                command.Parameters.AddWithValue("@date", date);
                                command.Parameters.AddWithValue("@by", payment.Paid_By);

                                // Insert payment into the database
                                result = (int)await command.ExecuteScalarAsync();
                            }

                            // Call the logic to update unpaid bills after inserting the payment, if the insert was successful
                            if (result > 0)
                            {
                                int updateResult = await UpdateBillsAfterPayment(result, payment, connection, transaction); // Pass the connection and transaction to ensure it's within the same transaction

                                if (updateResult < 1)
                                {
                                    throw new Exception();
                                }
                            }

                            // Commit the transaction
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // If an error occurs, roll back the transaction
                            transaction.Rollback();
                            // Log the error or rethrow for further handling
                            //throw new Exception("An error occurred while inserting payment and updating bills: " + ex.Message);
                            return 0;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // Handle any other errors that were not caught within the transaction scope
                //throw new Exception("An error occurred: " + ex.Message);
                return 0;
            }
        }



        public async Task<DataTable> PrintWaterBilling(ResidentAddress payment)
        {
            // Adjust your implementation here to loop through the list of reportWaterBilling items
            // and fetch the corresponding data.

            var dt = new DataTable();

            if (payment == null)
            {
                return dt;
            }
            try
            {
                // Setup DataTable columns
                dt.Columns.Add("Block");
                dt.Columns.Add("Lot");
                dt.Columns.Add("Street");
                dt.Columns.Add("Date");
                dt.Columns.Add("Payment");
                dt.Columns.Add("Bill");
                dt.Columns.Add("Name");

                // Sample row creation (replace with your actual data processing logic)
                DataRow row = dt.NewRow();
                row["Block"] = await _sanitize.HTMLSanitizerAsync(payment.Block);
                row["Lot"] = await _sanitize.HTMLSanitizerAsync(payment.Lot);
                row["Street"] = await _sanitize.HTMLSanitizerAsync(payment.Street);
                row["Date"] = DateTime.Now.ToString("MMMM dd, yyyy");
                row["Payment"] = await _sanitize.HTMLSanitizerAsync(payment.Payment.ToString("F2"));
                row["Bill"] = await _sanitize.HTMLSanitizerAsync(payment.TotalAmount.ToString("F2"));
                row["Name"] = await _sanitize.HTMLSanitizerAsync(payment.Name);

                dt.Rows.Add(row);
                return dt;
            }
            catch (Exception ex)
            {
                // Handle exception (logging, rethrow, etc.)
                throw new Exception($"Error generating report data: {ex.Message}");
            }
        }

        private async Task<string> GetStatus(decimal payment, decimal bill)
        {
            string status = "";
            if (payment < bill)
                status = "partial";
            else if (payment >= bill)
                status = "complete";

            return status;
        }

        private async Task<int> GetResidentID(string name)
        {
            try
            {
                var result = 0;
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        SELECT res_id from resident_tb r 
                        WHERE CONCAT(r.lname,', ',r.fname,', ',r.mname) LIKE @name", connection))
                    {
                        command.Parameters.AddWithValue("@name", name);//need to change when there is login shit

                        result = (int)await command.ExecuteScalarAsync();

                    }
                }
                return result;
            }
            catch (Exception)
            {
                throw;
                return 0;
            }
        }
    }
}
