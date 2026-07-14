using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Web.Configuration;

DotEnvConfiguration.LoadFromRepositoryRoot();
DotEnvConfiguration.ConfigureAspNetCoreUrls();

var builder = WebApplication.CreateBuilder(args);
var apiRateLimitOptions = RequiredConfiguration.ApiRateLimitOptions();

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(RateLimitPolicies.Api, limiterOptions =>
    {
        limiterOptions.PermitLimit = apiRateLimitOptions.PermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(apiRateLimitOptions.WindowSeconds);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = apiRateLimitOptions.QueueLimit;
    });
});
builder.ConfigureApplicationServices();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Alaasmagi Dotnet Template API v1");
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHealthChecks("/health").AllowAnonymous();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
