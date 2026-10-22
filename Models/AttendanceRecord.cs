
namespace WorkTime.Api.Models;

public class AttendanceRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public double? CheckInLatitude { get; set; }

    public double? CheckInLongitude { get; set; }

    public string? CheckInMethod { get; set; }

    public Employee? Employee { get; set; }
}
