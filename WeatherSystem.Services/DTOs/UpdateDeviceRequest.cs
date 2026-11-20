using WeatherSystem.Domain.Enums;

namespace WeatherSystem.Services.DTOs;

/// <summary>
/// Модель запиту на повне оновлення пристрою (PUT). Правила перевірки — у UpdateDeviceRequestValidator.
/// </summary>
public sealed class UpdateDeviceRequest
{
    public string Name { get; set; } = string.Empty;

    public DeviceType Type { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }
}
