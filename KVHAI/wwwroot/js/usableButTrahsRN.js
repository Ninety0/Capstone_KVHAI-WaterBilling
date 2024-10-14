function chartDataSets(yearlyData) {
    var datasets = [];
    var insights = [];

    Object.keys(yearlyData).forEach(function (year) {
        var yearData = yearlyData[year];

        // Create dataset for actual data
        datasets.push({
            label: `Cubic Consumption ${year}`,
            data: yearData.actualData,
            borderWidth: 2,
            fill: false,
            tension: 0.1,
            borderColor: getRandomColor(), // Use different colors for each year
        });

        // Create dataset for forecast data
        datasets.push({
            label: `Forecast Data ${year}`,
            data: yearData.forecastData,
            borderWidth: 2,
            fill: false,
            tension: 0.1,
            borderDash: [5, 5], // Dashed line for forecast
            borderColor: getRandomColor(),
        });

        // Combine insights for all years
        insights = insights.concat(yearData.insights || []);
    });

    // Pass datasets and insights to SetChartJs to display them
    SetChartJs(datasets, insights);
}