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

    // 5.1 CREATE ASYNC — SIN CAMBIOS
    public async Task<int> CreateAsync(int empresaId, CreateUsuarioDTO dto)
    {
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

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();
        return user.Id;
    }

    // GET ORIGINAL — SIN CAMBIOS
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
    
    // LIST ORIGINAL — SIN CAMBIOS
    public async Task<List<UsuarioListDTO>> ListAsync(int empresaId)
    {
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
    
    // UPDATE ORIGINAL — SIN CAMBIOS
    public async Task UpdateAsync(int empresaId, int id, UpdateUsuarioDTO dto)
    {
        var u = await _db.Usuarios
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

        await _db.SaveChangesAsync();
    }
    
    // DELETE ORIGINAL — SIN CAMBIOS
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
    
    // 1.3 MÉTODO NUEVO: CREAR O BUSCAR CAPACIDAD POR NOMBRE
    private async Task<Capacidad> GetOrCreateCapacidadAsync(int empresaId, string nombre)
    {
        var nombreNorm = nombre.Trim();

        var capacidad = await _db.Capacidades
            .FirstOrDefaultAsync(c => 
                c.EmpresaId == empresaId &&
                c.Nombre == nombreNorm &&
                c.IsActive);

        if (capacidad is not null)
            return capacidad;

        capacidad = new Capacidad
        {
            EmpresaId = empresaId,
            Nombre = nombreNorm,
            IsActive = true
        };

        _db.Capacidades.Add(capacidad);
        await _db.SaveChangesAsync();

        return capacidad;
    }
    
    // ACTUALIZAR CAPACIDADES COMO ADMIN (NUEVA LÓGICA)
    public async Task UpdateCapacidadesComoAdminAsync(
        int empresaId,
        int usuarioId,
        List<CapacidadNivelItem> capacidades)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioCapacidades)
                .ThenInclude(uc => uc.Capacidad)
            .FirstOrDefaultAsync(u => 
                u.Id == usuarioId &&
                u.EmpresaId == empresaId &&
                u.Rol == RolUsuario.Usuario);

        if (usuario is null)
            throw new KeyNotFoundException("Usuario no encontrado");

        usuario.UsuarioCapacidades.Clear();

        foreach (var item in capacidades)
        {
            var capacidad = await GetOrCreateCapacidadAsync(empresaId, item.Nombre);

            usuario.UsuarioCapacidades.Add(new UsuarioCapacidad
            {
                UsuarioId = usuario.Id,
                CapacidadId = capacidad.Id,
                Nivel = item.Nivel
            });
        }

        usuario.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
    
    // ACTUALIZAR MIS CAPACIDADES (TRABAJADOR)
    public async Task UpdateMisCapacidadesAsync(
        int usuarioId,
        int empresaId,
        List<CapacidadNivelItem> capacidades)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.UsuarioCapacidades)
                .ThenInclude(uc => uc.Capacidad)
            .FirstOrDefaultAsync(u => 
                u.Id == usuarioId &&
                u.EmpresaId == empresaId);

        if (usuario is null)
            throw new KeyNotFoundException("Usuario no encontrado");

        usuario.UsuarioCapacidades.Clear();

        foreach (var item in capacidades)
        {
            var capacidad = await GetOrCreateCapacidadAsync(empresaId, item.Nombre);

            usuario.UsuarioCapacidades.Add(new UsuarioCapacidad
            {
                UsuarioId = usuario.Id,
                CapacidadId = capacidad.Id,
                Nivel = item.Nivel
            });
        }

        usuario.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
