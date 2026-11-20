using FluentValidation;
using WeatherSystem.Services.DTOs;

namespace WeatherSystem.API.Validators;

/// <summary>
/// Правила перевірки запиту на створення пристрою.
/// </summary>
public sealed class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Назва пристрою є обов'язковою.")
            .MaximumLength(100).WithMessage("Назва пристрою не може перевищувати 100 символів.");

        RuleFor(request => request.Type)
            .IsInEnum().WithMessage("Вказано недопустимий тип пристрою.");

        RuleFor(request => request.Temperature)
            .InclusiveBetween(-50m, 100m)
            .When(request => request.Temperature.HasValue)
            .WithMessage("Температура має бути в діапазоні від -50 до 100.");

        RuleFor(request => request.Humidity)
            .InclusiveBetween(0m, 100m)
            .When(request => request.Humidity.HasValue)
            .WithMessage("Вологість має бути в діапазоні від 0 до 100.");
    }
}
