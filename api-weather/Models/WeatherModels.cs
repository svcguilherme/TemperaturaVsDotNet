namespace WeatherApi.Models;

public class WeatherResult
{
    public string City { get; set; } = "";
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public string Description { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ForecastResult
{
    public string City { get; set; } = "";
    public int Days { get; set; }
    public List<DayForecast> Forecast { get; set; } = [];
}

public class DayForecast
{
    public string Date { get; set; } = "";
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public string Description { get; set; } = "";
}
