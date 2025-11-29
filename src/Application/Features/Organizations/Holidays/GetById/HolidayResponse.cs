namespace Application.Organizations.Holidays.GetById;

public sealed class HolidayResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
