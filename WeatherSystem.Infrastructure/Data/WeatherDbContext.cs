using Microsoft.EntityFrameworkCore;
using WeatherSystem.Domain.Entities;

namespace WeatherSystem.Infrastructure.Data;

/// <summary>
/// Контекст бази даних PostgreSQL: через нього EF Core читає та записує пристрої.
/// Структура таблиці Devices описана через Fluent API в OnModelCreating.
/// </summary>
public sealed class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
        : base(options)
    {
    }

    /// <summary>Таблиця Devices з пристроями.</summary>
    public DbSet<WeatherDevice> Devices => Set<WeatherDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WeatherDevice>(entity =>
        {
            // Назва таблиці та обмеження CHECK — перевірка даних на рівні самої бази
            entity.ToTable("Devices", table =>
            {
                table.HasCheckConstraint("CK_Devices_Name_NotEmpty",
                    "length(trim(\"Name\")) > 0");
                table.HasCheckConstraint("CK_Devices_Temperature_Range",
                    "\"Temperature\" IS NULL OR \"Temperature\" BETWEEN -50 AND 100");
                table.HasCheckConstraint("CK_Devices_Humidity_Range",
                    "\"Humidity\" IS NULL OR \"Humidity\" BETWEEN 0 AND 100");
            });

            entity.HasKey(device => device.Id);

            entity.Property(device => device.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(device => device.Type)
                .IsRequired();

            entity.Property(device => device.IsEnabled)
                .HasDefaultValue(false);

            entity.Property(device => device.Temperature)
                .HasPrecision(5, 2);

            entity.Property(device => device.Humidity)
                .HasPrecision(5, 2);

            // PostgreSQL зберігає час як timestamp with time zone (UTC)
            entity.Property(device => device.LastUpdated)
                .IsRequired();
        });
    }
}
