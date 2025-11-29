using FluentValidation;

namespace Application.Devices.Update;

public class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(c => c.DeviceId).NotEmpty();

        RuleFor(c => c.Username)
            .MaximumLength(100)
            .When(c => !string.IsNullOrEmpty(c.Username))
            .WithMessage("Device name cannot exceed 100 characters");

        RuleFor(c => c.IpAddress)
            .Must(BeValidIpAddress)
            .When(c => !string.IsNullOrEmpty(c.IpAddress))
            .WithMessage("Valid IP address is required");

        RuleFor(c => c.Location)
            .MaximumLength(200)
            .When(c => !string.IsNullOrEmpty(c.Location))
            .WithMessage("Location cannot exceed 200 characters");

        RuleFor(c => c.DeviceModel)
            .MaximumLength(100)
            .When(c => !string.IsNullOrEmpty(c.DeviceModel))
            .WithMessage("Device model cannot exceed 100 characters");


    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        return !string.IsNullOrEmpty(ipAddress) && System.Net.IPAddress.TryParse(ipAddress, out _);
    }


}
