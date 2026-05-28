using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using PersonApi.Models;

namespace PersonApi.Services;

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
                id TEXT PRIMARY KEY, service TEXT NOT NULL,
                endpoint TEXT NOT NULL, method TEXT NOT NULL,
                status INTEGER NOT NULL, duration_ms INTEGER, created_at TEXT NOT NULL)
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task SavePersonQueryAsync(PersonResult result)
    {
        var col = _db.GetCollection<PersonResult>("person_queries");
        await col.InsertOneAsync(result);
    }

    public async Task LogTransactionAsync(string service, string endpoint, string method, int status, long ms = 0)
    {
        await Task.Run(() =>
        {
            using var conn = new SqliteConnection($"Data Source={_sqlitePath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO transactions VALUES ($id,$svc,$ep,$m,$s,$d,$ts)";
            cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            cmd.Parameters.AddWithValue("$svc", service);
            cmd.Parameters.AddWithValue("$ep", endpoint);
            cmd.Parameters.AddWithValue("$m", method);
            cmd.Parameters.AddWithValue("$s", status);
            cmd.Parameters.AddWithValue("$d", ms);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        });
    }
}
