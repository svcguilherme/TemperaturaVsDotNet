using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrdersApi.Data;
using OrdersApi.Services;
using OrdersApi.Workers;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var sqlitePath = builder.Configuration["SQLITE_PATH"] ?? "./data/orders.db";
Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath)!);
builder.Services.AddDbContext<OrdersDbContext>(o =>
    o.UseSqlite($"Data Source={sqlitePath}"));

builder.Services.AddSingleton<ServiceBusService>();
builder.Services.AddSingleton<EventHubService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddHostedService<OrderProcessorWorker>();

var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4318";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("api-orders"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint)));

var app = builder.Build();

// Criar schema e seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.EnsureCreatedAsync();
    await OrderSeeder.SeedAsync(db, 10_000);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseRouting();
app.UseHttpMetrics();
app.MapControllers();
app.MapMetrics();

app.Run();
