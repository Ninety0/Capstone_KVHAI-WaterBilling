using KVHAI.Models;

namespace KVHAI.CustomClass
{
    public class Forecasting
    {
        private readonly DBConnect _dbConnect;

        public Forecasting(DBConnect dBConnect)
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
        public List<double?> CalculateMovingAverage(List<double> dataList, int period)
        {
            List<double?> movingAverage = new List<double?>();

            for (int i = 0; i < dataList.Count; i++)
            {
                if (i + 1 >= period)
                {
                    // Calculate average for the last 'period' values
                    double average = dataList.Skip(i + 1 - period).Take(period).Average();
                    movingAverage.Add(average);
                }
                else
                {
                    // Not enough data for moving average, append null
                    movingAverage.Add(null);
                }
            }

            return movingAverage;
        }

        // Get percentage change and insights
        public async Task<Models.Forecasting> GetPercentChange()
        {
            var forecast = new KVHAI.Models.Forecasting();
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

        //here we will create a logic to get the data per year
        private List<double> GetActualDataForYear(int year)
        {
            // This method would return the actual data for the given year
            // For now, we'll use dummy data
            return year == 2024
                ? new List<double> { 1000, 1200, 1300, 1400, 1600, 1800, 1500, 1700, 1400, 2800, 3100, 3500 }
                : new List<double> { 3600, 3800, 4000, 4200, 4400, 4600, 4800, 5000, 5200, 5400, 5600, 5800 };
        }

        private void ProcessYearData(List<double> actualData, List<double?> movingAverage, List<double?> percentChange, List<string> insights)
        {
            for (int i = 0; i < actualData.Count; i++)
            {
                double actual = actualData[i];
                if (movingAverage[i].HasValue)
                {
                    double moveAvg = movingAverage[i].Value;
                    double percent = PercentChangeFormula(actual, moveAvg);
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
        private double PercentChangeFormula(double actualData, double forecastData)
        {
            return ((actualData - forecastData) / forecastData) * 100;
        }

        // Generate insight based on percentage change
        public string GenerateForecastInsights(double percentChange)
        {
            if (percentChange >= -5 && percentChange <= 5)
            {
                return "Stable: Water consumption is within normal limits.";
            }
            else if (percentChange > 5 && percentChange <= 25)
            {
                return "Moderate Increase: Water consumption is slightly higher than usual";
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
