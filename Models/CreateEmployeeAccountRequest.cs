
using System.ComponentModel.DataAnnotations;

namespace WorkTime.Api.Models;

public class CreateEmployeeAccountRequest
{
    [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
    [Range(1, int.MaxValue,
        ErrorMessage = "Mã nhân viên không hợp lệ.")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(8,
        ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    public string Password { get; set; } = string.Empty;
}
