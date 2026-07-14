using System.ComponentModel.DataAnnotations;

namespace DTO.Web;

public class AppUserDto
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? ConcurrencyToken { get; set; }

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
    public string Locale { get; set; } = "et";

    [StringLength(64)]
    public string? BankIban { get; set; }
}
