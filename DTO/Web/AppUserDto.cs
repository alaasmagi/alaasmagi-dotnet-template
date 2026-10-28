using System.ComponentModel.DataAnnotations;

namespace DTO.Web;

public record AppUserDto
{
    public Guid Id { get; init; }

    public string? ConcurrencyToken { get; init; }

    [Required]
    [StringLength(256)]
    public string Email { get; init; } = default!;

    [Required]
    [StringLength(256)]
    public string Username { get; init; } = default!;

    [Required]
    [StringLength(256)]
    public string FullName { get; init; } = default!;

    [Required]
    [StringLength(16)]
    public string Locale { get; init; } = Domain.AppUser.DefaultLocale;
}
