using MongoDB.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using PersonApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var mongoUri = builder.Configuration["MONGODB_URI"] ?? "mongodb://localhost:27017/temperaturadb";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUri));

builder.Services.AddSingleton<MessagingService>();
builder.Services.AddSingleton<PersistenceService>();
builder.Services.AddScoped<PersonService>();

var otelEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4318";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("api-person"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(otelEndpoint)));

var app = builder.Build();

app.Services.GetRequiredService<PersistenceService>().InitializeDatabase();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseRouting();
app.UseHttpMetrics();
app.MapControllers();
app.MapMetrics();

app.Run();
