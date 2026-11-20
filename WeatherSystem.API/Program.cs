using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WeatherSystem.API.Middleware;
using WeatherSystem.API.Validators;
using WeatherSystem.Domain.Entities;
using WeatherSystem.Domain.Enums;
using WeatherSystem.Domain.Interfaces;
using WeatherSystem.Infrastructure.Data;
using WeatherSystem.Infrastructure.Repositories;
using WeatherSystem.Services.Interfaces;
using WeatherSystem.Services.Services;

var builder = WebApplication.CreateBuilder(args);

// ЛР7: поза Development журнал пишеться одним рядком на подію з часом —
// так його зручно читати на вкладці Logs у Render
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.UseUtcTimestamp = true;
    });
}

// ЛР7: рядок підключення до PostgreSQL береться ТІЛЬКИ з середовища запуску —
// на Render це змінна ConnectionStrings__DefaultConnection. У коді й appsettings.json пароля немає.
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConnectionString))
{
    throw new InvalidOperationException(
        "Не задано рядок підключення: додайте змінну середовища ConnectionStrings__DefaultConnection.");
}

// Render дає адресу postgres://..., Npgsql потрібен формат Host=...;Database=...
var connectionString = DatabaseUrl.ToConnectionString(rawConnectionString);

builder.Services.AddDbContext<WeatherDbContext>(options =>
    options.UseNpgsql(connectionString)
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// ЛР7: CORS — дозволяємо запити з домену вебсторінки (клієнтської частини)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

// Постійне сховище замість InMemoryDeviceRepository.
// DeviceService і контролер при цьому не змінюються.
builder.Services.AddScoped<IDeviceRepository, EfDeviceRepository>();
// builder.Services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

// ЛР5 (входження 2): валідатори FluentValidation з цього проєкту
builder.Services.AddValidatorsFromAssemblyContaining<CreateDeviceRequestValidator>();

// ЛР5 (входження 5): перевірка стану API і бази даних
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WeatherDbContext>("database", tags: new[] { "ready" });

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// База даних: застосовуємо міграції і додаємо початкові пристрої, якщо таблиця порожня
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    try
    {
        app.Logger.LogInformation("Підключення до бази даних {Database}",
            DatabaseUrl.SafeDescription(connectionString));

        var pending = db.Database.GetPendingMigrations().ToList();
        db.Database.Migrate();
        if (pending.Count > 0)
        {
            app.Logger.LogInformation("Міграції EF Core застосовано: {Migrations}",
                string.Join(", ", pending));
        }
        else
        {
            app.Logger.LogInformation("Схема бази даних актуальна, нових міграцій немає");
        }

        if (!db.Devices.Any())
        {
            var thermometer = new WeatherDevice("Термометр Івано-Франківськ", DeviceType.Sensor, temperature: 4.5m);
            thermometer.Activate();
            var humiditySensor = new WeatherDevice("Датчик вологості Львів", DeviceType.Sensor, humidity: 78m);
            humiditySensor.Activate();

            db.Devices.AddRange(
                thermometer,
                humiditySensor,
                new WeatherDevice("Датчик швидкості вітру Київ", DeviceType.Sensor),
                new WeatherDevice("Сирена штормового попередження", DeviceType.Actuator),
                new WeatherDevice("Обігрів датчика вітру", DeviceType.Actuator));
            db.SaveChanges();
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogCritical(exception, "Не вдалося підготувати базу даних");
        throw;
    }
}

// Централізована обробка помилок — першою в конвеєрі
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("Frontend");

// Swagger доступний і в Production — для перевірки API після розгортання
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// /health — чи працює сам застосунок (без перевірок залежностей)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse
});

// /health/ready — чи готовий приймати запити (перевіряється база даних)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

// ЛР7: інформація про базу — підтвердження, що схема створена та актуальна
app.MapGet("/api/info/database", async (WeatherDbContext db) =>
{
    var applied = await db.Database.GetAppliedMigrationsAsync();
    var pending = await db.Database.GetPendingMigrationsAsync();
    var columns = await db.Database.SqlQuery<ColumnInfo>(
        $"""
        SELECT column_name AS "Name", data_type AS "Type", is_nullable AS "Nullable"
        FROM information_schema.columns
        WHERE table_name = 'Devices'
        ORDER BY ordinal_position
        """).ToListAsync();

    return Results.Ok(new
    {
        provider = "PostgreSQL",
        appliedMigrations = applied,
        pendingMigrations = pending,
        table = "Devices",
        rows = await db.Devices.CountAsync(),
        columns
    });
}).WithTags("Info");

// Тестовий маршрут для перевірки централізованої обробки помилок (повертає 500)
app.MapGet("/api/test/error", () =>
{
    throw new InvalidOperationException("Тестова помилка для перевірки middleware.");
}).WithTags("Test");

app.Logger.LogInformation(
    "WeatherSystem.API запущено. Середовище: {Environment}",
    app.Environment.EnvironmentName);

app.Run();

// Коротка відповідь health-check у JSON: лише стан, без технічних подробиць і секретів
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString()
        })
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(result));
}

// Рядок з information_schema.columns для /api/info/database
internal sealed class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Nullable { get; set; } = string.Empty;
}
