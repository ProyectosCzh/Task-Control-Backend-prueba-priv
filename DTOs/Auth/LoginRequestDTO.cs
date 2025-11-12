using System.ComponentModel.DataAnnotations;

namespace taskcontrolv1.DTOs.Auth;

public class LoginRequestDTO
{
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
    public int? EmpresaId { get; set; } // null para AdminGeneral
}