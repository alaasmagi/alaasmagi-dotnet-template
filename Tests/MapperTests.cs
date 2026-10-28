using Domain;
using DTO.DataAccess;
using DTO.DataAccess.Mappers;
using DTO.Web;
using DTO.Web.Mappers;

namespace Tests;

public class MapperTests
{
    private static AppUser CreateAppUser() => new()
    {
        Id = Guid.NewGuid(),
        ConcurrencyToken = "token",
        Email = "user@example.com",
        Username = "user",
        FullName = "Full Name",
        Locale = "et"
    };

    [Test]
    public void AppUserEntityMapper_RoundTrip_KeepsValues()
    {
        var mapper = new AppUserEntityMapper();
        var appUser = CreateAppUser();

        var result = mapper.Map(mapper.Map(appUser));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(appUser.Id));
            Assert.That(result.ConcurrencyToken, Is.EqualTo(appUser.ConcurrencyToken));
            Assert.That(result.Email, Is.EqualTo(appUser.Email));
            Assert.That(result.Username, Is.EqualTo(appUser.Username));
            Assert.That(result.FullName, Is.EqualTo(appUser.FullName));
            Assert.That(result.Locale, Is.EqualTo(appUser.Locale));
        });
    }

    [Test]
    public void AppUserEntityMapper_Null_ReturnsNull()
    {
        var mapper = new AppUserEntityMapper();

        Assert.Multiple(() =>
        {
            Assert.That(mapper.Map((AppUser?)null), Is.Null);
            Assert.That(mapper.Map((AppUserEntity?)null), Is.Null);
        });
    }

    [Test]
    public void AppUserDtoMapper_RoundTrip_KeepsValues()
    {
        var mapper = new AppUserDtoMapper();
        var appUser = CreateAppUser();

        var result = mapper.Map(mapper.Map(appUser));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.EqualTo(appUser.Id));
            Assert.That(result.ConcurrencyToken, Is.EqualTo(appUser.ConcurrencyToken));
            Assert.That(result.Email, Is.EqualTo(appUser.Email));
            Assert.That(result.Locale, Is.EqualTo(appUser.Locale));
        });
    }

    [Test]
    public void AppUserDtoMapper_NewDto_GetsIdAndEmptyConcurrencyToken()
    {
        var mapper = new AppUserDtoMapper();

        var result = mapper.Map(new AppUserDto { Email = "user@example.com", Username = "user", FullName = "Full Name" });

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.ConcurrencyToken, Is.Empty);
            Assert.That(result.Locale, Is.EqualTo(AppUser.DefaultLocale));
        });
    }
}
