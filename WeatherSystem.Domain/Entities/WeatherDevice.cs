using WeatherSystem.Domain.Enums;

namespace WeatherSystem.Domain.Entities;

/// <summary>
/// Доменна сутність — пристрій системи моніторингу погоди.
/// </summary>
public class WeatherDevice
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    public DeviceType Type { get; private set; }

    public bool IsEnabled { get; private set; }

    /// <summary>Останнє виміряне значення температури, °C (може бути відсутнім).</summary>
    public decimal? Temperature { get; private set; }

    /// <summary>Останнє виміряне значення вологості, % (може бути відсутнім).</summary>
    public decimal? Humidity { get; private set; }

    /// <summary>Дата й час останньої зміни даних пристрою (UTC).</summary>
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    // Порожній конструктор потрібен Entity Framework Core для читання з бази даних
    private WeatherDevice()
    {
    }

    public WeatherDevice(string name, DeviceType type,
        decimal? temperature = null, decimal? humidity = null)
    {
        Update(name, type, temperature, humidity);
        IsEnabled = false;
    }

    public void Activate()
    {
        IsEnabled = true;
        LastUpdated = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsEnabled = false;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Повне оновлення даних пристрою (використовується для PUT).
    /// </summary>
    public void Update(string name, DeviceType type,
        decimal? temperature, decimal? humidity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Назва пристрою не може бути порожньою.",
                nameof(name));
        }

        Name = name;
        Type = type;
        Temperature = temperature;
        Humidity = humidity;
        LastUpdated = DateTime.UtcNow;
    }
}
