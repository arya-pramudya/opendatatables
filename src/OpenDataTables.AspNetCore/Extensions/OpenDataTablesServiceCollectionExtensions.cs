using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using OpenSelect2.AspNetCore; // for AddOpenSelect2

namespace OpenDataTables.AspNetCore;

/// <summary>
/// DI registration for OpenDataTables.
/// </summary>
public static class OpenDataTablesServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenDataTables: its options and the application part that makes the <c>DataTable</c> /
    /// <c>FilterCard</c> ViewComponents and their compiled views discoverable.
    /// Also ensures OpenSelect2 is registered (its select filters depend on it).
    /// </summary>
    /// <remarks>
    /// The application-part registration is required because this package references MVC through the
    /// shared framework, which hides the MVC dependency from default deps.json-based part discovery.
    /// Call after <c>AddControllersWithViews()</c>.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional options configuration (login URL, page length, localization).</param>
    public static IServiceCollection AddOpenDataTables(
        this IServiceCollection services,
        Action<OpenDataTablesOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddOptions<OpenDataTablesOptions>();
        if (configure != null)
            builder.Configure(configure);

        // DataTable select filters render Select2, so make sure that package is wired too.
        services.AddOpenSelect2();

        AddApplicationPart(services, typeof(OpenDataTablesServiceCollectionExtensions).Assembly);

        return services;
    }

    private static void AddApplicationPart(IServiceCollection services, Assembly assembly)
    {
        var partManager = GetApplicationPartManager(services);
        var factory = ApplicationPartFactory.GetApplicationPartFactory(assembly);

        // An RCL's factory yields multiple parts sharing the assembly Name (types + compiled views) —
        // dedup by type and name, not name alone.
        foreach (var part in factory.GetApplicationParts(assembly))
        {
            if (!partManager.ApplicationParts.Any(p => p.GetType() == part.GetType() && p.Name == part.Name))
                partManager.ApplicationParts.Add(part);
        }
    }

    private static ApplicationPartManager GetApplicationPartManager(IServiceCollection services)
    {
        var existing = services
            .LastOrDefault(d => d.ServiceType == typeof(ApplicationPartManager))?
            .ImplementationInstance as ApplicationPartManager;

        if (existing != null)
            return existing;

        var manager = new ApplicationPartManager();
        services.AddSingleton(manager);
        return manager;
    }
}
