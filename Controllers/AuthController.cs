using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using taskcontrolv1.DTOs.Auth;
using taskcontrolv1.Services.Interfaces;

namespace taskcontrolv1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO dto)
    {
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
        var res = await _auth.LoginAsync(dto);
        return Ok(new { success = true, message = "Login exitoso", data = res });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDTO dto)
    {
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
        var res = await _auth.RefreshAsync(dto);
        return Ok(new { success = true, data = res });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDTO dto)
    {
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
        await _auth.LogoutAsync(dto.RefreshToken);
        return Ok(new { success = true, message = "Logout OK" });
    }

    [HttpPost("register-adminempresa")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAdminEmpresa([FromBody] RegisterAdminEmpresaDTO dto)
    {
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
        var empresaId = await _auth.RegisterAdminEmpresaAsync(dto);
        return StatusCode(201, new { success = true, message = "Empresa registrada en Pending", data = new { empresaId } });
    }
}