using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class WaterBillRepository
    {
        private readonly DBConnect _dbConnect;

        public WaterBillRepository(DBConnect dBConnect)
        {
            _dbConnect = dBConnect;
        }

        ////////////
        // CREATE //
        ////////////
        public async Task<int> CreateWaterBill(WaterBilling waterBilling)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO water_billing_tb (reading_id, amount, date_billing, due_date,status) VALUES(@read, @amount, @bill, @due,@status)", connection))
                    {
                        command.Parameters.AddWithValue("@read", waterBilling.Reading_ID);
                        command.Parameters.AddWithValue("@amount", waterBilling.Amount);
                        command.Parameters.AddWithValue("@bill", waterBilling.Date_Billing);
                        command.Parameters.AddWithValue("@due", waterBilling.Due_Date);
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
    }
}
