namespace LocationApi.Models;

public class LocationResult
{
    public string Ip { get; set; } = "";
    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public string Country { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
