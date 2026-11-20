namespace WeatherSystem.Services.DTOs;

/// <summary>
/// Модель запиту на зміну стану пристрою (PATCH). Правила перевірки — у SetDeviceStatusRequestValidator.
/// </summary>
public sealed class SetDeviceStatusRequest
{
    /// <summary>true — увімкнути, false — вимкнути.</summary>
    public bool? IsActive { get; set; }
}
