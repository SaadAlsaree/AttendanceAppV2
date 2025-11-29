using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Application.Abstractions.Data;
using Application.Models;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services;

internal sealed class HikvisionService : IHikvisionService
{
    private readonly HttpClient _httpClient;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<HikvisionService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HikvisionService(
        HttpClient httpClient,
        IApplicationDbContext context,
        ILogger<HikvisionService> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<HikvisionResponse<AccessLogSearchResult>> GetTodayEventsAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            List<Device> devices = await _context.Devices
                .Where(d => d.IsActive)
                .ToListAsync();

            if (!devices.Any())
            {
                return new HikvisionResponse<AccessLogSearchResult>
                {
                    Success = false,
                    Message = "لا توجد أجهزة نشطة",
                    Data = null
                };
            }

            // جلب الأحداث من جميع الأجهزة بشكل متوازي
            var tasks = devices.Select(device => GetDeviceEventsAsync(device, startTime, endTime)).ToList();
            AccessLogSearchResult?[] results = await Task.WhenAll(tasks);

            // دمج النتائج من جميع الأجهزة
            var allEvents = new List<AccessLogInfo>();
            int successfulDevices = 0;

            foreach (AccessLogSearchResult result in results)
            {
                if (result?.AcsEvent?.InfoList != null)
                {
                    allEvents.AddRange(result.AcsEvent.InfoList);
                    successfulDevices++;
                }
            }

            // ترتيب الأحداث حسب الوقت
            allEvents = allEvents.OrderByDescending(e => e.Time).ToList();

            return new HikvisionResponse<AccessLogSearchResult>
            {
                Success = true,
                Message = $"تم جلب {allEvents.Count} حدث من {successfulDevices}/{devices.Count} جهاز",
                Data = new AccessLogSearchResult
                {
                    AcsEvent = new AccessLogSearch
                    {
                        SearchID = Guid.NewGuid().ToString(),
                        ResponseStatusStrg = "OK",
                        NumOfMatches = allEvents.Count,
                        TotalMatches = allEvents.Count,
                        InfoList = allEvents
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب أحداث اليوم");
            return new HikvisionResponse<AccessLogSearchResult>
            {
                Success = false,
                Message = $"خطأ: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// جلب الأحداث من جهاز واحد
    /// </summary>
    private async Task<AccessLogSearchResult?> GetDeviceEventsAsync(Device device, DateTime startTime, DateTime endTime)
    {
        try
        {
            // التحقق من بيانات الجهاز
            if (string.IsNullOrEmpty(device.IpAddress) || string.IsNullOrEmpty(device.Username) || string.IsNullOrEmpty(device.Password))
            {
                _logger.LogWarning("بيانات غير مكتملة للجهاز {DeviceId}", device.SerialNumber ?? device.DeviceId);
                return null;
            }

            string url = $"{device.Protocol}://{device.IpAddress}:{device.Port}/ISAPI/AccessControl/AcsEvent?format=json";

            var searchData = new
            {
                AcsEventCond = new
                {
                    searchID = Guid.NewGuid().ToString(),
                    searchResultPosition = 0,
                    maxResults = 8000,
                    major = 5,
                    minor = 75,
                    startTime = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                    endTime = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                }
            };

            string jsonContent = JsonSerializer.Serialize(searchData);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            bool authSuccess = await SetDigestAuthenticationAsync(request, "POST", "/ISAPI/AccessControl/AcsEvent", device.Username, device.Password);
            if (!authSuccess)
            {
                _logger.LogWarning("فشل في المصادقة للجهاز {DeviceId}", device.SerialNumber ?? device.DeviceId);
                return null;
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(content))
            {
                AccessLogSearchResult? accessLogResult = JsonSerializer.Deserialize<AccessLogSearchResult>(content, _jsonOptions);

                _logger.LogInformation("تم جلب {EventCount} حدث من الجهاز {DeviceId}",
                    accessLogResult?.AcsEvent?.InfoList?.Count ?? 0,
                    device.SerialNumber ?? device.DeviceId);

                return accessLogResult;
            }
            else
            {
                _logger.LogWarning("فشل في جلب الأحداث من الجهاز {DeviceId}: {StatusCode}",
                    device.SerialNumber ?? device.DeviceId, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في جلب الأحداث من الجهاز {DeviceId}", device.SerialNumber ?? device.DeviceId);
            return null;
        }
    }

    public async Task<HikvisionResponse<DeviceStatus>> TestConnectionAsync(Guid deviceId)
    {
        try
        {
            Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
            if (device is null)
            {
                return new HikvisionResponse<DeviceStatus>
                {
                    Success = false,
                    Message = "جهاز غير موجود",
                    Data = null
                };
            }

            DeviceStatus deviceStatus = await TestDeviceConnectionAsync(device);
            return new HikvisionResponse<DeviceStatus>
            {
                Success = deviceStatus.IsOnline,
                Message = deviceStatus.IsOnline ? "تم الاتصال بالجهاز بنجاح" : "فشل في الاتصال بالجهاز",
                Data = deviceStatus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في الاتصال بالجهاز");
            return new HikvisionResponse<DeviceStatus>
            {
                Success = false,
                Message = $"خطأ في الاتصال: {ex.Message}",
                Data = null
            };
        }
    }

    public async Task<HikvisionResponse<List<DeviceStatus>>> TestAllDevicesConnectionAsync()
    {
        try
        {
            List<Device> devices = await _context.Devices
                .Where(d => d.IsActive)
                .ToListAsync();

            if (!devices.Any())
            {
                return new HikvisionResponse<List<DeviceStatus>>
                {
                    Success = false,
                    Message = "لا توجد أجهزة نشطة في قاعدة البيانات",
                    Data = new List<DeviceStatus>()
                };
            }

            var tasks = devices.Select(TestDeviceConnectionAsync).ToList();
            DeviceStatus[] results = await Task.WhenAll(tasks);

            int onlineDevices = results.Count(d => d.IsOnline);
            int totalDevices = results.Length;

            return new HikvisionResponse<List<DeviceStatus>>
            {
                Success = true,
                Message = $"تم فحص {totalDevices} جهاز. {onlineDevices} جهاز متصل و {totalDevices - onlineDevices} جهاز غير متصل",
                Data = results.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في فحص الأجهزة");
            return new HikvisionResponse<List<DeviceStatus>>
            {
                Success = false,
                Message = $"خطأ في فحص الأجهزة: {ex.Message}",
                Data = new List<DeviceStatus>()
            };
        }
    }

    private async Task<DeviceStatus> TestDeviceConnectionAsync(Device device)
    {
        try
        {
            // التحقق من البيانات المطلوبة
            if (string.IsNullOrEmpty(device.IpAddress) || string.IsNullOrEmpty(device.Port) || string.IsNullOrEmpty(device.Protocol))
            {
                return CreateDeviceStatus(device, "بيانات غير مكتملة", false, "بيانات الجهاز غير مكتملة (IP, Port, Protocol)");
            }

            if (string.IsNullOrEmpty(device.Username) || string.IsNullOrEmpty(device.Password))
            {
                return CreateDeviceStatus(device, "بيانات المصادقة غير مكتملة", false, "بيانات المصادقة غير مكتملة (Username, Password)");
            }

            // إنشاء URL للجهاز
            string url = $"{device.Protocol}://{device.IpAddress}:{device.Port}/ISAPI/System/deviceInfo";

            // محاولة الاتصال مع المصادقة
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            _logger.LogInformation("إنشاء طلب للجهاز {DeviceId} على {Url}", device.SerialNumber ?? device.DeviceId, url);

            // إعداد المصادقة
            bool authSuccess = await SetDigestAuthenticationAsync(request, "GET", "/ISAPI/System/deviceInfo", device.Username, device.Password);
            if (!authSuccess)
            {
                _logger.LogWarning("فشل في إعداد Digest Authentication للجهاز {DeviceId}", device.SerialNumber ?? device.DeviceId);
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("تم الاتصال بالجهاز {DeviceId} بنجاح", device.SerialNumber ?? device.DeviceId);
                return CreateDeviceStatus(device, "متصل", true, content);
            }
            else
            {
                _logger.LogWarning("فشل في الاتصال بالجهاز {DeviceId}: {StatusCode}", device.SerialNumber ?? device.DeviceId, response.StatusCode);
                return CreateDeviceStatus(device, "فشل في المصادقة", false, $"فشل في المصادقة: {response.StatusCode} - {content}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في الاتصال بالجهاز {DeviceId}", device.SerialNumber ?? device.DeviceId);
            return CreateDeviceStatus(device, "خطأ في الاتصال", false, $"خطأ: {ex.Message}");
        }
    }

    /// <summary>
    /// إعداد مصادقة Digest Authentication
    /// </summary>
    private async Task<bool> SetDigestAuthenticationAsync(HttpRequestMessage request, string method, string uri, string username, string password)
    {
        try
        {
            // أولاً نحصل على تحدي المصادقة
            using var challengeRequest = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
            HttpResponseMessage challengeResponse = await _httpClient.SendAsync(challengeRequest);

            if (challengeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                System.Net.Http.Headers.AuthenticationHeaderValue? wwwAuthenticate = challengeResponse.Headers.WwwAuthenticate.FirstOrDefault();
                if (wwwAuthenticate is not null && wwwAuthenticate.Scheme == "Digest")
                {
                    string digestHeader = CreateDigestAuthHeader(wwwAuthenticate.Parameter, method, uri, username, password);
                    if (!string.IsNullOrEmpty(digestHeader))
                    {
                        request.Headers.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(digestHeader);
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ في إعداد Digest Authentication");
            return false;
        }
    }

    /// <summary>
    /// إنشاء رأس مصادقة Digest
    /// </summary>
    private string CreateDigestAuthHeader(string? challengeParameter, string method, string uri, string username, string password)
    {
        if (string.IsNullOrEmpty(challengeParameter))
        {
            return string.Empty;
        }

        Dictionary<string, string> challenge = ParseDigestChallenge(challengeParameter);

        string ha1 = ComputeMD5Hash($"{username}:{challenge.GetValueOrDefault("realm", "")}:{password}");
        string ha2 = ComputeMD5Hash($"{method}:{uri}");
        string response = ComputeMD5Hash($"{ha1}:{challenge.GetValueOrDefault("nonce", "")}:{ha2}");

        return $"Digest username=\"{username}\", " +
               $"realm=\"{challenge.GetValueOrDefault("realm", "")}\", " +
               $"nonce=\"{challenge.GetValueOrDefault("nonce", "")}\", " +
               $"uri=\"{uri}\", " +
               $"response=\"{response}\"";
    }



    private Dictionary<string, string> ParseDigestChallenge(string challenge)
    {
        var result = new Dictionary<string, string>();
        string[] parts = challenge.Split(',');

        foreach (string part in parts)
        {
            string[] keyValue = part.Trim().Split('=', 2);
            if (keyValue.Length == 2)
            {
                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim().Trim('"');
                result[key] = value;
            }
        }

        return result;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:Do not use broken cryptographic algorithms", Justification = "MD5 required for Hikvision device compatibility")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Hikvision devices require lowercase MD5 hash")]
    private string ComputeMD5Hash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private DeviceStatus CreateDeviceStatus(Device device, string status, bool isOnline, string deviceInfo)
    {
        DeviceInfo? parsedInfo = null;
        if (isOnline && !string.IsNullOrEmpty(deviceInfo))
        {
            parsedInfo = ParseDeviceInfoXml(deviceInfo);
        }

        return new DeviceStatus
        {
            DeviceID = device.SerialNumber ?? device.DeviceId ?? device.Id.ToString(),
            Status = status,
            LastSeen = _dateTimeProvider.GetUtcNow(),
            IsOnline = isOnline,
            CurrentTime = _dateTimeProvider.GetUtcNow().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DeviceInfo = deviceInfo,
            ParsedDeviceInfo = parsedInfo,
            Location = device.Location,
            DeviceModel = device.DeviceModel,
            IpAddress = device.IpAddress,
            Port = device.Port,
            Protocol = device.Protocol
        };
    }

    /// <summary>
    /// تحليل XML معلومات الجهاز
    /// </summary>
    private DeviceInfo? ParseDeviceInfoXml(string xmlContent)
    {
        try
        {
            if (string.IsNullOrEmpty(xmlContent))
            {
                return null;
            }

            var doc = XDocument.Parse(xmlContent);
            XElement? deviceInfoElement = doc.Element(XName.Get("DeviceInfo", "http://www.isapi.org/ver20/XMLSchema"));

            if (deviceInfoElement is null)
            {
                return null;
            }

            return new DeviceInfo
            {
                DeviceName = GetElementValue(deviceInfoElement, "deviceName"),
                DeviceID = GetElementValue(deviceInfoElement, "deviceID"),
                Model = GetElementValue(deviceInfoElement, "model"),
                SerialNumber = GetElementValue(deviceInfoElement, "serialNumber"),
                MacAddress = GetElementValue(deviceInfoElement, "macAddress"),
                FirmwareVersion = GetElementValue(deviceInfoElement, "firmwareVersion"),
                FirmwareReleasedDate = GetElementValue(deviceInfoElement, "firmwareReleasedDate"),
                EncoderVersion = GetElementValue(deviceInfoElement, "encoderVersion"),
                EncoderReleasedDate = GetElementValue(deviceInfoElement, "encoderReleasedDate"),
                HardwareVersion = GetElementValue(deviceInfoElement, "hardwareVersion"),
                DeviceType = GetElementValue(deviceInfoElement, "deviceType"),
                SubDeviceType = GetElementValue(deviceInfoElement, "subDeviceType"),
                LocalZoneNum = GetElementIntValue(deviceInfoElement, "localZoneNum"),
                AlarmOutNum = GetElementIntValue(deviceInfoElement, "alarmOutNum"),
                RelayNum = GetElementIntValue(deviceInfoElement, "relayNum"),
                ElectroLockNum = GetElementIntValue(deviceInfoElement, "electroLockNum"),
                RS485Num = GetElementIntValue(deviceInfoElement, "RS485Num"),
                Manufacturer = GetElementValue(deviceInfoElement, "manufacturer"),
                OEMCode = GetElementValue(deviceInfoElement, "OEMCode"),
                MarketType = GetElementValue(deviceInfoElement, "marketType"),
                DisplayNum = GetElementIntValue(deviceInfoElement, "dispalyNum"), // ملاحظة: هناك خطأ إملائي في XML من الجهاز
                BspVersion = GetElementValue(deviceInfoElement, "bspVersion"),
                DspVersion = GetElementValue(deviceInfoElement, "dspVersion"),
                ProductionDate = GetElementValue(deviceInfoElement, "productionDate")
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "فشل في تحليل XML معلومات الجهاز");
            return null;
        }
    }

    /// <summary>
    /// الحصول على قيمة عنصر XML
    /// </summary>
    private static string GetElementValue(XElement parent, string elementName)
    {
        return parent.Element(XName.Get(elementName, "http://www.isapi.org/ver20/XMLSchema"))?.Value ?? string.Empty;
    }

    /// <summary>
    /// الحصول على قيمة عدد صحيح من عنصر XML
    /// </summary>
    private static int GetElementIntValue(XElement parent, string elementName)
    {
        string value = GetElementValue(parent, elementName);
        return int.TryParse(value, out int result) ? result : 0;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
