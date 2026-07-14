using Base.Domain;

namespace Domain;

public class AppUser : BaseEntityWithMetaConcurrency
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Locale { get; set; } = "et";
    public string? BankIban { get; set; }
}
