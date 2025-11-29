using SharedKernel;

namespace Domain.Entities.Devices;

public static class DeviceErrors
{
    public static Error NotFound(Guid deviceId) => Error.NotFound(
        "Device.NotFound",
        $"The device with Id = '{deviceId}' was not found");

    public static Error NotFound(string isupKey) => Error.NotFound(
        "Device.NotFound",
        $"The device with serial number = '{isupKey}' was not found");

    public static Error DuplicateSerialNumber(string serialNumber) => Error.Conflict(
        "Device.DuplicateSerialNumber",
        $"A device with serial number = '{serialNumber}' already exists");

    public static Error DuplicateIPAddress(string ipAddress) => Error.Conflict(
        "Device.DuplicateIPAddress",
        $"A device with IP address = '{ipAddress}' already exists");

    public static Error DeviceOffline(Guid deviceId) => Error.Problem(
        "Device.DeviceOffline",
        $"The device with Id = '{deviceId}' is offline");

    public static Error InvalidIpAddress(string ipAddress) => Error.Problem(
        "Device.InvalidIpAddress",
        $"Invalid IP address: '{ipAddress}'");

    public static Error BiometricNotSupported(Guid deviceId) => Error.Problem(
        "Device.BiometricNotSupported",
        $"The device with Id = '{deviceId}' does not support biometric authentication");

    public static Error DeviceNotSynced(Guid deviceId, DateTime lastSync) => Error.Problem(
        "Device.DeviceNotSynced",
        $"The device with Id = '{deviceId}' has not synced since '{lastSync}'");
}
