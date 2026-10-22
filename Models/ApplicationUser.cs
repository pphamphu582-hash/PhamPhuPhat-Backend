
using Microsoft.AspNetCore.Identity;

namespace WorkTime.Api.Models;

public class ApplicationUser : IdentityUser
{
    public int? EmployeeId { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;
}
