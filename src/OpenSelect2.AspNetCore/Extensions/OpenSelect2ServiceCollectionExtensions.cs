using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace OpenSelect2.AspNetCore;

/// <summary>
/// DI registration for OpenSelect2.
/// </summary>
public static class OpenSelect2ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenSelect2: its options and the application part that makes the <c>Select2</c>
    /// ViewComponent and its compiled views discoverable.
    /// </summary>
    /// <remarks>
    /// The application-part registration is required because this package references MVC through the
    /// shared framework (<c>Microsoft.AspNetCore.App</c>), which hides the MVC dependency from the
    /// default deps.json-based part discovery — the same reason <c>AddDefaultUI()</c> exists. Call this
    /// after <c>AddControllersWithViews()</c> (or any <c>AddMvc*</c> call).
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional options configuration (login URL, default limit, localization).</param>
    public static IServiceCollection AddOpenSelect2(
        this IServiceCollection services,
        Action<OpenSelect2Options>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddOptions<OpenSelect2Options>();
        if (configure != null)
            builder.Configure(configure);

        AddApplicationPart(services, typeof(OpenSelect2ServiceCollectionExtensions).Assembly);

        return services;
    }

    // Mirrors IMvcBuilder.AddApplicationPart: resolves the assembly's parts via its part factory
    // (so an RCL's consolidated views + types part is honored) and adds any not already present.
    private static void AddApplicationPart(IServiceCollection services, Assembly assembly)
    {
        var partManager = GetApplicationPartManager(services);
        var factory = ApplicationPartFactory.GetApplicationPartFactory(assembly);

        // An RCL's factory yields multiple parts that share the assembly Name (e.g. an AssemblyPart for
        // types AND a CompiledRazorAssemblyPart for views) — dedup by type *and* name, not name alone.
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
