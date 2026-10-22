
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WorkTime.Api.Data;
using WorkTime.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Controllers
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDataProtection();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Chưa cấu hình Jwt:Key trong User Secrets.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Chưa cấu hình Jwt:Issuer.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Chưa cấu hình Jwt:Audience.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();


// Kết nối SQL Server
var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "Không tìm thấy DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString));

// Cấu hình hệ thống tài khoản Identity
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

var app = builder.Build();

// Tạo các vai trò mặc định cho hệ thống WorkTime
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Manager", "Employee" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể tạo vai trò {role}: " +
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }
        }
    }
}


// Chỉ khởi tạo tài khoản Admin khi phát triển
if (app.Environment.IsDevelopment())
{
    var adminEmail =
        app.Configuration["BootstrapAdmin:Email"];

    var adminPassword =
        app.Configuration["BootstrapAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        using var scope = app.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var existingUser =
            await userManager.FindByEmailAsync(adminEmail);

        if (existingUser == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult =
                await userManager.CreateAsync(
                    admin, adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        createResult.Errors.Select(
                            e => e.Description)));
            }

            var roleResult =
                await userManager.AddToRoleAsync(
                    admin, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Không thể gán vai trò Admin: " +
                    string.Join("; ",
                        roleResult.Errors.Select(
                            e => e.Description)));
            }

            Console.WriteLine(
                "Đã tạo tài khoản Admin đầu tiên.");
        }
        else
        {
            Console.WriteLine(
                "Email Admin đã tồn tại. " +
                "Không tự động thay đổi quyền tài khoản.");
        }
    }
}


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
