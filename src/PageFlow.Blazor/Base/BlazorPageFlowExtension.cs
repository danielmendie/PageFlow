using Microsoft.Extensions.DependencyInjection;
using PageFlow.Blazor.Business;

namespace PageFlow.Blazor
{
    public static class DynamicNavigatorAccessorExtension
    {
        /// <summary>
        /// Adds services required for the Blazor Dynamic Navigator to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <remarks>This method registers the necessary services for the Blazor Dynamic Navigator,
        /// including navigation state management, route resolution, and route history storage. It also configures the
        /// provided <see cref="PageFlowSettings"/> instance using the specified <paramref name="configuration"/>
        /// delegate.</remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the services will be added.</param>
        /// <param name="configuration">A delegate to configure the <see cref="PageFlowSettings"/> used by the Blazor Dynamic Navigator.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
        public static IServiceCollection AddBlazorPageFlow(this IServiceCollection services, Action<PageFlowSettings> configuration = default!)
        {
            PageFlowSettings options = new PageFlowSettings();
            configuration?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<PageFlowRegistry>();
            services.AddScoped<IRouteStorage, RouteStorage>();
            services.AddScoped<IRouteHistory, RouteHistory>();
            services.AddScoped<NavigationState>();
            services.AddScoped<ILocalStorageProvider, LocalStorageProvider>();
            services.AddScoped<IRoutesProvider, RouteProvider>();
            services.AddScoped<IRouteResolver, RouteResolver>();
            services.AddScoped<IPageFlowNavigator, RouteNavigator>();
            return services;
        }
    }
}
