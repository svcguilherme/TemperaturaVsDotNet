using PersonApi.Models;

namespace PersonApi.Services;

public class PersonService(MessagingService messaging, PersistenceService persistence)
{
    public async Task<PersonResult> CalculateAsync(PersonRequest req)
    {
        var birthdate = DateOnly.Parse(req.Birthdate);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var years = today.Year - birthdate.Year;
        if (today < birthdate.AddYears(years)) years--;

        var lastBirthday = birthdate.AddYears(years);
        var months = 0;
        while (lastBirthday.AddMonths(months + 1) <= today) months++;

        var lastMonthDay = lastBirthday.AddMonths(months);
        var days = today.DayNumber - lastMonthDay.DayNumber;

        var result = new PersonResult
        {
            Name = req.Name,
            Birthdate = req.Birthdate,
            AgeYears = years,
            AgeMonths = months,
            AgeDays = days,
            ZodiacSign = GetZodiacSign(birthdate.Month, birthdate.Day),
            Timestamp = DateTime.UtcNow
        };

        await messaging.PublishAsync("person-events", "person.queried", new { req.Name, req.Birthdate, result.AgeYears });
        await persistence.SavePersonQueryAsync(result);
        await persistence.LogTransactionAsync("api-person", "/person", "POST", 200);

        return result;
    }

    private static string GetZodiacSign(int month, int day) => (month, day) switch
    {
        (3, >= 21) or (4, <= 19) => "Áries",
        (4, >= 20) or (5, <= 20) => "Touro",
        (5, >= 21) or (6, <= 20) => "Gêmeos",
        (6, >= 21) or (7, <= 22) => "Câncer",
        (7, >= 23) or (8, <= 22) => "Leão",
        (8, >= 23) or (9, <= 22) => "Virgem",
        (9, >= 23) or (10, <= 22) => "Libra",
        (10, >= 23) or (11, <= 21) => "Escorpião",
        (11, >= 22) or (12, <= 21) => "Sagitário",
        (12, >= 22) or (1, <= 19) => "Capricórnio",
        (1, >= 20) or (2, <= 18) => "Aquário",
        _ => "Peixes"
    };
}
