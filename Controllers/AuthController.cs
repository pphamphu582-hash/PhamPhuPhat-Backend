
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkTime.Api.Data;
using WorkTime.Api.Models;

namespace WorkTime.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
    }

    // ==================================================
    // 1. DANG NHAP VA NHAN JWT
    // POST: api/auth/login
    // ==================================================

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Email hoặc mật khẩu không đúng."
            });
        }

        // Kiểm tra tài khoản nhân viên còn hoạt động không
        if (user.EmployeeId.HasValue)
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.Id == user.EmployeeId.Value);

            if (employee == null || !employee.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản nhân viên không hoạt động."
                });
            }
        }

        // Kiểm tra mật khẩu và khóa tài khoản khi sai nhiều lần
        var signInResult = await _signInManager
            .CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return Unauthorized(new
            {
                message = "Tài khoản đang bị khóa tạm thời."
            });
        }

        if (!signInResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Email hoặc mật khẩu không đúng."
            });
        }

        // Lấy vai trò Admin, Manager hoặc Employee
        var roles = await _userManager.GetRolesAsync(user);

        // Tạo các Claims cho JWT
        var claims = new List<Claim>
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id),

            new Claim(
                ClaimTypes.Email,
                user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, role));
        }

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(
                "employeeId",
                user.EmployeeId.Value.ToString()));
        }

        // Lấy cấu hình JWT
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Jwt:Key trong User Secrets.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Jwt:Issuer.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Chưa cấu hình Jwt:Audience.");

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        // JWT có hiệu lực trong 1 giờ
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var jwtToken = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(jwtToken);

        return Ok(new
        {
            message = "Đăng nhập thành công.",
            accessToken,
            tokenType = "Bearer",
            expiresAt,
            user = new
            {
                user.Id,
                user.Email,
                user.EmployeeId,
                roles
            }
        });
    }

    // ==================================================
    // 2. LAY THONG TIN TAI KHOAN DANG DANG NHAP
    // GET: api/auth/me
    // ==================================================

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "Bạn chưa đăng nhập."
            });
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Tài khoản không tồn tại."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        string? fullName = null;

        if (user.EmployeeId.HasValue)
        {
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e => e.Id == user.EmployeeId.Value);

            if (employee == null || !employee.IsActive)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản nhân viên không hoạt động."
                });
            }

            fullName = employee.FullName;
        }

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.EmployeeId,
            fullName,
            roles
        });
    }

    // ==================================================
    // 3. ADMIN TAO TAI KHOAN CHO NHAN VIEN
    // POST: api/auth/create-employee
    // ==================================================

    [HttpPost("create-employee")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateEmployeeAccount(
        [FromBody] CreateEmployeeAccountRequest request)
    {
        // Tìm nhân viên trong bảng Employees
        var employee = await _context.Employees
            .FindAsync(request.EmployeeId);

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

        // Kiểm tra nhân viên đã liên kết tài khoản chưa
        var alreadyLinked = await _context.Users
            .AnyAsync(u => u.EmployeeId == employee.Id);

        if (alreadyLinked)
        {
            return Conflict(new
            {
                message = "Nhân viên đã có tài khoản đăng nhập."
            });
        }

        // Sử dụng email từ hồ sơ nhân viên
        var email = employee.Email.Trim();

        var existingUser = await _userManager
            .FindByEmailAsync(email);

        if (existingUser != null)
        {
            return Conflict(new
            {
                message = "Email đã được sử dụng."
            });
        }

        // Tạo tài khoản Identity
        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            EmployeeId = employee.Id,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager
            .CreateAsync(newUser, request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Tạo tài khoản thất bại.",
                errors = createResult.Errors
                    .Select(e => e.Description)
                    .ToList()
            });
        }

        // Gán quyền Employee
        var roleResult = await _userManager
            .AddToRoleAsync(newUser, "Employee");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(newUser);

            return StatusCode(500, new
            {
                message = "Không thể gán quyền Employee.",
                errors = roleResult.Errors
                    .Select(e => e.Description)
                    .ToList()
            });
        }

        return Ok(new
        {
            message = "Tạo tài khoản nhân viên thành công.",
            newUser.Id,
            newUser.Email,
            newUser.EmployeeId,
            fullName = employee.FullName,
            role = "Employee"
        });
    }
}

// ==================================================
// DU LIEU DAU VAO DANG NHAP
// ==================================================

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;
}
