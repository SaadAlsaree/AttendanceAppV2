using Domain.Enums;

namespace Application.Devices.Get;

public sealed class DeviceResponse
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Location { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? IsupKey { get; set; }
    public string? Port { get; set; }
    public string? Protocol { get; set; }
    public string? DeviceModel { get; set; }
    public string? SerialNumber { get; set; }
    public string? MacAddress { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? Department { get; set; }
    public string? Features { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastConnected { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? WorkLocationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation properties
    public string? OrganizationName { get; set; }
    public string? WorkLocationName { get; set; }

}
