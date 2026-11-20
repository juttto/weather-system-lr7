namespace WeatherSystem.Domain.Enums;

/// <summary>
/// Тип пристрою системи моніторингу погоди.
/// </summary>
public enum DeviceType
{
    /// <summary>Датчик — пристрій, що збирає погодні дані.</summary>
    Sensor,

    /// <summary>Виконавчий пристрій — виконує дію за командою системи.</summary>
    Actuator
}
