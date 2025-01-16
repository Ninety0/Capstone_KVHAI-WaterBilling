using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace KVHAI.Repository
{
    public class PaymentRepository
    {
        private readonly DBConnect _dBConnect;
        private readonly InputSanitize _sanitize;
        private readonly WaterBillRepository _waterBill;
        private readonly ListRepository _listRepository;
        private readonly NotificationRepository _notificationRepository;

        private List<Payment> PaymentList { get; set; }

        public PaymentRepository(DBConnect dBConnect, InputSanitize sanitize, WaterBillRepository waterBill, ListRepository listRepository, NotificationRepository notificationRepository)
        {
            _dBConnect = dBConnect;
            _sanitize = sanitize;
            _waterBill = waterBill;
            _listRepository = listRepository;
            _notificationRepository = notificationRepository;
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
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var command = new SqlCommand(@"
                        INSERT INTO payment_tb (addr_id, res_id, bill, paid_amount, remaining_balance, payment_method, payment_status, payment_date, paid_by, transaction_id, payer_id,payer_email) 
                        VALUES (@address, @res, @bill, @amount, @remainingBalance, @method, @status, @date, @by, @transactionId, @payerId, @payerMail);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@address", payment.Address_ID);
                                command.Parameters.AddWithValue("@res", payment.Resident_ID);
                                command.Parameters.AddWithValue("@bill", payment.Bill);
                                command.Parameters.AddWithValue("@amount", payment.Paid_Amount.ToString("F2"));
                                command.Parameters.AddWithValue("@remainingBalance", 0); // Initially zero
                                command.Parameters.AddWithValue("@method", payment.Payment_Method);
                                command.Parameters.AddWithValue("@status", status);
                                command.Parameters.AddWithValue("@date", date);
                                command.Parameters.AddWithValue("@by", payment.Paid_By);
                                command.Parameters.AddWithValue("@transactionId", payment.PayPal_TransactionId ?? (object)DBNull.Value); // Store transaction ID if online
                                command.Parameters.AddWithValue("@payerId", payment.PayPal_PayerId ?? (object)DBNull.Value); // Store transaction ID if online
                                command.Parameters.AddWithValue("@payerMail", payment.PayPal_PayerEmail ?? (object)DBNull.Value); // Store transaction ID if online


                                result = (int)await command.ExecuteScalarAsync();
                            }

                            if (result > 0)
                            {
                                int updateResult = await UpdateBillsAfterPayment(result, payment, connection, transaction);
                                if (updateResult < 1)
                                {
                                    throw new Exception("Error updating bills after payment");
                                }
                            }

                            var resident = await _listRepository.ResidentList();
                            var lname = resident.Where(r => r.Res_ID == payment.Resident_ID.ToString())
                                .Select(l => l.Lname).FirstOrDefault();
                            var notif = new Notification
                            {
                                Title = "Water Billing",
                                Message = $"{lname} has just been paid water bill.",
                                Url = "/kvhai/staff/onlinepayment/home",
                                Message_Type = "Cashier2",
                            };
                            var notificationResult = await _notificationRepository.SendNotificationToAdmin(notif);

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return 0;
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }




        public async Task<DataTable> PrintWaterBilling(Payment payment)
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
                string paymentDate = DateTime.TryParse(payment.Payment_Date, out DateTime date) ? date.ToString("MMMM dd, yyyy") : "";

                // Setup DataTable columns
                dt.Columns.Add("Block");
                dt.Columns.Add("Lot");
                dt.Columns.Add("Street");
                dt.Columns.Add("Date");
                dt.Columns.Add("Payment");
                dt.Columns.Add("Bill");
                dt.Columns.Add("Name");
                dt.Columns.Add("Method");

                // Sample row creation (replace with your actual data processing logic)
                DataRow row = dt.NewRow();
                row["Block"] = await _sanitize.HTMLSanitizerAsync(payment.Block);
                row["Lot"] = await _sanitize.HTMLSanitizerAsync(payment.Lot);
                row["Street"] = await _sanitize.HTMLSanitizerAsync(payment.Street);
                row["Date"] = paymentDate;
                row["Payment"] = await _sanitize.HTMLSanitizerAsync(payment.Paid_Amount.ToString("F2"));
                row["Bill"] = await _sanitize.HTMLSanitizerAsync(payment.Bill.ToString("F2"));
                row["Name"] = await _sanitize.HTMLSanitizerAsync(payment.Paid_By);
                row["Method"] = await _sanitize.HTMLSanitizerAsync(payment.Payment_Method);

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

        public async Task<List<Payment>> GetNewPayment()
        {
            try
            {
                var payment = new List<Payment>();
                var residentAddress = new List<ResidentAddress>();

                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    select ra.res_id, a.addr_id,payment_id,paid_by,block,lot,st_name,payment_method, payment_status,payment_date, is_owner 
                    from payment_tb p
                    JOIN address_tb a ON p.addr_id = a.addr_id
                    JOIN resident_address_tb ra ON a.addr_id = ra.addr_id AND p.res_id = ra.res_id
                    JOIN street_tb s ON a.st_id = s.st_id 
                    WHERE CONVERT(VARCHAR, payment_date,23) LIKE '%%'
                    ORDER BY payment_date DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var _payment = new Payment
                                {
                                    Resident_ID = reader.GetInt32(0),
                                    Address_ID = reader.GetInt32(1),
                                    Payment_ID = reader.GetInt32(2),
                                    Paid_By = reader.GetString(3),
                                    Block = reader.GetString(4),
                                    Lot = reader.GetString(5),
                                    Street = reader.GetString(6),
                                    Payment_Method = reader.GetString(7),
                                    Payment_Status = reader.GetString(8),
                                    Payment_Date = reader.GetDateTime(9).ToString("MMM dd yyyy hh:mm tt"),
                                    Is_Owner = reader.GetBoolean(10),

                                };
                                payment.Add(_payment);
                            }
                        }
                    }
                }


                return payment;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public async Task<List<string>> GetWaterBillingNumber()
        {
            var wb = await _listRepository.WaterBillingList();

            var waterbillNoList = wb.Select(w => w.WaterBill_No).ToList();

            return waterbillNoList;
        }


        public async Task<List<Payment>> GetRecentOnlinePayment1(int offset, int limit, string paymentMethod, string startDate = "", string endDate = "")
        {
            var paymentList = await _listRepository.PayList();
            var methodPay = string.IsNullOrEmpty(paymentMethod) ? "" : paymentMethod;

            var onlinePayList = paymentList.Where(m => m.Payment_Method == "online").ToList();
            try
            {
                var payList = new List<Payment>();
                var query = new StringBuilder();

                query.AppendLine(@"select p.payment_id, a.addr_id,p.res_id,    
                                p.transaction_id,a.block,
	                            a.lot,s.st_name,p.paid_amount, p.bill,p.paid_by, 
	                            p.payment_status, p.payment_date, payment_method from payment_tb p
                        JOIN address_tb a ON p.addr_id = a.addr_id
                        JOIN street_tb s ON a.st_id = s.st_id WHERE");
                if (!string.IsNullOrEmpty(startDate))
                {
                    query.AppendLine(@"p.payment_date BETWEEN @sd AND @ed AND");
                }
                query.AppendLine(@"payment_method LIKE @method  ORDER BY payment_date DESC 
                        OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY");
                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        if (!string.IsNullOrEmpty(startDate))
                        {
                            command.Parameters.AddWithValue("@sd", startDate);
                            command.Parameters.AddWithValue("@ed", string.IsNullOrEmpty(endDate) ?
                                DateTime.Now.ToString("yyyy-MM-dd") : endDate);

                        }
                        command.Parameters.AddWithValue("@method", "%" + methodPay + "%");
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var payment = new Payment
                                {
                                    Payment_ID = reader.GetInt32(0),
                                    Address_ID = reader.GetInt32(1),
                                    Resident_ID = reader.GetInt32(2),
                                    PayPal_TransactionId = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    Block = reader.GetString(4),
                                    Lot = reader.GetString(5),
                                    Street = reader.GetString(6),
                                    Paid_Amount = reader.GetDecimal(7),
                                    Bill = reader.GetDecimal(8),
                                    Paid_By = reader.GetString(9),
                                    Payment_Status = reader.GetString(10),
                                    Payment_Date = reader.GetDateTime(11).ToString("MMMM dd, yyyy HH:mm:ss"),
                                    Payment_Method = reader.GetString(12),
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

        public async Task<List<Payment>> GetRecentOnlinePayment(int offset, int limit, string paymentMethod, string startDate = "", string endDate = "")
        {
            try
            {
                var payList = new List<Payment>();
                var methodPay = string.IsNullOrEmpty(paymentMethod) ? "" : paymentMethod;

                // Build the base query
                var query = new StringBuilder();
                query.AppendLine(@"
            SELECT 
                p.payment_id, a.addr_id, p.res_id,    
                p.transaction_id, a.block, a.lot, s.st_name, 
                p.paid_amount, p.bill, p.paid_by, 
                p.payment_status, p.payment_date, p.payment_method 
            FROM payment_tb p
            JOIN address_tb a ON p.addr_id = a.addr_id
            JOIN street_tb s ON a.st_id = s.st_id 
            WHERE payment_method LIKE @method");

                // Add date conditions dynamically if provided
                if (!string.IsNullOrEmpty(startDate))
                    query.Append(" AND payment_date >= @startDate");

                if (!string.IsNullOrEmpty(endDate))
                    query.Append(" AND payment_date <= @endDate");

                query.AppendLine(@"
            ORDER BY p.payment_date DESC 
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY");

                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@method", "%" + methodPay + "%");
                        command.Parameters.AddWithValue("@offset", offset);
                        command.Parameters.AddWithValue("@limit", limit);

                        if (!string.IsNullOrEmpty(startDate))
                            command.Parameters.AddWithValue("@startDate", DateTime.ParseExact(startDate, "yyyy-MM-dd", null));

                        if (!string.IsNullOrEmpty(endDate))
                            command.Parameters.AddWithValue("@endDate", DateTime.ParseExact(endDate, "yyyy-MM-dd", null));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var payment = new Payment
                                {
                                    Payment_ID = reader.GetInt32(0),
                                    Address_ID = reader.GetInt32(1),
                                    Resident_ID = reader.GetInt32(2),
                                    PayPal_TransactionId = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                    Block = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                    Lot = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                    Street = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    Paid_Amount = reader.GetDecimal(7),
                                    Bill = reader.GetDecimal(8),
                                    Paid_By = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    Payment_Status = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                    Payment_Date = reader.GetDateTime(11).ToString("yyyy-MM-dd HH:mm:ss"), // Use consistent format
                                    Payment_Method = reader.GetString(12),
                                };

                                payList.Add(payment);
                            }
                        }
                    }
                }

                return payList;
            }
            catch (Exception ex)
            {
                // Log the exception using a proper logging framework
                // Example: _logger.LogError(ex, "Error fetching recent online payments");
                Console.Error.WriteLine($"Error fetching recent online payments: {ex.Message}");
                return new List<Payment>(); // Return an empty list to ensure the caller handles it safely
            }
        }



        public async Task<int> GetCountOnlinePayment1(string paymentMethod, string startDate = "", string endDate = "")
        {
            var paymentList = await _listRepository.PayList();

            // Use LINQ to filter the payment list
            var filteredPayments = paymentList
                .Where(p => p.Payment_Method.Contains(paymentMethod, StringComparison.OrdinalIgnoreCase) &&
                            DateTime.ParseExact(p.Payment_Date, "yyyy-MM-dd", null) >= DateTime.ParseExact(startDate, "yyyy-MM-dd", null) &&
                            DateTime.ParseExact(p.Payment_Date, "yyyy-MM-dd", null) >= DateTime.ParseExact(endDate, "yyyy-MM-dd", null))
                .ToList();

            // Get the count of the filtered payments
            var count = filteredPayments.Count();

            //var count = paymentList.Where(m => m.Payment_Method == paymentMethod &&
            //m.Payment_Date == startDate)
            //    .Count();

            return count;

        }

        public async Task<int> GetCountOnlinePayment(string paymentMethod, string startDate = "", string endDate = "")
        {
            try
            {
                var query = new StringBuilder("SELECT COUNT(*) FROM payment_tb WHERE payment_method LIKE @paymentMethod");

                if (!string.IsNullOrEmpty(startDate))
                    query.Append(" AND payment_date >= @startDate");

                if (!string.IsNullOrEmpty(endDate))
                    query.Append(" AND payment_date <= @endDate");

                using (var connection = await _dBConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(query.ToString(), connection))
                    {
                        command.Parameters.AddWithValue("@paymentMethod", "%" + paymentMethod + "%");
                        if (!string.IsNullOrEmpty(startDate))
                            command.Parameters.AddWithValue("@startDate", startDate);
                        if (!string.IsNullOrEmpty(endDate))
                            command.Parameters.AddWithValue("@endDate", endDate);

                        return (int)await command.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception)
            {
                return 0; // Handle exceptions appropriately
            }
        }

    }
}
