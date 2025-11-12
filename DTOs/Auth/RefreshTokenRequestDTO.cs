using System.ComponentModel.DataAnnotations;

namespace taskcontrolv1.DTOs.Auth;

public class RefreshTokenRequestDTO
{
    [Required] public string RefreshToken { get; set; } = null!;
}