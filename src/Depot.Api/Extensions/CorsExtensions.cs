namespace Depot.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "WebClient";

    /// <summary>
    /// The browser asks permission (an OPTIONS request) before each API call that carries a token; without this
    /// it remembers the answer for only a few seconds, so a page opening several lists pays for every one.
    /// </summary>
    private static readonly TimeSpan _preflightMaxAge = TimeSpan.FromHours(1);

    /// <summary>Lets the browser app at the origins listed in Cors:AllowedOrigins call the API.</summary>
    public static IServiceCollection AddWebClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetPreflightMaxAge(_preflightMaxAge)));

        return services;
    }
}
