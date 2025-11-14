using System.ComponentModel.DataAnnotations;

namespace taskcontrolv1.DTOs.Usuario;

public class CapacidadNivelItem
{
    [Required]
    public string Nombre { get; set; } = null!;  // ej: "Buen diseñador"

    [Range(1,5)]
    public int Nivel { get; set; } = 1;          // 1..5
}