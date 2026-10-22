
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WorkTime.Api.Data;
using WorkTime.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace WorkTime.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EmployeesController : ControllerBase

{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. GET: api/employees
    // Lấy danh sách tất cả nhân viên
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .ToListAsync();

        return Ok(employees);
    }

    // 2. GET: api/employees/1
    // Lấy thông tin nhân viên theo ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy nhân viên."
            });
        }

        return Ok(employee);
    }

    // 3. POST: api/employees
    // Thêm nhân viên mới
    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        [FromBody] EmployeeRequest request)
    {
        var email = request.Email.Trim();

        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == email);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "Email đã tồn tại."
            });
        }

        var employee = new Employee
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = "Employee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = employee.Id },
            employee
        );
    }

    // 4. PUT: api/employees/1
    // Cập nhật thông tin nhân viên
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        [FromBody] EmployeeRequest request)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy nhân viên."
            });
        }

        var email = request.Email.Trim();

        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == email && e.Id != id);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "Email đã được sử dụng."
            });
        }

        employee.FullName = request.FullName.Trim();
        employee.Email = email;
        employee.PhoneNumber = request.PhoneNumber.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Cập nhật nhân viên thành công.",
            employee
        });
    }

    // 5. DELETE: api/employees/1
    // Vô hiệu hóa nhân viên (xóa mềm)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy nhân viên."
            });
        }

        if (!employee.IsActive)
        {
            return BadRequest(new
            {
                message = "Nhân viên đã bị vô hiệu hóa."
            });
        }

        employee.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Đã vô hiệu hóa nhân viên thành công."
        });
    }
}

// Dữ liệu đầu vào khi thêm/sửa nhân viên
public class EmployeeRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}
