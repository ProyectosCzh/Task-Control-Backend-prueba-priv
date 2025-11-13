using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.Models;
using taskcontrolv1.Models.Enums;
using taskcontrolv1.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace taskcontrolv1.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly AppDbContext _db;
        public EmpresaService(AppDbContext db) => _db = db;

        // Obtener empresa por Id
        public Task<Empresa?> GetByIdAsync(int id) => _db.Empresas.FirstOrDefaultAsync(e => e.Id == id);

        // Verifica si una empresa está aprobada
        public async Task<bool> EmpresaEstaAprobadaAsync(int empresaId)
        {
            var e = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == empresaId);
            return e is not null && e.Estado == EstadoEmpresa.Approved;
        }

        // Crear empresa con estado Pending
        public async Task<int> CrearEmpresaPendingAsync(string nombre, string? dir, string? tel)
        {
            var e = new Empresa { Nombre = nombre, Direccion = dir, Telefono = tel, Estado = EstadoEmpresa.Pending };
            _db.Empresas.Add(e);
            await _db.SaveChangesAsync();
            return e.Id;
        }

        // Aprobar empresa
        public async Task AprobarAsync(int empresaId)
        {
            var e = await _db.Empresas.FirstOrDefaultAsync(x => x.Id == empresaId) ?? throw new KeyNotFoundException();
            e.Estado = EstadoEmpresa.Approved;
            e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Rechazar empresa
        public async Task RechazarAsync(int empresaId, string? motivo = null)
        {
            var e = await _db.Empresas.FirstOrDefaultAsync(x => x.Id == empresaId) ?? throw new KeyNotFoundException();
            e.Estado = EstadoEmpresa.Rejected;
            e.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // (Opcional) registrar motivo en otra tabla de auditoría
        }

        // Eliminación total de la empresa y todas sus relaciones
        public async Task HardDeleteAsync(int empresaId)
        {
            // 1) Cargar empresa
            var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.Id == empresaId);
            if (empresa is null)
                throw new KeyNotFoundException("Empresa no encontrada");

            // 2) Usuarios de la empresa
            var usuarios = await _db.Usuarios
                .Where(u => u.EmpresaId == empresaId)
                .ToListAsync();

            // 3) RefreshTokens de esos usuarios
            var userIds = usuarios.Select(u => u.Id).ToList();
            var tokens = await _db.RefreshTokens
                .Where(rt => userIds.Contains(rt.UsuarioId))
                .ToListAsync();

            // 4) Capacidades de la empresa
            var capacidades = await _db.Capacidades
                .Where(c => c.EmpresaId == empresaId)
                .ToListAsync();

            // 5) Relación Usuario-Capacidad
            var capacidadIds = capacidades.Select(c => c.Id).ToList();
            var usuarioCapacidades = await _db.UsuarioCapacidades
                .Where(uc => capacidadIds.Contains(uc.CapacidadId))
                .ToListAsync();

            // TODO: agregar tareas, chats u otras relaciones según se implementen

            // 6) Eliminación en orden
            _db.UsuarioCapacidades.RemoveRange(usuarioCapacidades);
            _db.Capacidades.RemoveRange(capacidades);
            _db.RefreshTokens.RemoveRange(tokens);
            _db.Usuarios.RemoveRange(usuarios);
            _db.Empresas.Remove(empresa);

            await _db.SaveChangesAsync();
        }
    }
}
