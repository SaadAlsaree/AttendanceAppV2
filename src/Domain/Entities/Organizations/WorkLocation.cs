using Domain.Common;

namespace Domain.Entities.Organizations;

public sealed class WorkLocation : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? WifiSSID { get; set; } // For additional location verification
    public string? BeaconId { get; set; } // For indoor positioning

    // Navigation Properties
    public OrganizationalUnit Organization { get; set; } = null!;

    // Business Logic Methods (Pure calculations are OK to keep)
    public bool IsWithinRadius(double checkLatitude, double checkLongitude)
    {
        // Simple distance calculation using Haversine formula
        const double earthRadius = 6371000; // meters

        double lat1Rad = Latitude * Math.PI / 180;
        double lat2Rad = checkLatitude * Math.PI / 180;
        double deltaLat = (checkLatitude - Latitude) * Math.PI / 180;
        double deltaLon = (checkLongitude - Longitude) * Math.PI / 180;

        double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = earthRadius * c;

        return distance <= RadiusMeters;
    }
}
