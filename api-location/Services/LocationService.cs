using System.Text.Json;
using LocationApi.Models;

namespace LocationApi.Services;

public class LocationService(
    IHttpClientFactory httpFactory,
    CacheService cache,
    MessagingService messaging,
    PersistenceService persistence)
{
    public async Task<LocationResult> GetLocationAsync(string? ip)
    {
        ip ??= "8.8.8.8";
        var cacheKey = $"location:{ip}";
        var cached = await cache.GetAsync(cacheKey);
        if (cached != null)
            return JsonSerializer.Deserialize<LocationResult>(cached)!;

        var result = await FetchLocationAsync(ip);

        await cache.SetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromHours(24));
        await messaging.PublishAsync("location-events", "location.queried", new { ip, result.City, result.Country });
        await persistence.SaveLocationQueryAsync(result);
        await persistence.LogTransactionAsync("api-location", "/location", "GET", 200);

        return result;
    }

    private async Task<LocationResult> FetchLocationAsync(string ip)
    {
        try
        {
            var http = httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(5);
            var url = $"http://ip-api.com/json/{ip}?fields=status,city,regionName,country,lat,lon,timezone,query";
            var resp = await http.GetStringAsync(url);
            var doc = JsonDocument.Parse(resp).RootElement;

            if (doc.GetProperty("status").GetString() == "success")
            {
                return new LocationResult
                {
                    Ip = ip,
                    City = doc.GetProperty("city").GetString() ?? "",
                    Region = doc.GetProperty("regionName").GetString() ?? "",
                    Country = doc.GetProperty("country").GetString() ?? "",
                    Latitude = doc.GetProperty("lat").GetDouble(),
                    Longitude = doc.GetProperty("lon").GetDouble(),
                    Timezone = doc.GetProperty("timezone").GetString() ?? "",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        catch { /* fallback */ }

        return new LocationResult
        {
            Ip = ip,
            City = "São Paulo",
            Region = "São Paulo",
            Country = "Brazil",
            Latitude = -23.5505,
            Longitude = -46.6333,
            Timezone = "America/Sao_Paulo",
            Timestamp = DateTime.UtcNow
        };
    }
}
