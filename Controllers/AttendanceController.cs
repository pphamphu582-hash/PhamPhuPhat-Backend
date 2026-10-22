
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using WorkTime.Api.Data;
using WorkTime.Api.Models;

namespace WorkTime.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Lay nhan vien tu tai khoan dang nhap
    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await _userManager.FindByIdAsync(userId);

        if (user?.EmployeeId == null)
            return null;

        var employee = await _context.Employees
            .FindAsync(user.EmployeeId.Value);

        if (employee == null || !employee.IsActive)
            return null;

        return employee;
    }

    // ==========================================
    // 1. EMPLOYEE CHECK-IN
    // POST: api/attendance/checkin
    // ==========================================

    [HttpPost("checkin")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckIn(
        [FromBody] AttendanceCheckInRequest request)
    {
        var employee = await GetCurrentEmployeeAsync();

        if (employee == null)
        {
            return Forbid();
        }

        // Phai cung cap ca 2 toa do hoac khong toa do
        if (request.Latitude.HasValue !=
            request.Longitude.HasValue)
        {
            return BadRequest(new
            {
                message = "Cần cung cấp cả vĩ độ và kinh độ."
            });
        }

        // Khong cho check-in khi dang co ca chua check-out
        bool hasOpenShift = await _context.AttendanceRecords
            .AnyAsync(a =>
                a.EmployeeId == employee.Id &&
                a.CheckOutTime == null);

        if (hasOpenShift)
        {
            return BadRequest(new
            {
                message = "Bạn đã check-in và chưa check-out."
            });
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            CheckInTime = DateTime.UtcNow,
            CheckOutTime = null,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInMethod = request.Latitude.HasValue
                ? "GPS"
                : "Manual"
        };

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Check-in thành công.",
            record.Id,
            record.EmployeeId,
            employee.FullName,
            record.CheckInTime,
            record.CheckInMethod
        });
    }

    // ==========================================
    // 2. EMPLOYEE CHECK-OUT
    // POST: api/attendance/checkout
    // ==========================================

    [HttpPost("checkout")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> CheckOut()
    {
        var employee = await GetCurrentEmployeeAsync();

        if (employee == null)
        {
            return Forbid();
        }

        var record = await _context.AttendanceRecords
            .Where(a =>
                a.EmployeeId == employee.Id &&
                a.CheckOutTime == null)
            .OrderByDescending(a => a.CheckInTime)
            .FirstOrDefaultAsync();

        if (record == null)
        {
            return BadRequest(new
            {
                message = "Bạn chưa có ca làm đang mở."
            });
        }

        record.CheckOutTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var totalMinutes =
            (record.CheckOutTime.Value -
             record.CheckInTime).TotalMinutes;

        return Ok(new
        {
            message = "Check-out thành công.",
            record.Id,
            record.EmployeeId,
            record.CheckInTime,
            record.CheckOutTime,
            totalMinutes = Math.Round(totalMinutes, 2)
        });
    }

    // ==========================================
    // 3. EMPLOYEE XEM LICH SU CUA MINH
    // GET: api/attendance/mine
    // ==========================================

    [HttpGet("mine")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyAttendance()
    {
        var employee = await GetCurrentEmployeeAsync();

        if (employee == null)
        {
            return Forbid();
        }

        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id)
            .OrderByDescending(a => a.CheckInTime)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.CheckInTime,
                a.CheckOutTime,
                a.CheckInLatitude,
                a.CheckInLongitude,
                a.CheckInMethod
            })
            .ToListAsync();

        return Ok(new
        {
            employee.Id,
            employee.FullName,
            totalRecords = records.Count,
            records
        });
    }

    // ==========================================
    // 4. ADMIN XEM LICH SU 1 NHAN VIEN
    // GET: api/attendance/employee/1
    // ==========================================

    [HttpGet("employee/{employeeId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmployeeHistory(
        int employeeId)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy nhân viên."
            });
        }

        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.CheckInTime)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.CheckInTime,
                a.CheckOutTime,
                a.CheckInLatitude,
                a.CheckInLongitude,
                a.CheckInMethod
            })
            .ToListAsync();

        return Ok(new
        {
            employee.Id,
            employee.FullName,
            totalRecords = records.Count,
            records
        });
    }

    // ==========================================
    // 5. ADMIN XEM CHAM CONG HOM NAY
    // GET: api/attendance/today
    // ==========================================

    [HttpGet("today")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetTodayAttendance()
    {
        // Xac dinh ngay hien tai theo gio Viet Nam UTC+7
        var vietnamToday = DateTime.UtcNow
            .AddHours(7)
            .Date;

        var startUtc = vietnamToday.AddHours(-7);
        var endUtc = startUtc.AddDays(1);

        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a =>
                a.CheckInTime >= startUtc &&
                a.CheckInTime < endUtc)
            .OrderByDescending(a => a.CheckInTime)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.CheckInTime,
                a.CheckOutTime,
                a.CheckInMethod
            })
            .ToListAsync();

        return Ok(new
        {
            date = vietnamToday.ToString("yyyy-MM-dd"),
            totalRecords = records.Count,
            records
        });
    }
}

// Du lieu dau vao Check-in
public class AttendanceCheckInRequest
{
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }
}
