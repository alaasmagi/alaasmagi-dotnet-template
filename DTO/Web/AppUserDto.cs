using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace DTO.Web;

public class AppUserDto : BaseEntityWithConcurrency
{
    [Required]
    [StringLength(320)]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(256)]
    public string Username { get; set; } = default!;

    [Required]
    [StringLength(256)]
    public string FullName { get; set; } = default!;

    [Required]
    [StringLength(16)]
    public string Locale { get; set; } = "en";
}
