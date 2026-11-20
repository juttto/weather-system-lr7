using WeatherSystem.Domain.Enums;

namespace WeatherSystem.Services.DTOs;

/// <summary>
/// Модель запиту на створення пристрою (POST). Правила перевірки — у CreateDeviceRequestValidator.
/// </summary>
public sealed class CreateDeviceRequest
{
    public string Name { get; set; } = string.Empty;

    public DeviceType Type { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }
}
