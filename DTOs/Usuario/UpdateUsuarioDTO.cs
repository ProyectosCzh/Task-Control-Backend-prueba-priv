using System.ComponentModel.DataAnnotations;
using taskcontrolv1.Models.Enums;

namespace taskcontrolv1.DTOs.Usuario;

public class UpdateUsuarioDTO
{
    [Required] public string NombreCompleto { get; set; } = null!;
    public string? Telefono { get; set; }
    public Departamento? Departamento { get; set; }
    [Range(1,5)] public int? NivelHabilidad { get; set; }
    public List<CapacidadNivelItem> Capacidades { get; set; } = new();
}