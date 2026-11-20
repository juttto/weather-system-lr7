namespace WeatherSystem.Services.DTOs;

/// <summary>
/// Дані про пристрій, що передаються в UI / API.
/// </summary>
public class DeviceDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }

    public DateTime LastUpdated { get; set; }
}
