using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherSystem.Domain.Enums;
using WeatherSystem.Services.DTOs;

namespace WeatherSystem.Desktop;

/// <summary>
/// Звертається до REST API (api/devices) тими самими запитами, що й Swagger.
/// </summary>
public class DevicesApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public DevicesApiClient(HttpClient http)
    {
        _http = http;
    }

    // GET api/devices?name=...
    public async Task<List<DeviceDto>> GetDevicesAsync(string? name)
    {
        string url = string.IsNullOrWhiteSpace(name)
            ? "api/devices"
            : $"api/devices?name={Uri.EscapeDataString(name.Trim())}";
        return await _http.GetFromJsonAsync<List<DeviceDto>>(url, Json) ?? new();
    }

    // POST api/devices -> 201 або 400 з повідомленнями валідації
    public async Task<(DeviceDto? Device, string? Error)> CreateAsync(
        string name, DeviceType type, decimal? temperature, decimal? humidity)
    {
        var request = new CreateDeviceRequest
        {
            Name = name,
            Type = type,
            Temperature = temperature,
            Humidity = humidity
        };
        using var response = await _http.PostAsJsonAsync("api/devices", request, Json);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return (null, await ReadValidationErrorsAsync(response));
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeviceDto>(Json), null);
    }

    // PATCH api/devices/{id}/status -> 204 або 404
    public async Task<bool> SetStatusAsync(Guid id, bool isActive)
    {
        var request = new SetDeviceStatusRequest { IsActive = isActive };
        using var response = await _http.PatchAsJsonAsync($"api/devices/{id}/status", request, Json);
        return response.StatusCode == HttpStatusCode.NoContent;
    }

    // DELETE api/devices/{id} -> 204 або 404
    public async Task<bool> DeleteAsync(Guid id)
    {
        using var response = await _http.DeleteAsync($"api/devices/{id}");
        return response.StatusCode == HttpStatusCode.NoContent;
    }

    // GET health/ready -> "Healthy" / "Unhealthy" (перевірка API і бази даних)
    public async Task<string> GetHealthAsync()
    {
        using var response = await _http.GetAsync("health/ready");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("status").GetString() ?? "Unknown";
    }

    // Дістає тексти помилок з відповіді 400 ("errors": { "Name": ["..."] })
    private static async Task<string> ReadValidationErrorsAsync(HttpResponseMessage response)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()))
                    .Where(m => !string.IsNullOrEmpty(m))
                    .Distinct();
                return string.Join(" ", messages);
            }
        }
        catch (JsonException) { }
        return "Сервер не прийняв дані (400).";
    }
}
