using Microsoft.EntityFrameworkCore;
using taskcontrolv1.Data;
using taskcontrolv1.Models;
using taskcontrolv1.Models.Enums;
using taskcontrolv1.Services.Interfaces;

namespace taskcontrolv1.Services;

public class EmpresaService : IEmpresaService
{
    private readonly AppDbContext _db;
    public EmpresaService(AppDbContext db) => _db = db;

    public Task<Empresa?> GetByIdAsync(int id) => _db.Empresas.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<bool> EmpresaEstaAprobadaAsync(int empresaId)
    {
        var e = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == empresaId);
        return e is not null && e.Estado == EstadoEmpresa.Approved;
    }

    public async Task<int> CrearEmpresaPendingAsync(string nombre, string? dir, string? tel)
    {
        var e = new Empresa { Nombre = nombre, Direccion = dir, Telefono = tel, Estado = EstadoEmpresa.Pending };
        _db.Empresas.Add(e);
        await _db.SaveChangesAsync();
        return e.Id;
    }

    public async Task AprobarAsync(int empresaId)
    {
        var e = await _db.Empresas.FirstOrDefaultAsync(x => x.Id == empresaId) ?? throw new KeyNotFoundException();
        e.Estado = EstadoEmpresa.Approved;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RechazarAsync(int empresaId, string? motivo = null)
    {
        var e = await _db.Empresas.FirstOrDefaultAsync(x => x.Id == empresaId) ?? throw new KeyNotFoundException();
        e.Estado = EstadoEmpresa.Rejected;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        // (opcional) registrar motivo en otra tabla de auditoría
    }
}