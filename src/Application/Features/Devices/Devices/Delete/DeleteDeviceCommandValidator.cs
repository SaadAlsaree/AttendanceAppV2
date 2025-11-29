using FluentValidation;

namespace Application.Devices.Delete;

public class DeleteDeviceCommandValidator : AbstractValidator<DeleteDeviceCommand>
{
    public DeleteDeviceCommandValidator()
    {
        RuleFor(c => c.DeviceId).NotEmpty();
    }
}
