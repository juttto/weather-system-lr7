using WeatherSystem.Domain.Entities;

namespace WeatherSystem.Domain.Interfaces;

/// <summary>
/// Контракт сховища пристроїв. Описує операції, але не спосіб зберігання.
/// </summary>
public interface IDeviceRepository
{
    Task<WeatherDevice?> GetByIdAsync(Guid id);

    Task<IEnumerable<WeatherDevice>> GetAllAsync();

    Task AddAsync(WeatherDevice device);

    Task UpdateAsync(WeatherDevice device);

    Task<bool> DeleteAsync(Guid id);
}
