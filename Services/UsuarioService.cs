using System.Linq;
using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.DTOs.Usuario;
using taskcontrolv1.Helpers;
using taskcontrolv1.Models;
using taskcontrolv1.Models.Enums;
using taskcontrolv1.Services.Interfaces;

namespace taskcontrolv1.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _db;
    public UsuarioService(AppDbContext db) => _db = db;

    public async Task<int> CreateAsync(int empresaId, CreateUsuarioDTO dto)
    {
        var capsIds = dto.Capacidades.Select(c => c.CapacidadId).ToList();
        if (capsIds.Count > 0)
        {
            var validCount = await _db.Capacidades
                .CountAsync(c => c.EmpresaId == empresaId && capsIds.Contains(c.Id) && c.IsActive);

            if (validCount != capsIds.Count)
                throw new ArgumentException("Hay capacidades que no pertenecen a esta empresa o están inactivas");
        }

        PasswordHasher.CreatePasswordHash(dto.Password, out var hash, out var salt);

        var user = new Usuario
        {
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            Telefono = dto.Telefono,
            PasswordHash = hash,
            PasswordSalt = salt,
            Rol = RolUsuario.Usuario,
            EmpresaId = empresaId,
            Departamento = dto.Departamento,
            NivelHabilidad = dto.NivelHabilidad
        };

        foreach (var c in dto.Capacidades)
        {
            user.UsuarioCapacidades.Add(new UsuarioCapacidad
            {
                CapacidadId = c.CapacidadId,
                Nivel = c.Nivel
            });
        }

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();
        return user.Id;
    }

    public async Task<UsuarioDTO?> GetAsync(
        int requesterUserId,
        int? requesterEmpresaId,
        int id,
        bool requesterIsAdminEmpresa,
        bool requesterIsAdminGeneral)
    {
        var u = await _db.Usuarios
            .Include(x => x.UsuarioCapacidades)
                .ThenInclude(uc => uc.Capacidad)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (u is null) return null;

        var isOwner = requesterUserId == id;
        if (!requesterIsAdminGeneral)
        {
            if (!isOwner && !(requesterIsAdminEmpresa && requesterEmpresaId == u.EmpresaId))
                throw new UnauthorizedAccessException("No autorizado");
        }

        return new UsuarioDTO
        {
            Id = u.Id,
            Email = u.Email,
            NombreCompleto = u.NombreCompleto,
            Telefono = u.Telefono,
            Rol = u.Rol.ToString(),
            EmpresaId = u.EmpresaId ?? 0,
            Departamento = u.Departamento?.ToString(),
            NivelHabilidad = u.NivelHabilidad,
            IsActive = u.IsActive,
            Capacidades = u.UsuarioCapacidades
                .Select(uc => new CapacidadNivelView
                {
                    CapacidadId = uc.CapacidadId,
                    Nombre = uc.Capacidad.Nombre,
                    Nivel = uc.Nivel
                }).ToList()
        };
    }

    public async Task<List<UsuarioListDTO>> ListAsync(int empresaId)
    {
        // ✅ FORZAMOS EL USO DE System.Linq.Queryable.Select
        var query = _db.Usuarios
            .AsNoTracking()
            .Where(u => u.EmpresaId == empresaId && u.Rol == RolUsuario.Usuario)
            .OrderByDescending(u => u.CreatedAt);

        return await EntityFrameworkQueryableExtensions.ToListAsync(
            Queryable.Select(query, u => new UsuarioListDTO
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                Email = u.Email,
                Departamento = u.Departamento != null ? u.Departamento.ToString() : null,
                NivelHabilidad = u.NivelHabilidad,
                IsActive = u.IsActive
            }));
    }

    public async Task UpdateAsync(int empresaId, int id, UpdateUsuarioDTO dto)
    {
        var u = await _db.Usuarios
            .Include(x => x.UsuarioCapacidades)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.EmpresaId == empresaId &&
                x.Rol == RolUsuario.Usuario);

        if (u is null)
            throw new KeyNotFoundException("Usuario no encontrado");

        u.NombreCompleto = dto.NombreCompleto;
        u.Telefono = dto.Telefono;
        u.Departamento = dto.Departamento;
        u.NivelHabilidad = dto.NivelHabilidad;
        u.UpdatedAt = DateTime.UtcNow;

        u.UsuarioCapacidades.Clear();

        if (dto.Capacidades.Count > 0)
        {
            var capsIds = dto.Capacidades.Select(c => c.CapacidadId).ToList();
            var validCount = await _db.Capacidades
                .CountAsync(c => c.EmpresaId == empresaId && capsIds.Contains(c.Id) && c.IsActive);

            if (validCount != capsIds.Count)
                throw new ArgumentException("Hay capacidades que no pertenecen a esta empresa o están inactivas");

            foreach (var c in dto.Capacidades)
            {
                u.UsuarioCapacidades.Add(new UsuarioCapacidad
                {
                    UsuarioId = u.Id,
                    CapacidadId = c.CapacidadId,
                    Nivel = c.Nivel
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int empresaId, int id)
    {
        var u = await _db.Usuarios
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.EmpresaId == empresaId &&
                x.Rol == RolUsuario.Usuario);

        if (u is null)
            throw new KeyNotFoundException("Usuario no encontrado");

        u.IsActive = false;
        u.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
