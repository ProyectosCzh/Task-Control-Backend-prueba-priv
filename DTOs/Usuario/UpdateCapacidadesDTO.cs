using System.ComponentModel.DataAnnotations;

namespace taskcontrolv1.DTOs.Usuario;

public class UpdateCapacidadesDTO
{
    [Required]
    public List<CapacidadNivelItem> Capacidades { get; set; } = new();
}