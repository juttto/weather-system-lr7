using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using WeatherSystem.Services.DTOs;
using WeatherSystem.Services.Interfaces;

namespace WeatherSystem.API.Controllers;

/// <summary>
/// Контролер для роботи з пристроями системи моніторингу погоди.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IValidator<CreateDeviceRequest> _createValidator;
    private readonly IValidator<UpdateDeviceRequest> _updateValidator;
    private readonly IValidator<SetDeviceStatusRequest> _statusValidator;

    /// <summary>
    /// Створює контролер і отримує сервіс пристроїв та валідатори через DI.
    /// </summary>
    public DevicesController(
        IDeviceService deviceService,
        IValidator<CreateDeviceRequest> createValidator,
        IValidator<UpdateDeviceRequest> updateValidator,
        IValidator<SetDeviceStatusRequest> statusValidator)
    {
        _deviceService = deviceService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Повертає список пристроїв, за потреби відфільтрований за назвою.
    /// </summary>
    /// <param name="name">Частина назви пристрою (необов'язково), наприклад «датчик».</param>
    /// <returns>Список пристроїв (200 OK).</returns>
    /// <response code="200">Список пристроїв (може бути порожнім).</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetAll(
        [FromQuery] string? name)
    {
        var devices = await _deviceService.SearchDevicesAsync(name ?? string.Empty);
        return Ok(devices);
    }

    /// <summary>
    /// Повертає пристрій за його ідентифікатором.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор пристрою (GUID).</param>
    /// <returns>Пристрій (200 OK) або 404 Not Found, якщо його не знайдено.</returns>
    /// <response code="200">Пристрій знайдено.</response>
    /// <response code="404">Пристрій із таким Id не знайдено.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> GetById(Guid id)
    {
        var device = await _deviceService.GetDeviceDetailsAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        return Ok(device);
    }

    /// <summary>
    /// Реєструє новий пристрій у системі.
    /// </summary>
    /// <param name="request">Назва, тип і (необов'язково) температура та вологість.</param>
    /// <returns>Створений пристрій (201 Created) або 400 Bad Request при некоректних даних.</returns>
    /// <response code="201">Пристрій створено; заголовок Location містить адресу ресурсу.</response>
    /// <response code="400">Помилка валідації даних запиту.</response>
    [HttpPost]
    public async Task<ActionResult<DeviceDto>> Create(
        [FromBody] CreateDeviceRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationError(validation);
        }

        var newDevice = await _deviceService.CreateDeviceAsync(request);
        return CreatedAtAction(
            nameof(GetById),
            new { id = newDevice.Id },
            newDevice);
    }

    /// <summary>
    /// Повністю оновлює дані пристрою (назву, тип, температуру, вологість).
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор пристрою (GUID).</param>
    /// <param name="request">Новий повний набір даних пристрою.</param>
    /// <returns>Оновлений пристрій (200 OK) або 404 Not Found.</returns>
    /// <response code="200">Пристрій оновлено.</response>
    /// <response code="400">Помилка валідації даних запиту.</response>
    /// <response code="404">Пристрій із таким Id не знайдено.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DeviceDto>> Update(
        Guid id,
        [FromBody] UpdateDeviceRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationError(validation);
        }

        var updatedDevice = await _deviceService.UpdateDeviceAsync(id, request);
        if (updatedDevice is null)
        {
            return NotFound();
        }

        return Ok(updatedDevice);
    }

    /// <summary>
    /// Оновлює статус роботи пристрою (активує або деактивує його).
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор пристрою (GUID).</param>
    /// <param name="request">Об'єкт з новим статусом (IsActive).</param>
    /// <returns>Повертає 204 No Content у разі успіху або 404 Not Found.</returns>
    /// <response code="204">Стан пристрою змінено.</response>
    /// <response code="400">Не вказано isActive.</response>
    /// <response code="404">Пристрій із таким Id не знайдено.</response>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetDeviceStatusRequest request)
    {
        var validation = await _statusValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationError(validation);
        }

        var result = await _deviceService.SetDeviceStateAsync(
            id,
            request.IsActive!.Value);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Видаляє пристрій із системи.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор пристрою (GUID).</param>
    /// <returns>Повертає 204 No Content у разі успіху або 404 Not Found.</returns>
    /// <response code="204">Пристрій видалено.</response>
    /// <response code="404">Пристрій із таким Id не знайдено.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _deviceService.DeleteDeviceAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Перетворює помилки FluentValidation на відповідь 400 Bad Request (Problem Details)
    private ActionResult ValidationError(ValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }
}
