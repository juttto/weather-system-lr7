using Microsoft.EntityFrameworkCore;
using WeatherSystem.Domain.Entities;
using WeatherSystem.Domain.Interfaces;
using WeatherSystem.Infrastructure.Data;

namespace WeatherSystem.Infrastructure.Repositories;

/// <summary>
/// Репозиторій з постійним зберіганням: пристрої записуються в базу даних SQLite через EF Core.
/// </summary>
public class EfDeviceRepository : IDeviceRepository
{
    private readonly WeatherDbContext _context;

    public EfDeviceRepository(WeatherDbContext context)
    {
        _context = context;
    }

    public async Task<WeatherDevice?> GetByIdAsync(Guid id)
    {
        return await _context.Devices.FindAsync(id);
    }

    public async Task<IEnumerable<WeatherDevice>> GetAllAsync()
    {
        return await _context.Devices
            .AsNoTracking()
            .OrderBy(device => device.Name)
            .ToListAsync();
    }

    public async Task AddAsync(WeatherDevice device)
    {
        await _context.Devices.AddAsync(device);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WeatherDevice device)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device is null)
        {
            return false;
        }

        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
        return true;
    }
}
