using System.Text.Json.Serialization;

namespace Application.Models;


/// <summary>
/// نموذج لحدث التعرف على الوجه من جهاز Hikvision
/// </summary>
public class AccessControlEvent
{
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("portNo")]
    public int PortNo { get; set; }

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("channelID")]
    public int ChannelID { get; set; }

    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("activePostCount")]
    public int ActivePostCount { get; set; }

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("eventState")]
    public string EventState { get; set; } = string.Empty;

    [JsonPropertyName("eventDescription")]
    public string EventDescription { get; set; } = string.Empty;

    [JsonPropertyName("deviceID")]
    public string DeviceID { get; set; } = string.Empty;

    [JsonPropertyName("deviceDescription")]
    public string DeviceDescription { get; set; } = string.Empty;

    [JsonPropertyName("AccessControllerEvent")]
    public AccessControllerEventDetails? AccessControllerEvent { get; set; }
}

/// <summary>
/// تفاصيل حدث التحكم في الوصول
/// </summary>
public class AccessControllerEventDetails
{
    [JsonPropertyName("employeeNoString")]
    public string EmployeeNoString { get; set; } = string.Empty;

    [JsonPropertyName("userType")]
    public string UserType { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("userID")]
    public string UserID { get; set; } = string.Empty;

    [JsonPropertyName("doorName")]
    public string DoorName { get; set; } = string.Empty;

    [JsonPropertyName("doorNo")]
    public int DoorNo { get; set; }

    [JsonPropertyName("cardNo")]
    public string CardNo { get; set; } = string.Empty;

    [JsonPropertyName("cardType")]
    public int CardType { get; set; }

    [JsonPropertyName("whiteListNo")]
    public int WhiteListNo { get; set; }

    [JsonPropertyName("reportChannel")]
    public int ReportChannel { get; set; }

    [JsonPropertyName("cardReaderKind")]
    public int CardReaderKind { get; set; }

    [JsonPropertyName("cardReaderNo")]
    public int CardReaderNo { get; set; }

    [JsonPropertyName("accessChannel")]
    public int AccessChannel { get; set; }

    [JsonPropertyName("deviceNo")]
    public string DeviceNo { get; set; } = string.Empty;

    [JsonPropertyName("pictureURL")]
    public string PictureURL { get; set; } = string.Empty;

    [JsonPropertyName("serialNo")]
    public string SerialNo { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("localUIRight")]
    public bool LocalUIRight { get; set; }

    [JsonPropertyName("currentVerifyMode")]
    public string CurrentVerifyMode { get; set; } = string.Empty;

    [JsonPropertyName("QRCodeInfo")]
    public QRCodeInfo? QRCodeInfo { get; set; }

    [JsonPropertyName("attendanceStatus")]
    public string AttendanceStatus { get; set; } = string.Empty;

    [JsonPropertyName("mask")]
    public string Mask { get; set; } = string.Empty;

    [JsonPropertyName("purePwdVerifyEnable")]
    public bool PurePwdVerifyEnable { get; set; }

    [JsonPropertyName("thermalTemperature")]
    public string ThermalTemperature { get; set; } = string.Empty;

    [JsonPropertyName("IsTemperatureNormal")]
    public bool IsTemperatureNormal { get; set; }

    [JsonPropertyName("temperatureUnit")]
    public string TemperatureUnit { get; set; } = string.Empty;

    [JsonPropertyName("belongGroup")]
    public string BelongGroup { get; set; } = string.Empty;

    [JsonPropertyName("personInfo")]
    public PersonInfo? PersonInfo { get; set; }
}

/// <summary>
/// معلومات QR Code
/// </summary>
public class QRCodeInfo
{
    [JsonPropertyName("QRCodeData")]
    public string QRCodeData { get; set; } = string.Empty;

    [JsonPropertyName("QRCodePictureURL")]
    public string QRCodePictureURL { get; set; } = string.Empty;
}

/// <summary>
/// معلومات الشخص
/// </summary>
public class PersonInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("birthday")]
    public string Birthday { get; set; } = string.Empty;

    [JsonPropertyName("phoneNo")]
    public string PhoneNo { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;
}

/// <summary>
/// إعدادات الاتصال بجهاز Hikvision
/// </summary>
public class HikvisionDeviceConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = "192.168.25.250";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "Saad1987";
    public string Protocol { get; set; } = "http";
    public int Port { get; set; } = 80;
    public string DeviceModel { get; set; } = "DS-K1T673DWX";
    public string SerialNumber { get; set; } = "DS-K1T673DWX20250703V044101ENFQ0198725";
    public string MacAddress { get; set; } = "A4:D5:C2:0D:EE:16";
    public string FirmwareVersion { get; set; } = "V4.41.1";
    public string Location { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastConnected { get; set; }
    public string ConnectionStatus { get; set; } = "غير محدد";
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// مجموعة الأجهزة
/// </summary>
public class DeviceCollection
{
    public List<HikvisionDeviceConfig> Devices { get; set; } = new();
}

/// <summary>
/// معلومات حالة جهاز مع التفاصيل الإضافية
/// </summary>
public class DeviceStatusInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string ConnectionStatus { get; set; } = string.Empty;
    public DateTime? LastConnected { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// استجابة من جهاز Hikvision
/// </summary>
public class HikvisionResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// معلومات الجهاز المحللة من XML
/// </summary>
public class DeviceInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceID { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string FirmwareReleasedDate { get; set; } = string.Empty;
    public string EncoderVersion { get; set; } = string.Empty;
    public string EncoderReleasedDate { get; set; } = string.Empty;
    public string HardwareVersion { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string SubDeviceType { get; set; } = string.Empty;
    public int LocalZoneNum { get; set; }
    public int AlarmOutNum { get; set; }
    public int RelayNum { get; set; }
    public int ElectroLockNum { get; set; }
    public int RS485Num { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string OEMCode { get; set; } = string.Empty;
    public string MarketType { get; set; } = string.Empty;
    public int DisplayNum { get; set; }
    public string BspVersion { get; set; } = string.Empty;
    public string DspVersion { get; set; } = string.Empty;
    public string ProductionDate { get; set; } = string.Empty;
}

/// <summary>
/// معلومات حالة الجهاز
/// </summary>
public class DeviceStatus
{
    public string DeviceID { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public string CurrentTime { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DeviceInfo? ParsedDeviceInfo { get; set; }
    public string? Location { get; set; }
    public string? DeviceModel { get; set; }
    public string? IpAddress { get; set; }
    public string? Port { get; set; }
    public string? Protocol { get; set; }
}

/// <summary>
/// نتيجة البحث عن المستخدمين
/// </summary>
public class UserInfoSearchResult
{
    [JsonPropertyName("UserInfoSearch")]
    public UserInfoSearch UserInfoSearch { get; set; } = new();
}

/// <summary>
/// بيانات البحث عن المستخدمين
/// </summary>
public class UserInfoSearch
{
    [JsonPropertyName("searchID")]
    public string SearchID { get; set; } = string.Empty;

    [JsonPropertyName("responseStatusStrg")]
    public string ResponseStatusStrg { get; set; } = string.Empty;

    [JsonPropertyName("numOfMatches")]
    public int NumOfMatches { get; set; }

    [JsonPropertyName("totalMatches")]
    public int TotalMatches { get; set; }

    [JsonPropertyName("UserInfo")]
    public List<UserInfo> UserInfo { get; set; } = new();
}

/// <summary>
/// معلومات المستخدم
/// </summary>
public class UserInfo
{
    [JsonPropertyName("employeeNo")]
    public string EmployeeNo { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("userType")]
    public string UserType { get; set; } = string.Empty;

    [JsonPropertyName("onlyVerify")]
    public bool OnlyVerify { get; set; }

    [JsonPropertyName("closeDelayEnabled")]
    public bool CloseDelayEnabled { get; set; }

    [JsonPropertyName("Valid")]
    public UserValidPeriod Valid { get; set; } = new();

    [JsonPropertyName("belongGroup")]
    public string BelongGroup { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("doorRight")]
    public string DoorRight { get; set; } = string.Empty;

    [JsonPropertyName("RightPlan")]
    public List<UserRightPlan> RightPlan { get; set; } = new();

    [JsonPropertyName("maxOpenDoorTime")]
    public int MaxOpenDoorTime { get; set; }

    [JsonPropertyName("openDoorTime")]
    public int OpenDoorTime { get; set; }

    [JsonPropertyName("roomNumber")]
    public int RoomNumber { get; set; }

    [JsonPropertyName("floorNumber")]
    public int FloorNumber { get; set; }

    [JsonPropertyName("localUIRight")]
    public bool LocalUIRight { get; set; }

    [JsonPropertyName("linkageUserID")]
    public int LinkageUserID { get; set; }

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("numOfCard")]
    public int NumOfCard { get; set; }

    [JsonPropertyName("numOfRemoteControl")]
    public int NumOfRemoteControl { get; set; }

    [JsonPropertyName("numOfFP")]
    public int NumOfFP { get; set; }

    [JsonPropertyName("numOfFace")]
    public int NumOfFace { get; set; }

    [JsonPropertyName("numOfPPAndPV")]
    public int NumOfPPAndPV { get; set; }

    [JsonPropertyName("PersonInfoExtends")]
    public List<PersonInfoExtend> PersonInfoExtends { get; set; } = new();

    [JsonPropertyName("faceURL")]
    public string FaceURL { get; set; } = string.Empty;
}

/// <summary>
/// فترة صلاحية المستخدم
/// </summary>
public class UserValidPeriod
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("beginTime")]
    public string BeginTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("timeType")]
    public string TimeType { get; set; } = string.Empty;
}

/// <summary>
/// خطة صلاحيات المستخدم
/// </summary>
public class UserRightPlan
{
    [JsonPropertyName("doorNo")]
    public int DoorNo { get; set; }

    [JsonPropertyName("planTemplateNo")]
    public string PlanTemplateNo { get; set; } = string.Empty;
}

/// <summary>
/// معلومات إضافية للشخص
/// </summary>
public class PersonInfoExtend
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// نتيجة البحث عن سجلات الوصول
/// </summary>
public class AccessLogSearchResult
{
    [JsonPropertyName("AcsEvent")]
    public AccessLogSearch AcsEvent { get; set; } = new();
}

/// <summary>
/// بيانات البحث عن سجلات الوصول
/// </summary>
public class AccessLogSearch
{
    [JsonPropertyName("searchID")]
    public string SearchID { get; set; } = string.Empty;

    [JsonPropertyName("totalMatches")]
    public int TotalMatches { get; set; }

    [JsonPropertyName("responseStatusStrg")]
    public string ResponseStatusStrg { get; set; } = string.Empty;

    [JsonPropertyName("numOfMatches")]
    public int NumOfMatches { get; set; }

    [JsonPropertyName("InfoList")]
    public List<AccessLogInfo> InfoList { get; set; } = new();
}

/// <summary>
/// معلومات سجل الوصول
/// </summary>
public class AccessLogInfo
{
    [JsonPropertyName("major")]
    public int Major { get; set; }

    [JsonPropertyName("minor")]
    public int Minor { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("cardNo")]
    public string CardNo { get; set; } = string.Empty;

    [JsonPropertyName("cardType")]
    public int CardType { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cardReaderNo")]
    public int CardReaderNo { get; set; }

    [JsonPropertyName("doorNo")]
    public int DoorNo { get; set; }

    [JsonPropertyName("employeeNoString")]
    public string EmployeeNoString { get; set; } = string.Empty;

    [JsonPropertyName("serialNo")]
    public int SerialNo { get; set; }

    [JsonPropertyName("userType")]
    public string UserType { get; set; } = string.Empty;

    [JsonPropertyName("currentVerifyMode")]
    public string CurrentVerifyMode { get; set; } = string.Empty;

    [JsonPropertyName("attendanceStatus")]
    public string AttendanceStatus { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("mask")]
    public string Mask { get; set; } = string.Empty;

    [JsonPropertyName("pictureURL")]
    public string PictureURL { get; set; } = string.Empty;

    [JsonPropertyName("FaceRect")]
    public FaceRectangle FaceRect { get; set; } = new();
}

/// <summary>
/// مستطيل الوجه في الصورة
/// </summary>
public class FaceRectangle
{
    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
