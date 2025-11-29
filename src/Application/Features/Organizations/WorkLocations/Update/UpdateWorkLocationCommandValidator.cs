using FluentValidation;

namespace Application.Organizations.WorkLocations.Update;

public class UpdateWorkLocationCommandValidator : AbstractValidator<UpdateWorkLocationCommand>
{
    public UpdateWorkLocationCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Address).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Latitude).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitude).InclusiveBetween(-180, 180);
        RuleFor(c => c.RadiusMeters).InclusiveBetween(1, 10000);
        RuleFor(c => c.Description).MaximumLength(1000).When(c => c.Description is not null);
        RuleFor(c => c.WifiSSID).MaximumLength(100).When(c => c.WifiSSID is not null);
        RuleFor(c => c.BeaconId).MaximumLength(100).When(c => c.BeaconId is not null);
    }
}
