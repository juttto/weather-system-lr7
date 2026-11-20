using Microsoft.Extensions.Logging;
using WeatherSystem.Domain.Entities;
using WeatherSystem.Domain.Interfaces;
using WeatherSystem.Services.DTOs;
using WeatherSystem.Services.Interfaces;

namespace WeatherSystem.Services.Services;

/// <summary>
/// Сервіс пристроїв: отримує сутності через IDeviceRepository і повертає DTO.
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _repository;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(IDeviceRepository repository, ILogger<DeviceService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        _logger = logger;
    }

    public async Task<DeviceDto?> GetDeviceDetailsAsync(Guid id)
    {
        var device = await _repository.GetByIdAsync(id);
        if (device is null)
        {
            return null;
        }

        return MapToDto(device);
    }

    public async Task<IReadOnlyList<DeviceDto>> GetAllDevicesAsync()
    {
        var devices = await _repository.GetAllAsync();
        return devices
            .Select(MapToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<DeviceDto>> SearchDevicesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllDevicesAsync();
        }

        var devices = await _repository.GetAllAsync();
        var normalizedQuery = query.Trim();

        return devices
            .Where(device => device.Name.Contains(
                normalizedQuery,
                StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto)
            .ToList();
    }

    public async Task<DeviceDto> CreateDeviceAsync(CreateDeviceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var device = new WeatherDevice(request.Name, request.Type,
            request.Temperature, request.Humidity);
        await _repository.AddAsync(device);
        _logger.LogInformation("Створено пристрій {DeviceId} «{DeviceName}»", device.Id, device.Name);

        return MapToDto(device);
    }

    public async Task<DeviceDto?> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var device = await _repository.GetByIdAsync(id);
        if (device is null)
        {
            return null;
        }

        device.Update(request.Name, request.Type, request.Temperature, request.Humidity);
        await _repository.UpdateAsync(device);

        return MapToDto(device);
    }

    public async Task<bool> SetDeviceStateAsync(Guid id, bool isActive)
    {
        var device = await _repository.GetByIdAsync(id);
        if (device is null)
        {
            return false;
        }

        if (isActive)
        {
            device.Activate();
        }
        else
        {
            device.Deactivate();
        }

        await _repository.UpdateAsync(device);
        return true;
    }

    public async Task<bool> DeleteDeviceAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Видалено пристрій {DeviceId}", id);
        }

        return deleted;
    }

    private static DeviceDto MapToDto(WeatherDevice device)
    {
        return new DeviceDto
        {
            Id = device.Id,
            Name = device.Name,
            Type = device.Type.ToString(),
            IsEnabled = device.IsEnabled,
            Temperature = device.Temperature,
            Humidity = device.Humidity,
            LastUpdated = device.LastUpdated
        };
    }
}
