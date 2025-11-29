using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Devices.Get;

internal sealed class GetDevicesQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetDevicesQuery, PaginatedResponse<DeviceResponse>>
{
    public async Task<Result<PaginatedResponse<DeviceResponse>>> Handle(GetDevicesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Device> devicesQuery = context.Devices
            .Include(d => d.Organization)
            .AsNoTracking();

        // Apply filters



        // Apply search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm.ToUpperInvariant();
            devicesQuery = devicesQuery.Where(d =>
                d.Username != null && d.Username.ToUpperInvariant().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                d.IpAddress != null && d.IpAddress.ToUpperInvariant().Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }


        // Get total count
        int totalCount = await devicesQuery.CountAsync(cancellationToken);

        // Apply pagination
        List<DeviceResponse> devices = await devicesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DeviceResponse
            {
                Id = d.Id,
                Username = d.Username,
                Password = d.Password,
                Location = d.Location,
                IpAddress = d.IpAddress,
                DeviceId = d.DeviceId,
                IsupKey = d.IsupKey,
                Port = d.Port,
                Protocol = d.Protocol,
                DeviceModel = d.DeviceModel,
                SerialNumber = d.SerialNumber,
                MacAddress = d.MacAddress,
                FirmwareVersion = d.FirmwareVersion,
                Department = d.Department,
                Features = d.Features,
                IsActive = d.IsActive,
                LastConnected = d.LastConnected,
                OrganizationId = d.OrganizationId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.LastUpdatedAt ?? DateTime.UtcNow,
                OrganizationName = d.Organization != null ? d.Organization.UnitName : null,
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<DeviceResponse>.Create(
            devices,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
