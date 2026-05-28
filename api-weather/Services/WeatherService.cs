using System.Text.Json;
using WeatherApi.Models;

namespace WeatherApi.Services;

public class WeatherService(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    CacheService cache,
    MessagingService messaging,
    PersistenceService persistence)
{
    private readonly string _owmKey = config["OPENWEATHER_API_KEY"] ?? "";
    private readonly string _waKey = config["WEATHER_API_KEY"] ?? "";

    public async Task<WeatherResult> GetWeatherAsync(string city)
    {
        var cacheKey = $"weather:{city.ToLower()}";
        var cached = await cache.GetAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<WeatherResult>(cached)!;

        var result = await FetchFromApiAsync(city);

        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(10));
        await messaging.PublishAsync("climate-events", "weather.queried", new { city, result.Temperature, result.Humidity });
        await persistence.SaveWeatherQueryAsync(result);
        await persistence.LogTransactionAsync("api-weather", "/weather", "GET", 200);

        return result;
    }

    public async Task<ForecastResult> GetForecastAsync(string city, int days)
    {
        var cacheKey = $"forecast:{city.ToLower()}:{days}";
        var cached = await cache.GetAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<ForecastResult>(cached)!;

        var result = await FetchForecastFromApiAsync(city, days);

        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(30));
        await messaging.PublishAsync("climate-events", "weather.forecast.queried", new { city, days });
        await persistence.LogTransactionAsync("api-weather", "/forecast", "GET", 200);

        return result;
    }

    private async Task<WeatherResult> FetchFromApiAsync(string city)
    {
        if (!string.IsNullOrEmpty(_owmKey))
        {
            try
            {
                var http = httpFactory.CreateClient();
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={Uri.EscapeDataString(city)}&appid={_owmKey}&units=metric&lang=pt_br";
                var resp = await http.GetStringAsync(url);
                var doc = JsonDocument.Parse(resp).RootElement;
                return new WeatherResult
                {
                    City = city,
                    Temperature = doc.GetProperty("main").GetProperty("temp").GetDouble(),
                    FeelsLike = doc.GetProperty("main").GetProperty("feels_like").GetDouble(),
                    Humidity = doc.GetProperty("main").GetProperty("humidity").GetInt32(),
                    WindSpeed = doc.GetProperty("wind").GetProperty("speed").GetDouble(),
                    Description = doc.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                    Source = "openweathermap",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch { /* fallback */ }
        }

        return new WeatherResult
        {
            City = city,
            Temperature = Random.Shared.NextDouble() * 30 + 5,
            FeelsLike = Random.Shared.NextDouble() * 30 + 3,
            Humidity = Random.Shared.Next(30, 95),
            WindSpeed = Random.Shared.NextDouble() * 20,
            Description = "simulado (sem chave de API)",
            Source = "mock",
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<ForecastResult> FetchForecastFromApiAsync(string city, int days)
    {
        await Task.CompletedTask;
        var forecasts = Enumerable.Range(0, days).Select(i => new DayForecast
        {
            Date = DateTime.UtcNow.AddDays(i + 1).ToString("yyyy-MM-dd"),
            TempMin = Math.Round(Random.Shared.NextDouble() * 20 + 5, 1),
            TempMax = Math.Round(Random.Shared.NextDouble() * 20 + 20, 1),
            Description = "previsão simulada"
        }).ToList();

        return new ForecastResult { City = city, Days = days, Forecast = forecasts };
    }
}
