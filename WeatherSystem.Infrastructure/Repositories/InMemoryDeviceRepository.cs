using WeatherSystem.Domain.Entities;
using WeatherSystem.Domain.Interfaces;

namespace WeatherSystem.Infrastructure.Repositories;

/// <summary>
/// Тимчасове сховище пристроїв у пам'яті на основі List.
/// </summary>
public class InMemoryDeviceRepository : IDeviceRepository
{
    private readonly List<WeatherDevice> _devices = new();

    public Task<WeatherDevice?> GetByIdAsync(Guid id)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        return Task.FromResult(device);
    }

    public Task<IEnumerable<WeatherDevice>> GetAllAsync()
    {
        IEnumerable<WeatherDevice> devices = _devices.ToList();
        return Task.FromResult(devices);
    }

    public Task AddAsync(WeatherDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (_devices.Any(d => d.Id == device.Id))
        {
            throw new InvalidOperationException(
                $"Пристрій із Id '{device.Id}' вже існує.");
        }

        _devices.Add(device);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WeatherDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var index = _devices.FindIndex(d => d.Id == device.Id);
        if (index >= 0)
        {
            _devices[index] = device;
        }

        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var device = _devices.FirstOrDefault(d => d.Id == id);
        if (device is null)
        {
            return Task.FromResult(false);
        }

        _devices.Remove(device);
        return Task.FromResult(true);
    }
}
