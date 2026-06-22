using Microsoft.Extensions.Options;

namespace HungarianVatDeclarationGenerator.Api.Configuration;

/// <summary>
/// Extension methods for registering configuration settings in DI container.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Binds a configuration section to a settings class and registers both IOptions&lt;T&gt; and T in DI.
    /// </summary>
    /// <typeparam name="T">The settings class type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <returns>The bound settings instance.</returns>
    public static T ConfigureSettings<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where T : class
    {
        IConfigurationSection configSection = configuration.GetSection(sectionName);

        services.Configure<T>(configSection);
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<T>>().Value);

        T settings = configSection.Get<T>()
            ?? throw new InvalidOperationException($"Configuration section '{sectionName}' is missing or invalid.");

        return settings;
    }
}
