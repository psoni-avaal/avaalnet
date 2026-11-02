using BasicDotNetPostgresWebsite.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AdminService and its dependencies into the service collection.
    /// </summary>
    public static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        // Register the service with a scoped lifetime, as it depends on DbContext
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}