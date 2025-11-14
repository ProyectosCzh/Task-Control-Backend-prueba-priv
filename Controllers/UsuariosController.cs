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

    //  NUEVO: PERFIL COMPLETO DEL USUARIO AUTENTICADO (CON CAPACIDADES)
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = UserId();

        var dto = await _svc.GetAsync(
            requesterUserId: userId,
            requesterEmpresaId: EmpresaIdClaim(),
            id: userId,                                // solicita sus propios datos
            requesterIsAdminEmpresa: IsAdminEmpresa(),
            requesterIsAdminGeneral: IsAdminGeneral()
        );

        if (dto is null)
            return NotFound(new { success = false, message = "Usuario no encontrado" });

        return Ok(new { success = true, data = dto });
    }

    // HELPERS DE ROLES Y CLAIMS
    private bool IsAdminGeneral() =>
        string.Equals(
            User.FindFirstValue(ClaimTypes.Role),
            RolUsuario.AdminGeneral.ToString(),
            StringComparison.Ordinal
        );

    private bool IsAdminEmpresa() =>
        string.Equals(
            User.FindFirstValue(ClaimTypes.Role),
            RolUsuario.AdminEmpresa.ToString(),
            StringComparison.Ordinal
        );

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
    
    // CRUD DE USUARIOS (ADMIN EMPRESA / ADMIN GENERAL)
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
        if (!IsAdminGeneral() && empresaId is null)
            return Unauthorized();

        var list = await _svc.ListAsync(empresaId ?? 0);
        return Ok(new { success = true, data = list });
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
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
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDTO dto)
    {
        if (!IsAdminEmpresa()) return Forbid();
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        await _svc.UpdateAsync(empresaId.Value, id, dto);
        return Ok(new { success = true, message = "Usuario actualizado" });
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdminEmpresa()) return Forbid();

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        await _svc.DeleteAsync(empresaId.Value, id);
        return Ok(new { success = true, message = "Usuario desactivado" });
    }
    
    // CAPACIDADES DEL USUARIO
    [HttpPut("{id:int}/capacidades")]
    public async Task<IActionResult> UpdateCapacidadesUsuario(
        int id,
        [FromBody] UpdateCapacidadesDTO dto)
    {
        if (!IsAdminEmpresa()) return Forbid();
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        await _svc.UpdateCapacidadesComoAdminAsync(empresaId.Value, id, dto.Capacidades);

        return Ok(new { success = true, message = "Capacidades del usuario actualizadas" });
    }

    [HttpPut("mis-capacidades")]
    public async Task<IActionResult> UpdateMisCapacidades([FromBody] UpdateCapacidadesDTO dto)
    {
        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        var empresaId = EmpresaIdClaim();
        if (empresaId is null) return Unauthorized();

        var userId = UserId();

        await _svc.UpdateMisCapacidadesAsync(userId, empresaId.Value, dto.Capacidades);

        return Ok(new { success = true, message = "Tus capacidades han sido actualizadas" });
    }
}
