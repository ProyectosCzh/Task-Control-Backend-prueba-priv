using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.DTOs.Usuario;
//modelo agregado para uso de TokensSwagger
using System.IdentityModel.Tokens.Jwt;

namespace taskcontrolv1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsuariosController(AppDbContext db) => _db = db;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier) // fallback
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(sub)) return Unauthorized();

        var userId = int.Parse(sub);
        var u = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (u is null) return NotFound();

        var dto = new UsuarioPerfilDTO
        {
            Id = u.Id,
            Email = u.Email,
            NombreCompleto = u.NombreCompleto,
            Telefono = u.Telefono,
            Rol = u.Rol.ToString(),
            EmpresaId = u.EmpresaId
        };
        return Ok(new { success = true, data = dto });
    }
}