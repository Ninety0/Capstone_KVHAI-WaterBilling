using KVHAI.Models;

namespace KVHAI.CustomClass
{
    public class ForecastingRepo
    {
        private readonly DBConnect _dbConnect;

        public ForecastingRepo(DBConnect dBConnect)
        {
            _dbConnect = dBConnect;
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
        public List<double?> CalculateMovingAverage(List<double?> dataList, int period)
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

        // Get percentage change and insights
        public async Task<Forecasting> GetPercentChange()
        {
            var forecast = new Forecasting();
            var years = new[] { 2024, 2025 }; //we can have get year

            foreach (var year in years)
            {
                var actualData = GetActualDataForYear(year);
                var movingAverage = CalculateMovingAverage(actualData, 5);
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

        public async Task<Forecasting> GetPercentChange(YearData forecastData, List<string> yearList)
        {
            var forecast = new Forecasting();
            var years = new[] { 2024 }; //we can have get year

            foreach (var year in yearList)
            {
                var _actualDataList = GetActualDataList(forecastData);
                var _movingAverageData = CalculateMovingAverage(_actualDataList, 5);
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

        private List<double?> GetActualDataList(YearData forecastData)
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

        //here we will create a logic to get the data per year
        private List<double?> GetActualDataForYear(int year)
        {
            // This method would return the actual data for the given year
            // For now, we'll use dummy data
            return new List<double?> { 1000, 1200, 1300, 1400, 1600, 1800, 1500, 1700, 1400, 2800, 3100 };
        }

        private void ProcessYearData(List<double?> actualData, List<double?> movingAverage, List<double?> percentChange, List<string> insights)
        {
            for (int i = 0; i < actualData.Count; i++)
            {
                double? actual = actualData[i];
                if (movingAverage[i].HasValue)
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
