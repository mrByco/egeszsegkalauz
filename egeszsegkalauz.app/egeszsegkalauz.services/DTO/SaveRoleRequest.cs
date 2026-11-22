using System.ComponentModel.DataAnnotations;
using egeszsegkalauz.data.Models;

namespace egeszsegkalauz.services.DTO;

public class SaveRoleRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    public required List<Permission> Permissions { get; set; }
}
