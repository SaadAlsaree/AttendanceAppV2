using Application.Abstractions.Messaging;

namespace Application.Organizations.WorkLocations.Update;

public sealed class UpdateWorkLocationCommand : ICommand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? WifiSSID { get; set; }
    public string? BeaconId { get; set; }
}
