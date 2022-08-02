using Microsoft.EntityFrameworkCore;
using NetDevPack.Fido2.EntityFramework.Store.Store;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for registering crypto services
/// </summary>
public static class EFCoreServiceExtensions
{
    /// <summary>
    /// Sets the signing credential.
    /// </summary>
    public static IServiceCollection AddFido2Context<TContext>(this IServiceCollection services) where TContext : DbContext, IFido2Context
    {
        services.AddScoped<IFido2Store, Fido2Store<TContext>>();

        return services;
    }
}