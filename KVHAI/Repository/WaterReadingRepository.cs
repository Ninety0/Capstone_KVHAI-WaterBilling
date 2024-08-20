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
                    using (var command = new SqlCommand("INSERT INTO water_reading_tb (emp_id,addr_id,consumption,date_reading) VALUES(@emp_id, @addr_id, @cons2umption, @date)", connection))
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

        public async Task<ModelBinding> GetPreviousReading(string location, string date = "")
        {
            var prevDate = string.IsNullOrEmpty(date) ? DateTime.Now.AddMonths(-1).ToString("yyyy-MM") : date;
            var waterReading = new List<WaterReading>();
            var residentAddress = new List<ResidentAddress>();
            var models = new ModelBinding();

            var query = await GetSqlQuery(location);


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
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

        public async Task<ModelBinding> GetCurrentReading(string location, string date = "")
        {
            var prevDate = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("yyyy-MM") : date;
            var waterReading = new List<WaterReading>();
            var models = new ModelBinding();
            var query = await GetSqlQuery(location);


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    command.Parameters.AddWithValue("@location", "%" + location + "%");
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
            //await GetPreviousReading();
            //await GetCurrentReading();
        }


        ////////////
        // UPDATE //
        ////////////

        ////////////
        // DELETE //
        ////////////

        ///////////////////////
        // CUSTOM FUNCTIONS //
        /////////////////////
        public async Task<bool> CheckExistReading(string id)
        {
            try
            {
                var month = DateTime.Now.ToString("yyyy-MM");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    Select COUNT(*) from water_reading_tb wr 
                    JOIN address_tb ad ON wr.addr_id = ad.addr_id
                    WHERE date_reading like @month AND ad.addr_id = @id", connection))
                    {
                        command.Parameters.AddWithValue("@month", "%" + month + "%");
                        command.Parameters.AddWithValue("@id", id);

                        int result = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

        private async Task<string> GetSqlQuery(string parameter)
        {
            string query = "";

            if (string.IsNullOrEmpty(parameter))
            {
                //QUERY TO GET ALL
                query = @"
                        SELECT *
                        FROM (
                            SELECT 
		                        wr.reading_id, wr.emp_id, wr.addr_id,ad.res_id,consumption, date_reading,location
                            FROM water_reading_tb wr 
                            JOIN address_tb ad ON wr.addr_id = ad.addr_id
	                        WHERE wr.date_reading LIKE @date AND ad.location LIKE @location
                        ) AS nested_readings
                        RIGHT JOIN resident_tb res ON nested_readings.res_id = res.res_id
                        ORDER BY CAST(block AS INT)";
            }
            else
            {
                //QUERY TO GET BASED ON LOCATION
                query = @"
                        SELECT *
                        FROM (
                            SELECT 
		                        wr.reading_id, wr.emp_id, wr.addr_id,ad.res_id,consumption, date_reading,location
                            FROM water_reading_tb wr 
                            JOIN address_tb ad ON wr.addr_id = ad.addr_id
	                        WHERE wr.date_reading LIKE @date AND ad.location LIKE @location
                        ) AS nested_readings
                        JOIN resident_tb res ON nested_readings.res_id = res.res_id
                        ORDER BY CAST(block AS INT)";
            }

            return query;
        }

        public async Task<List<string>> GetWaterBillNo()
        {
            List<string> bill_no = new List<string>();
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("SELECT * FROM waterbill_cycle_tb", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bill_no.Add(reader["bill_no"].ToString() ?? string.Empty);
                            }
                        }
                    }
                }
                return bill_no;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<(List<string> StartDate, List<string> EndDate)> WaterReadingList()
        {
            try
            {
                var DateRange = new List<string>();
                var dateList = new List<int>();
                var dateFrom = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    SELECT DISTINCT CAST(date_reading AS DATE) AS reading_date
                    FROM water_reading_tb
                    ORDER BY reading_date;
                    ", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var date = "";

                                if (DateTime.TryParse(reader["reading_date"].ToString() ?? string.Empty, out DateTime result))
                                {
                                    date = result.ToString("MM");
                                }
                                dateList.Add(Convert.ToInt32(date));

                            }
                        }
                    }
                }

                var monthStart = dateList.FirstOrDefault();
                var monthEnd = dateList.LastOrDefault();
                var startListDate = new List<string>();
                var endListDate = new List<string>();

                for (int i = monthStart; i <= monthEnd; i++)
                {
                    string start = DateTime.Now.ToString("yyyy-") + i.ToString() + "-1";

                    string end = DateTime.Now.Year.ToString() + "-" + (i + 1).ToString() + "-" + DateTime.DaysInMonth(DateTime.Now.Year, i + 1).ToString();

                    //string end = DateTime.Now.Year.ToString() + "-" + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i + 1).ToString() + "-" + DateTime.DaysInMonth(DateTime.Now.Year, i + 1).ToString();

                    startListDate.Add(start);
                    endListDate.Add(end);

                    DateRange.Add("Water Reading" + start + " To " + end);
                }

                return (startListDate, endListDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (null, null);
            }

        }

        public async Task DateRange()
        {
            var dateStart = "";
            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand("SELECT top 1 FROM water_reading_tb", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (DateTime.TryParse(reader["date_reading"].ToString() ?? string.Empty, out DateTime result))
                            {
                                dateStart = result.ToString("yyyy-MM");
                            }

                        }
                    }
                }
            }
            //var dateFrom =

        }
    }
}
