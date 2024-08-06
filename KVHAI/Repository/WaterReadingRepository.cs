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


        ///////////
        // READ ///
        ///////////
        public async Task<List<WaterReading>> GetAllWaterReading(string location)
        {
            var reading = new List<WaterReading>();
            var prev = "2024-07-01";
            var current = "2024-08-01";

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                      select * from water_reading_tb wr 
                      JOIN address_tb ad ON wr.addr_id = ad.addr_id
                      WHERE location = @location", connection))
                {
                    command.Parameters.AddWithValue("@location", location);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading();
                            reading.Add(wr);

                        }
                    }
                }
            }

            return reading;
        }

        public async Task<ModelBinding> GetPreviousReading()
        {
            var prevDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM");
            var waterReading = new List<WaterReading>();
            var residentAddress = new List<ResidentAddress>();
            var models = new ModelBinding();


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                      SELECT *
                        FROM (
                            SELECT 
		                        wr.reading_id, wr.emp_id, wr.addr_id,ad.res_id,consumption, date_reading,location
                            FROM water_reading_tb wr 
                            JOIN address_tb ad ON wr.addr_id = ad.addr_id
	                        WHERE wr.date_reading LIKE @date
                        ) AS nested_readings
                        RIGHT JOIN resident_tb res ON nested_readings.res_id = res.res_id", connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading
                            {
                                Consumption = reader["consumption"].ToString() ?? string.Empty,

                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
                            };

                            var address = new ResidentAddress
                            {
                                Name = string.Concat(reader["lname"].ToString(), ", ", reader["fname"].ToString()) ?? string.Empty,
                                Block = reader["block"].ToString() ?? string.Empty,
                                Lot = reader["lot"].ToString() ?? string.Empty,
                            };
                            //var wr = new WaterReading();
                            //var address = new ResidentAddress();

                            //address.Name = string.Concat(", ", reader["lname"].ToString(), reader["fname"].ToString()) ?? string.Empty;
                            //address.Block = reader["block"].ToString() ?? string.Empty;
                            //address.Lot = reader["lot"].ToString() ?? string.Empty;
                            //wr.Consumption = reader["consumption"].ToString() ?? string.Empty;
                            //wr.Date = reader["date_reading"].ToString() ?? string.Empty;

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

        public async Task<ModelBinding> GetCurrentReading()
        {
            var prevDate = DateTime.Now.ToString("yyyy-MM");
            var waterReading = new List<WaterReading>();
            var models = new ModelBinding();


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                      SELECT *
                        FROM (
                            SELECT 
		                        wr.reading_id, wr.emp_id, wr.addr_id,ad.res_id,consumption, date_reading,location
                            FROM water_reading_tb wr 
                            JOIN address_tb ad ON wr.addr_id = ad.addr_id
	                        WHERE wr.date_reading LIKE @date
                        ) AS nested_readings
                        RIGHT JOIN resident_tb res ON nested_readings.res_id = res.res_id", connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading
                            {
                                Consumption = reader["consumption"].ToString() ?? string.Empty,

                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
                            };


                            waterReading.Add(wr);

                        }
                    }
                }
            }
            models.CurrentReading = waterReading;

            return models;
        }

        public async Task GetReadingConsumption()
        {
            await GetPreviousReading();
            await GetCurrentReading();
        }


        ////////////
        // UPDATE //
        ////////////

        ////////////
        // DELETE //
        ////////////
    }
}
