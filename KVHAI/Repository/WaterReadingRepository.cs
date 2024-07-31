using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;

namespace KVHAI.Repository
{
    public class WaterReadingRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;

        public WaterReadingRepository(DBConnect dBConnect, InputSanitize inputSanitize)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
        }
        ////////////
        // CREATE //
        ////////////
        public async Task<int> CreateWaterReading(WaterReading waterReading)
        {
            try
            {
                waterReading.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO water_reading_tb (emp_id,addr_id,consumption,date_reading) VALUES(@emp_id, @addr_id, @consumption, @date)", connection))
                    {
                        command.Parameters.AddWithValue("@emp_id", 1);
                        command.Parameters.AddWithValue("@addr_id", waterReading.Address_ID);
                        command.Parameters.AddWithValue("@consumption", await _sanitize.HTMLSanitizerAsync(waterReading.Consumption));
                        command.Parameters.AddWithValue("@date", waterReading.Date);

                        int result = await command.ExecuteNonQueryAsync();

                        return result > 0 ? 1 : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        ////////////
        // READ //
        ////////////

        ////////////
        // UPDATE //
        ////////////

        ////////////
        // DELETE //
        ////////////
    }
}
