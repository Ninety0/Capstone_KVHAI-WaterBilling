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
            var actualDataList = new List<double>
            {
                1000, 1200, 1300, 1400, 1600, 1800, 1500, 1700, 1400, 2800, 3100, 3500
            };

            var movingAverage = CalculateMovingAverage(actualDataList, 5);

            var insights = new List<string>();
            var percentChange = new List<double?>();

            for (int i = 0; i < actualDataList.Count; i++)
            {
                double actual = actualDataList[i];

                if (movingAverage[i].HasValue) // Check if moving average exists for this index
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

            forecast = new Models.Forecasting
            {
                Actual_Data = actualDataList,
                Moving_Average = movingAverage,
                Percent_Change = percentChange,
                Insights = insights
            };

            return forecast;
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
