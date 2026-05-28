namespace PersonApi.Models;

public class PersonRequest
{
    public string Name { get; set; } = "";
    public string Birthdate { get; set; } = "";
}

public class PersonResult
{
    public string Name { get; set; } = "";
    public string Birthdate { get; set; } = "";
    public int AgeYears { get; set; }
    public int AgeMonths { get; set; }
    public int AgeDays { get; set; }
    public string ZodiacSign { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
