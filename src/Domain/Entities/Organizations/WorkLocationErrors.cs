using SharedKernel;

namespace Domain.Entities.Organizations;

public static class WorkLocationErrors
{
    public static Error NotFound(Guid workLocationId) => Error.NotFound(
        "WorkLocation.NotFound",
        $"The work location with Id = '{workLocationId}' was not found");

    public static Error DuplicateName(string name) => Error.Conflict(
        "WorkLocation.DuplicateName",
        $"A work location with name = '{name}' already exists");

    public static Error InvalidCoordinates(double latitude, double longitude) => Error.Problem(
        "WorkLocation.InvalidCoordinates",
        $"Invalid coordinates: latitude = '{latitude}', longitude = '{longitude}'");

    public static Error InvalidRadius(int radius) => Error.Problem(
        "WorkLocation.InvalidRadius",
        $"Invalid radius: '{radius}' meters. Radius must be greater than 0");

    public static Error LocationOutOfRange(double latitude, double longitude, string locationName) => Error.Problem(
        "WorkLocation.LocationOutOfRange",
        $"Location ({latitude}, {longitude}) is outside the allowed range for '{locationName}'");

    public static Error InactiveLocation(Guid workLocationId) => Error.Problem(
        "WorkLocation.InactiveLocation",
        $"The work location with Id = '{workLocationId}' is inactive");
}
