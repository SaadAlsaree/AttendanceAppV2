using Application.Models;

namespace Application.Abstractions.Data;
public interface IHikvisionService : IDisposable
{
    Task<HikvisionResponse<DeviceStatus>> TestConnectionAsync(Guid deviceId);
    Task<HikvisionResponse<AccessLogSearchResult>> GetTodayEventsAsync(DateTime startTime, DateTime endTime);
    Task<HikvisionResponse<List<DeviceStatus>>> TestAllDevicesConnectionAsync();
}
