using Base.Domain;

namespace Domain;

public class AppUser : BaseEntityWithConcurrency
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Locale { get; set; } = "en";
}
