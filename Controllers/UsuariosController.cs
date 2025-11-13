using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.DTOs.Usuario;
using taskcontrolv1.Models.Enums;
using taskcontrolv1.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace taskcontrolv1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUsuarioService _svc;

    public UsuariosController(AppDbContext db, IUsuarioService svc)
    {
        _db = db;
        _svc = svc;
    }

    // --- PERFIL DEL USUARIO AUTENTICADO ---
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(sub))
            return Unauthorized();

        if (!int.TryParse(sub, out var userId))
            return BadRequest(new { success = false, message = "ID de usuario inválido en el token" });

        var u = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (u is null)
            return NotFound(new { success = false, message = "Usuario no encontrado" });

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

    // --- HELPERS DE CONTEXTO Y ROLES ---
    private bool IsAdminGeneral() =>
        string.Equals(User.FindFirstValue(ClaimTypes.Role),
                      RolUsuario.AdminGeneral.ToString(),
                      StringComparison.Ordinal);

    private bool IsAdminEmpresa() =>
        string.Equals(User.FindFirstValue(ClaimTypes.Role),
                      RolUsuario.AdminEmpresa.ToString(),
                      StringComparison.Ordinal);

    private int? EmpresaIdClaim()
    {
        var v = User.FindFirst("empresaId")?.Value;
        return int.TryParse(v, out var id) ? id : null;
    }

    private int UserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.Parse(sub!);
    }

    // --- CRUD DE USUARIOS (ADMIN EMPRESA / ADMIN GENERAL) ---

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioDTO dto)
    {
        if (!IsAdminEmpresa()) return Forbid();
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        var id = await _svc.CreateAsync(empresaId.Value, dto);
        return StatusCode(201, new { success = true, message = "Usuario creado", data = new { id } });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!IsAdminEmpresa() && !IsAdminGeneral()) return Forbid();

        var empresaId = EmpresaIdClaim();
        if (!IsAdminGeneral() && empresaId is null) return Unauthorized();

        var list = await _svc.ListAsync(empresaId ?? 0);
        return Ok(new { success = true, data = list });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var dto = await _svc.GetAsync(
            requesterUserId: UserId(),
            requesterEmpresaId: EmpresaIdClaim(),
            id: id,
            requesterIsAdminEmpresa: IsAdminEmpresa(),
            requesterIsAdminGeneral: IsAdminGeneral()
        );

        if (dto is null)
            return NotFound(new { success = false, message = "Usuario no encontrado" });

        return Ok(new { success = true, data = dto });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateUsuarioDTO dto)
    {
        if (!IsAdminEmpresa()) return Forbid();
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        await _svc.UpdateAsync(empresaId.Value, id, dto);
        return Ok(new { success = true, message = "Usuario actualizado" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!IsAdminEmpresa()) return Forbid();

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        await _svc.DeleteAsync(empresaId.Value, id);
        return Ok(new { success = true, message = "Usuario desactivado" });
    }
}
