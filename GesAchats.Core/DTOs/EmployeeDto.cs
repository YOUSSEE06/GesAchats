namespace GesAchats.Core.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; } // Note: No column yet, let's assume false for now
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
