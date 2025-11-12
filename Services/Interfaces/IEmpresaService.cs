using taskcontrolv1.Models.Enums;
using taskcontrolv1.Models;

namespace taskcontrolv1.Services.Interfaces;

public interface IEmpresaService
{
    Task<Empresa?> GetByIdAsync(int id);
    Task<bool> EmpresaEstaAprobadaAsync(int empresaId);
    Task<int> CrearEmpresaPendingAsync(string nombre, string? dir, string? tel);
    Task AprobarAsync(int empresaId);
    Task RechazarAsync(int empresaId, string? motivo = null);
}