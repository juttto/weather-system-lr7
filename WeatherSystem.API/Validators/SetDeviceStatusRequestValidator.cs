using FluentValidation;
using WeatherSystem.Services.DTOs;

namespace WeatherSystem.API.Validators;

/// <summary>
/// Правила перевірки запиту на зміну стану: у доменній моделі пристрій або ввімкнений, або вимкнений,
/// тому значення isActive (true / false) обов'язкове.
/// </summary>
public sealed class SetDeviceStatusRequestValidator : AbstractValidator<SetDeviceStatusRequest>
{
    public SetDeviceStatusRequestValidator()
    {
        RuleFor(request => request.IsActive)
            .NotNull().WithMessage("Стан пристрою (isActive: true або false) є обов'язковим.");
    }
}
