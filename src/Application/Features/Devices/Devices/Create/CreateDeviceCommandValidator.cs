using FluentValidation;

namespace Application.Devices.Create;

public class CreateDeviceCommandValidator : AbstractValidator<CreateDeviceCommand>
{
    public CreateDeviceCommandValidator()
    {
        RuleFor(c => c.Username)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Username is required and cannot exceed 100 characters");

        RuleFor(c => c.Password)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Password is required and cannot exceed 100 characters");

        RuleFor(c => c.IpAddress)
            .NotEmpty()
            .Must(BeValidIpAddress)
            .WithMessage("Valid IP address is required");



        RuleFor(c => c.Port)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Port is required and cannot exceed 50 characters");

        RuleFor(c => c.Location)
            .MaximumLength(200)
            .When(c => !string.IsNullOrEmpty(c.Location))
            .WithMessage("Location cannot exceed 200 characters");

        RuleFor(c => c.Protocol)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Protocol is required and cannot exceed 50 characters");

        RuleFor(c => c.DeviceModel)
        .NotEmpty()
        .MaximumLength(100)
        .WithMessage("Device model is required and cannot exceed 100 characters");

        RuleFor(c => c.SerialNumber)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Serial number is required and cannot exceed 50 characters");
    }

    private static bool BeValidIpAddress(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }


}
