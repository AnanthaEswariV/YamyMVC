
namespace YamyProject.Core.Models;

public partial class TblSecUser
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string EmpId { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public int RoleId { get; set; }

    public int Active { get; set; }

    public int State { get; set; }

    public int? PasswordUpdatedBy { get; set; }

    public DateOnly? PasswordLastUpdate { get; set; }
}
