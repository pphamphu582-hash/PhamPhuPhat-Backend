
namespace WorkTime.Api.Models;

public class WorkShift
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public Employee? Employee { get; set; }
}
