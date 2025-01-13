using KVHAI.Models;
using KVHAI.Repository;

namespace KVHAI.CustomClass
{
    public class ForecastingRepo
    {
        private readonly DBConnect _dbConnect;
        private readonly ListRepository _listRepository;

        public ForecastingRepo(DBConnect dBConnect, ListRepository listRepository)
        {
            _dbConnect = dBConnect;
            _listRepository = listRepository;
        }

        //public async Task GetActualDataForGraph()
        //{
        //    var dateFrom =
        //    using (var connect = await _dbConnect.GetOpenConnectionAsync())
        //    {
        //        using (var command = new SqlCommand("select * from water_billing_tb wb WHERE date_issue_from between @dateFrom AND @dateTo", connect))
        //        {
        //            command.Parameters.AddWithValue("");
        //        }
        //    }
        //}

        // Calculate the simple moving average
        public List<double?> CalculateMovingAverage1(List<double?> dataList, int period)
        {
            List<double?> movingAverage = new List<double?>();
            //{ 1000, 1200, 1300, 1400, 1600}A.D
            //{  } M.A
            //for (int i = 0; i < dataList.Count; i++)
            //{
            //    //0 + 1 = 1 >= 5 = f null
            //    //1 + 1 = 2 >= 5 = f null
            //    //2 + 1 = 3 >= 5 = f null
            //    //3 + 1 = 4 >= 5 = f null
            //    //4 + 1 = 5 > 5 = f  null
            //    if (i + 1 >= period)
            //    {
            //        // Calculate average for the last 'period' values
            //        //skip(i+1 = 1 - 5 = -4).Take(5)
            //        double? average = dataList.Skip(i + 1 - period).Take(period).Average();
            //        movingAverage.Add(average);
            //    }
            //    else
            //    {
            //        // Not enough data for moving average, append null
            //        movingAverage.Add(null);
            //    }
            //}

            for (int i = 0; i < dataList.Count; i++)
            {
                if (i + 1 >= period)
                {
                    // Calculate average for the previous 'period' values
                    double? average = dataList.Skip(i - period).Take(period).Average();
                    movingAverage.Add(average);
                }
                else
                {
                    // Not enough data for moving average, append null
                    movingAverage.Add(null);
                }
            }

            // Add one more null at the beginning and remove the last calculated value
            movingAverage.Insert(0, null);
            //movingAverage.RemoveAt(movingAverage.Count - 1);

            return movingAverage;
        }

        public List<double?> CalculateMovingAverage(List<double?> dataList, int period, int startMonth)
        {
            List<double?> movingAverage = new List<double?>();
            for (int i = 0; i < dataList.Count; i++)
            {
                if (i + 1 >= period && i >= startMonth - 1)
                {
                    // Calculate average for the previous 'period' values within the same year
                    double? average = dataList.Skip(Math.Max(0, i + 1 - period)).Take(period).Average();
                    movingAverage.Add(average);
                }
                else
                {
                    // Not enough data for moving average, append null
                    movingAverage.Add(null);
                }
            }

            movingAverage.Insert(0, null);
            return movingAverage;
        }

        public List<double?> CalculateMovingAverageReplicate(List<double?> dataList, int period)
        {
            List<double?> movingAverage = new List<double?>();
            //{ 1000, 1200, 1300, 1400, 1600}A.D
            //{  } M.A

            for (int i = 0; i < dataList.Count; i++)
            {
                if (i + 1 >= period)
                {
                    // Calculate average for the previous 'period' values
                    double? average = dataList.Skip(i - period).Take(period).Average();
                    movingAverage.Add(average);
                }
                else
                {
                    // Not enough data for moving average, append null
                    movingAverage.Add(null);
                }
            }

            // Add one more null at the beginning and remove the last calculated value
            movingAverage.Insert(0, null);
            //movingAverage.RemoveAt(movingAverage.Count - 1);

            return movingAverage;
        }

        // Get percentage change and insights
        public async Task<Forecasting> GetPercentChange()
        {
            var forecast = new Forecasting();
            var years = new[] { 2024, 2025 }; //we can have get year

            foreach (var year in years)
            {
                var actualData = GetActualDataForYear(year);
                var movingAverage = CalculateMovingAverage1(actualData, 5);
                var percentChange = new List<double?>();
                var insights = new List<string>();

                ProcessYearData(actualData, movingAverage, percentChange, insights);

                forecast.YearlyData[year] = new YearData
                {
                    ActualData = actualData,
                    MovingAverage = movingAverage,
                    PercentChange = percentChange,
                    Insights = insights
                };
            }

            return forecast;
        }

        public async Task<Forecasting> GetPercentChange1(YearData forecastData, List<string> yearList)
        {
            var forecast = new Forecasting();
            var years = new[] { 2024 }; //we can have get year

            foreach (var year in yearList)
            {
                var _actualDataList = GetActualDataList(forecastData);
                var _movingAverageData = CalculateMovingAverage1(_actualDataList, 5);
                var percentChange = new List<double?>();
                var insights = new List<string>();

                ProcessYearData(_actualDataList, _movingAverageData, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = _actualDataList,
                    MovingAverage = _movingAverageData,
                    PercentChange = percentChange,
                    Insights = insights
                };
            }

            return forecast;
        }

        public async Task<Forecasting> GetPercentChange(YearData forecastData, List<string> yearList)
        {
            var forecast = new Forecasting();

            foreach (var year in yearList)
            {
                var actualDataList = GetActualDataList(forecastData);
                var movingAverageData = CalculateMovingAverage1(actualDataList, 5);
                //var movingAverageData = CalculateMovingAverage(actualDataList, 5, forecastData.ConsumptionMonth[0]);

                var percentChange = new List<double?>();
                var insights = new List<string>();

                ProcessYearData(actualDataList, movingAverageData, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = actualDataList,
                    MovingAverage = movingAverageData,
                    PercentChange = percentChange,
                    Insights = insights
                };
            }

            return forecast;
        }

        public async Task<Forecasting> GetPercentDuplicate(YearData forecastData, string year, Dictionary<string, List<double>> yearlyConsumptionData)
        {
            try
            {
                var forecast = new Forecasting();
                var dataList = new List<Double?>();

                foreach (var value in yearlyConsumptionData[year])
                {
                    dataList.Add(Convert.ToDouble(value));
                }

                //for (int i = 0; i < yearlyConsumptionData.Keys.Count; i++)
                //{
                //    // Get the value at specific index and convert it
                //    var data = Convert.ToDouble(yearlyConsumptionData.Values.ElementAt(i));
                //    dataList.Add(data);
                //}

                var actualDataList = GetActualDataList(forecastData);
                var movingAverageData = CalculateMovingAverage1(actualDataList, 5);
                //var movingAverageData = CalculateMovingAverage1(actualDataList, 5);
                //var movingAverageData = CalculateMovingAverage(actualDataList, 5, forecastData.ConsumptionMonth[0]);

                var percentChange = new List<double?>();
                var insights = new List<string>();

                ProcessYearData(actualDataList, movingAverageData, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = dataList,
                    MovingAverage = movingAverageData,
                    PercentChange = percentChange,
                    Insights = insights
                };

                return forecast;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Forecasting> GetPercentChangeCopy(YearData forecastData, List<string> yearList, Dictionary<string, List<int>> yearlyConsumptionData)
        {
            var forecast = new Forecasting();
            /*
             the data from 2025 the only thing we need

to compensate or to make it continous data we need to get the 
last 5 months from 2024 to have forecast data in 2025

and the actual data from 2025 january is none
             */

            //for (int i = 0; i < yearList.Count; i++)
            //{
            //    var item = yearlyConsumptionData.ElementAt(i);
            //    var value = item.Value;
            //}

            foreach (var year in yearList)
            {
                var actualDataList = GetActualDataList(forecastData);
                var movingAverageData = CalculateMovingAverage1(actualDataList, 5);
                //var movingAverageData = CalculateMovingAverage(actualDataList, 5, forecastData.ConsumptionMonth[0]);

                var percentChange = new List<double?>();
                var insights = new List<string>();

                ProcessYearData(actualDataList, movingAverageData, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = actualDataList,
                    MovingAverage = movingAverageData,
                    PercentChange = percentChange,
                    Insights = insights
                };
            }

            return forecast;
        }

        //user
        public async Task<Forecasting> GetPercentDatabase(string address_id, string year)
        {
            try
            {
                var actualDataList = await _listRepository.ActualDataList();
                var forecastDataList = await _listRepository.ForecastDataList();
                var forecast = new Forecasting();
                var dataList = new List<Double?>();
                var forecastedData = new List<Double?>();

                var actualDataByAddress = actualDataList.Where(a => a.Address_ID == address_id &&
                 DateTime.ParseExact(a.Date, "yyyy-MM-dd", null).Year.ToString() == year)
                    .Select(a => (double?)Convert.ToDouble(a.Actual_Data)).ToList();

                var forecastDataByAddress = forecastDataList.Where(a => a.Address_ID == address_id &&
                 DateTime.ParseExact(a.Date, "yyyy-MM-dd", null).Year.ToString() == year).ToList();

                var percentChange = new List<double?>();
                var insights = new List<string>();

                if (forecastDataByAddress.Count > 0)
                {
                    // Find the start month of forecast data
                    var firstDateString = forecastDataByAddress.Select(d => d.Date).FirstOrDefault();
                    int startMonth = !string.IsNullOrEmpty(firstDateString)
                        ? DateTime.ParseExact(firstDateString, "yyyy-MM-dd", null).Month
                        : 1; // Default to January if no valid date

                    // Initialize forecastedData with null values up to the start month
                    forecastedData = Enumerable.Repeat<double?>(null, startMonth - 1).ToList();

                    // Extract forecast data and append it to forecastedData
                    var _fd = forecastDataByAddress.Where(a => a.Address_ID == address_id)
                        .Select(a => (double?)Convert.ToDouble(a.Forecast_Data))
                        .ToList();

                    forecastedData.AddRange(_fd);
                }

                ProcessYearData(actualDataByAddress, forecastedData, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = actualDataByAddress,
                    MovingAverage = forecastedData,
                    PercentChange = percentChange,
                    Insights = insights
                };

                return forecast;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //admin
        public async Task<Forecasting> GetPercentDatabaseAdmin(string year)
        {
            try
            {
                var actualDataList = await _listRepository.ActualDataList();
                var forecastDataList = await _listRepository.ForecastDataList();
                var forecast = new Forecasting();
                var dataList = new List<Double?>();
                var forecastedData = new List<Double?>();
                var actualDataAddressMonthlyValues = new List<double?>(new double?[12]); // Initialize list for 12 months (0-based)

                // Populate the list
                foreach (var item in actualDataList)
                {
                    // Parse the date and get the month index (0-based for the list)
                    var indexMonth = DateTime.ParseExact(item.Date, "yyyy-MM-dd", null).Month - 1;

                    // Add the value to the corresponding month
                    actualDataAddressMonthlyValues[indexMonth] += (double?)Convert.ToDouble(item.Actual_Data);
                }

                var forecastDataAddress = forecastDataList.Where(a => DateTime.ParseExact(a.Date, "yyyy-MM-dd", null).Year.ToString() == year).ToList();

                var percentChange = new List<double?>();
                var insights = new List<string>();

                if (forecastDataAddress.Count > 0)
                {
                    // Initialize a list for 12 months (null values by default)
                    var forecastDataMonthlyValues = new List<double?>(new double?[12]);

                    // Sum the forecast data values per month
                    foreach (var item in forecastDataAddress)
                    {
                        // Parse the date and get the month index (0-based)
                        var indexMonth = DateTime.ParseExact(item.Date, "yyyy-MM-dd", null).Month - 1;

                        // Add the forecast data to the corresponding month
                        forecastDataMonthlyValues[indexMonth] += (double?)Convert.ToDouble(item.Forecast_Data);
                    }

                    // Find the start month of forecast data
                    var firstDateString = forecastDataAddress.Select(d => d.Date).FirstOrDefault();
                    int startMonth = !string.IsNullOrEmpty(firstDateString)
                        ? DateTime.ParseExact(firstDateString, "yyyy-MM-dd", null).Month
                        : 1; // Default to January if no valid date

                    // Initialize forecastedData with null values up to the start month
                    forecastedData = Enumerable.Repeat<double?>(null, startMonth - 1).ToList();

                    // Append the summed forecast data values to forecastedData
                    forecastedData.AddRange(forecastDataMonthlyValues.Skip(startMonth - 1));
                }

                ProcessYearData(actualDataAddressMonthlyValues, forecastedData, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = actualDataAddressMonthlyValues,
                    MovingAverage = forecastedData,
                    PercentChange = percentChange,
                    Insights = insights
                };

                return forecast;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Forecasting> GetPercentDatabase1(string address_id, string year)
        {
            try
            {
                var actualDataList = await _listRepository.ActualDataList();
                var forecastDataList = await _listRepository.ForecastDataList();
                var forecast = new Forecasting();

                // Filter actual data and forecast data by address and year
                var actualDataByAddress = actualDataList
                    .Where(a => a.Address_ID == address_id)
                    .OrderBy(a => a.Date)
                    .Select(a => (double?)Convert.ToDouble(a.Actual_Data))
                .ToList();
                var forecastDataByAddress = forecastDataList
                    .Where(a => a.Address_ID == address_id)
                    .OrderBy(a => a.Date)
                    .Select(a => new { Value = (double?)Convert.ToDouble(a.Forecast_Data), Month = !string.IsNullOrEmpty(a.Date) ? DateTime.ParseExact(a.Date, "yyyy-MM-dd", null).Month : 0 })
                    .ToList();

                var percentChange = new List<double?>();
                var insights = new List<string>();

                // Align moving average data with the correct month in actual data
                var movingAverage = actualDataByAddress
                    .Select((value, index) =>
                    {
                        var month = index + 1; // Assuming months are indexed 1-12
                        var matchingForecast = forecastDataByAddress.FirstOrDefault(f => f.Month == month);
                        return matchingForecast?.Value;
                    })
                    .ToList();

                ProcessYearData(actualDataByAddress, movingAverage, percentChange, insights);

                forecast.YearlyData[Convert.ToInt32(year)] = new YearData
                {
                    ActualData = actualDataByAddress,
                    MovingAverage = movingAverage,
                    PercentChange = percentChange,
                    Insights = insights
                };

                return forecast;
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        private List<double?> GetActualDataList1(YearData forecastData)
        {
            var actualDataList = new List<double?>();
            var startMonth = forecastData.ConsumptionMonth[0];
            var endMonth = forecastData.ConsumptionMonth.LastOrDefault();
            for (int i = 0; i < endMonth; i++)
            {
                if (i + 1 >= startMonth && i - startMonth + 1 < forecastData.ActualData.Count)
                {
                    actualDataList.Add(forecastData.ActualData[i - startMonth + 1]);
                }
                else
                {
                    actualDataList.Add(null);
                }
            }
            return actualDataList;
        }

        private List<double?> GetActualDataList(YearData forecastData)
        {
            var actualDataList = new List<double?>();
            var startMonth = forecastData.ConsumptionMonth[0];
            var endMonth = forecastData.ConsumptionMonth.LastOrDefault();

            // Normalize the range of months (e.g., 9 to 1 becomes 9 to 13)
            int normalizedEndMonth = endMonth < startMonth ? endMonth + 12 : endMonth;

            for (int i = startMonth; i <= normalizedEndMonth; i++)
            {
                int actualIndex = (i - 1) % 12; // Wrap around to valid month index (0 to 11)
                int dataIndex = i - startMonth; // Offset within the ActualData list

                if (dataIndex < forecastData.ActualData.Count)
                {
                    actualDataList.Add(forecastData.ActualData[dataIndex]);
                }
                else
                {
                    actualDataList.Add(null); // No data for this month
                }
            }

            return actualDataList;
        }


        //here we will create a logic to get the data per year
        private List<double?> GetActualDataForYear(int year)
        {
            // This method would return the actual data for the given year
            // For now, we'll use dummy data
            return new List<double?> { 1000, 1200, 1300, 1400, 1600, 1800, 1500, 1700, 1400, 2800, 3100 };
        }

        private void ProcessYearData(List<double?> actualData, List<double?> movingAverage, List<double?> percentChange, List<string> insights)
        {
            try
            {
                for (int i = 0; i < actualData.Count; i++)
                {
                    double? actual = actualData[i];
                    if (i < movingAverage.Count && movingAverage[i].HasValue)
                    {
                        double moveAvg = movingAverage[i].Value;
                        double? percent = PercentChangeFormula(actual, moveAvg);
                        string message = GenerateForecastInsights(percent);
                        percentChange.Add(percent);
                        insights.Add(message);
                    }
                    else
                    {
                        percentChange.Add(null);
                        insights.Add(null);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        // Calculate percentage change
        private double? PercentChangeFormula(double? actualData, double forecastData)
        {
            return ((actualData - forecastData) / forecastData) * 100;
        }

        // Generate insight based on percentage change
        public string GenerateForecastInsights(double? percentChange)
        {
            if (percentChange >= -5 && percentChange <= 5)
            {
                return "Stable: Water consumption is within normal limits.";
            }
            else if (percentChange > 5 && percentChange <= 25)
            {
                return "Moderate Increase: Water consumption is slightly higher than usual average consumption.";
            }
            else if (percentChange > 25 && percentChange <= 50)
            {
                return "High Increase: Water consumption is rising significantly above the recent average.";
            }
            else if (percentChange > 50)
            {
                return "Critical: Water consumption has spiked dramatically. Immediate investigation recommended.";
            }
            else
            {
                return "Decrease in Demand: Water consumption is below the recent average.";
            }
        }
    }
}
