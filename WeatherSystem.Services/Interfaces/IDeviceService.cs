using WeatherSystem.Services.DTOs;

namespace WeatherSystem.Services.Interfaces;

/// <summary>
/// Контракт сервісного шару для роботи з пристроями.
/// </summary>
public interface IDeviceService
{
    Task<DeviceDto?> GetDeviceDetailsAsync(Guid id);

    Task<IReadOnlyList<DeviceDto>> GetAllDevicesAsync();

    Task<IReadOnlyList<DeviceDto>> SearchDevicesAsync(string query);

    Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request);

    Task<DeviceDto?> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request);

    Task<bool> SetDeviceStateAsync(Guid id, bool isActive);

    Task<bool> DeleteDeviceAsync(Guid id);
}
