using KVHAI.CustomClass;
using KVHAI.Models;
using System.Data.SqlClient;
using System.Text;

namespace KVHAI.Repository
{
    public class WaterReadingRepository
    {
        private readonly DBConnect _dbConnect;
        private readonly InputSanitize _sanitize;
        private readonly ListRepository _listRepository;

        public WaterReadingRepository(DBConnect dBConnect, InputSanitize inputSanitize, ListRepository listRepository)
        {
            _dbConnect = dBConnect;
            _sanitize = inputSanitize;
            _listRepository = listRepository;
        }
        ////////////
        // CREATE //
        ////////////
        public async Task<int> CreateWaterReading(WaterReading waterReading)
        {
            try
            {
                int result = 0;
                waterReading.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO water_reading_tb (emp_id,addr_id,consumption,date_reading) VALUES(@emp_id, @addr_id, @consumption, @date)", connection))
                    {
                        command.Parameters.AddWithValue("@emp_id", 1);//this will nedd to change if admin has login form yet
                        command.Parameters.AddWithValue("@addr_id", waterReading.Address_ID);
                        command.Parameters.AddWithValue("@consumption", await _sanitize.HTMLSanitizerAsync(waterReading.Consumption));
                        command.Parameters.AddWithValue("@date", waterReading.Date);

                        result = await command.ExecuteNonQueryAsync();


                        // INSERT FORECAST DATA TOO IF POSSIBLE
                        //var yearNumber = !string.IsNullOrEmpty(waterReading.Date) ? DateTime.ParseExact(waterReading.Date, "yyyy-MM-dd HH:mm:ss", null).Year : 0;
                        //INSERT ACTUAL DATA IN DATABASE
                        await InsertActualData(waterReading.Address_ID, waterReading.Date, waterReading.Consumption);


                        return result > 0 ? 1 : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public async Task InsertActualData(string addressID, string date, string current_consumption)
        {
            try
            {
                var waterReadingList = await _listRepository.WaterReadingList();
                var readingCountByAddress = waterReadingList.Where(a => a.Address_ID == addressID).Select(c => c.Consumption).Count();

                if (readingCountByAddress < 2)
                {
                    return;
                }

                var previousReading = await GetPreviousReadingForActualData(date, addressID);

                var actualData = Convert.ToDouble(current_consumption) - Convert.ToDouble(previousReading.Consumption);
                var actualDate = DateTime.TryParse(date, out DateTime _date) ? _date.AddMonths(-1) : DateTime.Now.AddMonths(-1);

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO actual_data_tb(address_id,actual_data,date)VALUES(@id,@data,@date)", connection))
                    {
                        command.Parameters.AddWithValue("@id", addressID);
                        command.Parameters.AddWithValue("@data", actualData);
                        command.Parameters.AddWithValue("@date", actualDate);

                        await command.ExecuteNonQueryAsync();

                        await InsertForecastData(addressID);
                    }
                }

            }
            catch (Exception)
            {
                //return 0;
            }
        }

        public async Task InsertForecastData(string addressID)
        {
            try
            {
                var actualDataList = await _listRepository.ActualDataList();
                var dataCountByAddress = actualDataList.Where(a => a.Address_ID == addressID).Count();
                var sortedByDateDescending = actualDataList.Where(a => a.Address_ID == addressID)
                    .OrderByDescending(a => a.Date).ToList();

                var newDataList = new List<Double>();

                if (dataCountByAddress < 5)
                {
                    return;
                }

                for (int i = 0; i < 5; i++)
                {
                    newDataList.Add(Convert.ToDouble(sortedByDateDescending[i].Actual_Data));
                }

                var average = newDataList.Average();
                var forecastDate = DateTime.Now;

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand("INSERT INTO forecast_tb(address_id,forecast_data,date)VALUES(@id,@data,@date)", connection))
                    {
                        command.Parameters.AddWithValue("@id", addressID);
                        command.Parameters.AddWithValue("@data", average);
                        command.Parameters.AddWithValue("@date", forecastDate);

                        await command.ExecuteNonQueryAsync();
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        public async Task<WaterReading> GetPreviousReadingForActualData(string date, string addressID)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(addressID))
            {
                return null;
            }

            var datePrevious = "";
            if (DateTime.TryParse(date, out DateTime _date))
            {
                datePrevious = _date.AddMonths(-1).ToString("yyyy-MM");
            }
            else
            {
                return null; // Invalid date format
            }

            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                SELECT * FROM water_reading_tb
                WHERE addr_id = @id 
                AND FORMAT(date_reading, 'yyyy-MM') = @prevdate", connection))
                    {
                        command.Parameters.AddWithValue("@id", addressID);
                        command.Parameters.AddWithValue("@prevdate", datePrevious);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new WaterReading
                                {
                                    Reading_ID = reader.GetInt32(0).ToString(),
                                    Address_ID = reader.GetInt32(2).ToString(),
                                    Consumption = reader.GetInt32(3).ToString(),
                                    Date = reader.GetDateTime(4).ToString(),
                                };
                            }
                            return null; // No reading found for previous month
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                // You might want to use proper logging here
                Console.WriteLine($"Error getting previous reading: {ex.Message}");
                throw; // or return null depending on your error handling strategy
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
            var _location = string.IsNullOrEmpty(location) ? "1" : location;
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
                WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND a.location LIKE @location 
                ORDER BY CAST(block as INT);
                ", connection))
                {
                    command.Parameters.AddWithValue("@date", "%" + prevDate + "%");
                    command.Parameters.AddWithValue("@location", "%" + _location + "%");
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading
                            {
                                Reading_ID = reader["reading_id"].ToString() ?? string.Empty,
                                Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                Consumption = reader["consumption"].ToString() ?? string.Empty,

                                Resident_ID = reader["res_id"].ToString() ?? string.Empty,
                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd") : string.Empty
                            };

                            var address = new ResidentAddress
                            {
                                Address_ID = Convert.ToInt32(reader["addr_id"].ToString() ?? string.Empty),
                                Resident_ID = Convert.ToInt32(reader["res_id"].ToString() ?? string.Empty),

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

        public async Task<ModelBinding> GetCurrentReading(string location, string date = "")
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
                    WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND a.location LIKE @location
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
                                Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                Resident_ID = reader["res_id"].ToString() ?? string.Empty,

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

        public async Task<ModelBinding> GetAllReadingByResident(string addressID, string residentID = "", string year = "")
        {
            var monthNow = DateTime.Now.ToString("yyyy-MM");
            var waterReading = new List<WaterReading>();
            var models = new ModelBinding();

            var query = new StringBuilder();

            query.AppendLine(@"select * from water_reading_tb wr
                    JOIN address_tb a ON wr.addr_id = a.addr_id
                    JOIN resident_tb r ON a.res_id = r.res_id
                    WHERE a.addr_id = @addr_id");
            if (!string.IsNullOrEmpty(year))
            {
                query.AppendLine("AND FORMAT(date_reading, 'yyyy') = @year");
            }
            query.AppendLine("ORDER BY date_reading DESC");


            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(query.ToString(), connection)) //AND r.res_id = @res_id
                {
                    command.Parameters.AddWithValue("@addr_id", addressID);
                    if (!string.IsNullOrEmpty(year))
                    {
                        command.Parameters.AddWithValue("@year", year);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading
                            {
                                Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                Resident_ID = reader["res_id"].ToString() ?? string.Empty,

                                Consumption = reader["consumption"].ToString() ?? string.Empty,

                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("MMM yyyy") : string.Empty
                            };
                            waterReading.Add(wr);
                        }
                    }
                }
            }
            models.AllWaterConsumptionByResident = waterReading;

            return models;
        }

        public async Task<List<WaterReading>> GerReadingForGraph1(string addressID, string year = "")
        {
            var monthNow = DateTime.Now.ToString("yyyy-MM");
            var waterReading = new List<WaterReading>();

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                using (var command = new SqlCommand(@"
                    select * from water_reading_tb WHERE addr_id = @addr_id AND CONVERT(VARCHAR, date_reading, 23 ) LIKE @year
                ", connection))
                {
                    command.Parameters.AddWithValue("@addr_id", addressID);
                    command.Parameters.AddWithValue("@year", "%" + year + "%");

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var wr = new WaterReading
                            {
                                Reading_ID = reader["reading_id"].ToString() ?? string.Empty,
                                Address_ID = reader["addr_id"].ToString() ?? string.Empty,

                                Consumption = reader["consumption"].ToString() ?? string.Empty,

                                Date = reader["date_reading"] != DBNull.Value ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd HH:mm:ss") : string.Empty
                            };
                            waterReading.Add(wr);
                        }
                    }
                }
            }
            return waterReading;
        }

        public async Task<List<WaterReading>> GerReadingForGraphCompensate(string addressID, int monthNum, int countData, string year = "")
        {
            var waterReading = new List<WaterReading>();
            var currentYear = int.Parse(year);
            var previousYear = currentYear - 1;

            using (var connection = await _dbConnect.GetOpenConnectionAsync())
            {
                // Fetch data for the current year
                //        using (var command = new SqlCommand(@"
                //    SELECT * 
                //    FROM water_reading_tb 
                //    WHERE addr_id = @addr_id 
                //      AND YEAR(date_reading) = @currentYear
                //    ORDER BY date_reading
                //", connection))
                //        {
                //            command.Parameters.AddWithValue("@addr_id", addressID);
                //            command.Parameters.AddWithValue("@currentYear", currentYear);

                //            using (var reader = await command.ExecuteReaderAsync())
                //            {
                //                while (await reader.ReadAsync())
                //                {


                //                    var wr = new WaterReading
                //                    {
                //                        Reading_ID = reader["reading_id"].ToString() ?? string.Empty,
                //                        Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                //                        Consumption = reader["consumption"].ToString() ?? string.Empty,
                //                        Date = reader["date_reading"] != DBNull.Value
                //                            ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd HH:mm:ss")
                //                            : string.Empty
                //                    };
                //                    monthNumber = !string.IsNullOrEmpty(wr.Date) ? DateTime.ParseExact(wr.Date, "yyyy-MM-dd HH:mm:ss", null).Month : 0;
                //                    waterReading.Add(wr);
                //                }
                //            }
                //        }

                // Check if the current year has enough data (>= 5 months)
                if (countData < 5)
                {
                    int monthReading = 0;
                    if (monthNum < 5)
                    {
                        monthReading = 12 - (5 - monthNum);//12-
                    }
                    // Fetch additional data from the previous year (months >= 9)
                    using (var command = new SqlCommand(@"
                SELECT * 
                FROM water_reading_tb 
                WHERE addr_id = @addr_id 
                  AND YEAR(date_reading) = @previousYear 
                  AND MONTH(date_reading) >= @month
                ORDER BY date_reading
            ", connection))
                    {
                        command.Parameters.AddWithValue("@addr_id", addressID);
                        command.Parameters.AddWithValue("@previousYear", previousYear);
                        command.Parameters.AddWithValue("@month", monthReading);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var wr = new WaterReading
                                {
                                    Reading_ID = reader["reading_id"].ToString() ?? string.Empty,
                                    Address_ID = reader["addr_id"].ToString() ?? string.Empty,
                                    Consumption = reader["consumption"].ToString() ?? string.Empty,
                                    Date = reader["date_reading"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["date_reading"]).ToString("yyyy-MM-dd HH:mm:ss")
                                        : string.Empty
                                };
                                waterReading.Add(wr);
                            }
                        }
                    }
                }
            }

            return waterReading.OrderBy(r => r.Date).ToList(); // Ensure the data is sorted by date
        }





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
                    WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) like @month AND ad.addr_id = @id", connection))
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

                for (int i = monthStart; i < monthEnd; i++)
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
        }

        public async Task<List<LocationPercentage>> GetDashboard()
        {
            var address = await _listRepository.AddressList();
            var waterRead = await _listRepository.WaterReadingList();
            var locationPercentList = new List<LocationPercentage>();

            for (int i = 1; i <= 4; i++)
            {
                var readLocation = await GetCountReadingByLocation(i);
                var countLocation = address.Where(l => l.Location == i.ToString()).Count();

                var percentLocation = await CalculateProgress(readLocation, countLocation);

                var _locationPercent = new LocationPercentage
                {
                    Location = $"location{i}",
                    Percentage = percentLocation
                    //Location = $"location{i}",
                    //Percentage = 25 * i
                };

                locationPercentList.Add(_locationPercent);
            }

            return locationPercentList;

        }
        private async Task<double> CalculateProgress(int countReading, int countTotal)
        {
            if (countTotal < 1)
            {
                return 0;
            }
            double percentage = Math.Round((countReading / (double)countTotal) * 100);

            return percentage;
        }
        private async Task<int> GetCountReadingByLocation(int location)
        {
            try
            {
                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                    select count(*) from water_reading_tb wr
                    JOIN address_tb a ON wr.addr_id= a.addr_id 
                    WHERE CONVERT(VARCHAR, wr.date_reading, 23 ) LIKE @date AND location = @location
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@date", "%" + DateTime.Now.ToString("yyyy-MM") + "%");
                        command.Parameters.AddWithValue("@location", location);
                        var result = await command.ExecuteScalarAsync();

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<List<int>> GetYearList()
        {
            var wrList = await _listRepository.WaterReadingList();
            var orderByYear = wrList.OrderByDescending(d => d.Date).ToList();
            var yearLst = new List<int>();
            foreach (var year in orderByYear)
            {
                if (DateTime.TryParse(year.Date, out DateTime yearResult))
                {
                    int yearData = Convert.ToInt32(yearResult.ToString("yyyy"));
                    if (!yearLst.Contains(yearData))
                    {
                        yearLst.Add(yearData);
                    }
                }
            }

            return yearLst;
        }

        public async Task<ModelBinding> GetLatestReadingByMonth()
        {
            try
            {
                var waterRead = new List<WaterReading>();
                var addressRead = new List<Address>();
                var residentRead = new List<Resident>();
                var empRead = new List<Employee>();

                using (var connection = await _dbConnect.GetOpenConnectionAsync())
                {
                    using (var command = new SqlCommand(@"
                        select * from water_reading_tb wr
                        JOIN address_tb a ON wr.addr_id = a.addr_id
                        JOIN street_tb s ON a.st_id = s.st_id
                        JOIN resident_tb r ON a.res_id = r.res_id
                        JOIN employee_tb e ON wr.emp_id = e.emp_id
                        WHERE FORMAT(date_reading, 'yyyy-MM') = @date
                        ORDER BY date_reading DESC
                    ", connection))
                    {
                        command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM"));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var _wr = new WaterReading
                                {
                                    Reading_ID = reader["reading_id"].ToString(),
                                    Emp_ID = reader["emp_id"].ToString(),
                                    Address_ID = reader["addr_id"].ToString(),
                                    Consumption = reader["consumption"].ToString(),
                                    Date = reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm:ss"),
                                };
                                waterRead.Add(_wr);

                                var _address = new Address
                                {
                                    Address_ID = Convert.ToInt32(reader["addr_id"].ToString()),
                                    Resident_ID = Convert.ToInt32(reader["res_id"].ToString()),
                                    Block = reader["block"].ToString(),
                                    Lot = reader["lot"].ToString(),
                                    Street_ID = Convert.ToInt32(reader["st_id"].ToString()),
                                    Street_Name = reader["st_name"].ToString()
                                };
                                addressRead.Add(_address);

                                var res = new Resident
                                {
                                    Res_ID = reader["res_id"].ToString(),
                                    Lname = reader["lname"].ToString(),
                                    Fname = reader["fname"].ToString(),
                                    Mname = reader["mname"].ToString(),
                                };
                                residentRead.Add(res);

                                var emp = new Employee
                                {
                                    Emp_ID = reader.GetInt32(0).ToString(),
                                    Lname = reader.GetString(32).ToString(),
                                    Fname = reader.GetString(33).ToString(),
                                    Mname = reader.GetString(34).ToString(),
                                };
                                empRead.Add(emp);

                            }
                        }


                        var model = new ModelBinding
                        {
                            WaterReadingList = waterRead,
                            AddressList = addressRead,
                            ResidentList = residentRead,
                            EmployeeList = empRead
                        };

                        return model;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
