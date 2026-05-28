using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using WeatherApi.Models;

namespace WeatherApi.Services;

public class PersistenceService(IMongoClient mongo, IConfiguration config)
{
    private readonly string _sqlitePath = config["SQLITE_PATH"] ?? "./data/transactions.db";
    private readonly IMongoDatabase _db = mongo.GetDatabase("temperaturadb");

    public void InitializeDatabase()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_sqlitePath)!);
        using var conn = new SqliteConnection($"Data Source={_sqlitePath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS transactions (
                id TEXT PRIMARY KEY,
                service TEXT NOT NULL,
                endpoint TEXT NOT NULL,
                method TEXT NOT NULL,
                status INTEGER NOT NULL,
                duration_ms INTEGER,
                created_at TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task SaveWeatherQueryAsync(WeatherResult result)
    {
        var col = _db.GetCollection<WeatherResult>("weather_queries");
        await col.InsertOneAsync(result);
    }

    public async Task LogTransactionAsync(string service, string endpoint, string method, int status, long durationMs = 0)
    {
        await Task.Run(() =>
        {
            using var conn = new SqliteConnection($"Data Source={_sqlitePath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO transactions (id, service, endpoint, method, status, duration_ms, created_at)
                VALUES ($id, $service, $endpoint, $method, $status, $dur, $ts)
                """;
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$service", service);
            cmd.Parameters.AddWithValue("$endpoint", endpoint);
            cmd.Parameters.AddWithValue("$method", method);
            cmd.Parameters.AddWithValue("$status", status);
            cmd.Parameters.AddWithValue("$dur", durationMs);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        });
    }
}
