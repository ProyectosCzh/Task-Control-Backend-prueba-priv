using taskcontrolv1.DTOs.Usuario;

namespace taskcontrolv1.Services.Interfaces;

public interface IUsuarioService
{
    Task<int> CreateAsync(int empresaId, CreateUsuarioDTO dto);
    Task<UsuarioDTO?> GetAsync(int requesterUserId, int? requesterEmpresaId, int id, bool requesterIsAdminEmpresa, bool requesterIsAdminGeneral);
    Task<List<UsuarioListDTO>> ListAsync(int empresaId);
    Task UpdateAsync(int empresaId, int id, UpdateUsuarioDTO dto);
    Task DeleteAsync(int empresaId, int id); // Soft delete = IsActive = false
}