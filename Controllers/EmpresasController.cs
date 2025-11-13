using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.DTOs.Empresa;
using taskcontrolv1.Models.Enums;
using taskcontrolv1.Services.Interfaces;
using taskcontrolv1.Filters;
namespace taskcontrolv1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmpresasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmpresaService _svc;

    public EmpresasController(AppDbContext db, IEmpresaService svc)
    {
        _db = db; _svc = svc;
    }

    // GET /api/empresas?estado=Approved|Pending|Rejected
    [HttpGet]
    [AuthorizeRole(RolUsuario.AdminGeneral)]
    public async Task<IActionResult> List([FromQuery] string? estado = null)
    {
        var query = _db.Empresas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoEmpresa>(estado, true, out var st))
            query = query.Where(e => e.Estado == st);

        var list = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EmpresaListDTO
            {
                Id = e.Id, Nombre = e.Nombre, Estado = e.Estado.ToString(), CreatedAt = e.CreatedAt
            }).ToListAsync();

        return Ok(new { success = true, data = list });
    }

    [HttpPut("{id:int}/aprobar")]
    [AuthorizeRole(RolUsuario.AdminGeneral)]
    public async Task<IActionResult> Aprobar([FromRoute] int id)
    {
        await _svc.AprobarAsync(id);
        return Ok(new { success = true, message = "Empresa aprobada exitosamente" });
    }

    [HttpPut("{id:int}/rechazar")]
    [AuthorizeRole(RolUsuario.AdminGeneral)]
    public async Task<IActionResult> Rechazar([FromRoute] int id)
    {
        await _svc.RechazarAsync(id);
        return Ok(new { success = true, message = "Empresa rechazada exitosamente" });
    }

    // GET /api/empresas/{id}/estadisticas  (pendiente de completar en siguiente iteración)
    [HttpGet("{id:int}/estadisticas")]
    [Authorize] // AdminGeneral o AdminEmpresa dueño
    public async Task<IActionResult> Estadisticas([FromRoute] int id)
    {
        // TODO: calcular agregados reales cuando tengamos Tareas y Usuarios completos.
        var empresa = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (empresa is null) return NotFound();

        var mock = new {
            empresaId = id,
            totalTrabajadores = 0,
            trabajadoresActivos = 0,
            totalTareas = 0,
            tareasPendientes = 0,
            tareasAsignadas = 0,
            tareasAceptadas = 0,
            tareasFinalizadas = 0,
            tareasCanceladas = 0
        };
        return Ok(new { success = true, data = mock });
    }
}
