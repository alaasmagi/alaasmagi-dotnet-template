using Asp.Versioning;
using Base.Contracts.DTO;
using Base.DTO;
using Contracts.Application;
using DTO.Web;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Web.Configuration;
using IMapper = Base.Contracts.DTO.IMapper<DTO.Web.AppUserDto, Domain.AppUser>;

namespace Web.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[EnableRateLimiting(RateLimitPolicies.Api)]
[Route("api/v{version:apiVersion}/app-users")]
[Produces("application/json")]
public class AppUsersController(
    IAppUserService appUserService,
    IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUserDto>>> GetAll()
    {
        var result = await appUserService.GetAllAsync();
        if (!result.Successful)
        {
            return ToProblem(result.Error);
        }

        return Ok(mapper.Map(result.Value)?.ToList() ?? []);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppUserDto>> GetById(Guid id)
    {
        var result = await appUserService.GetByIdAsync(id);
        if (!result.Successful)
        {
            return ToProblem(result.Error);
        }

        return Ok(mapper.Map(result.Value));
    }

    [HttpPost]
    public async Task<ActionResult<AppUserDto>> Create(AppUserDto input)
    {
        var appUser = mapper.Map(input);
        if (appUser == null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid AppUser payload.");
        }

        var result = await appUserService.CreateAsync(appUser);
        if (!result.Successful)
        {
            return ToProblem(result.Error);
        }

        var output = mapper.Map(result.Value)!;
        return CreatedAtAction(nameof(GetById), new { version = "1", id = output.Id }, output);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppUserDto>> Update(
        Guid id,
        AppUserDto input,
        [FromHeader(Name = "If-Match")] string? expectedConcurrencyToken)
    {
        var appUser = mapper.Map(input);
        if (appUser == null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid AppUser payload.");
        }

        var result = await appUserService.UpdateAsync(id, appUser, NormalizeConcurrencyToken(expectedConcurrencyToken));
        if (!result.Successful)
        {
            return ToProblem(result.Error);
        }

        return Ok(mapper.Map(result.Value));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "If-Match")] string? expectedConcurrencyToken)
    {
        var result = await appUserService.RemoveAsync(id, NormalizeConcurrencyToken(expectedConcurrencyToken));
        if (!result.Successful)
        {
            return ToProblem(result.Error);
        }

        return NoContent();
    }

    private static string? NormalizeConcurrencyToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Trim('"');
    }

    private ObjectResult ToProblem(IError? error)
    {
        var statusCode = error?.Code switch
        {
            ErrorDefaults.Codes.NotFound => StatusCodes.Status404NotFound,
            ErrorDefaults.Codes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorDefaults.Codes.ConcurrencyConflict => StatusCodes.Status409Conflict,
            ErrorDefaults.Codes.ConcurrencyTokenRequired => StatusCodes.Status428PreconditionRequired,
            ErrorDefaults.Codes.InvalidPaging => StatusCodes.Status400BadRequest,
            ErrorDefaults.Codes.MapFailed => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            statusCode: statusCode,
            title: error?.Code ?? "REQUEST_FAILED",
            detail: error?.Message ?? "The request failed.");
    }
}
