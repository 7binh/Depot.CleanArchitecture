namespace Depot.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "WebClient";

    /// <summary>Lets the browser app at the origins listed in Cors:AllowedOrigins call the API.</summary>
    public static IServiceCollection AddWebClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
