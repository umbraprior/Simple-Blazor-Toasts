using Microsoft.Extensions.DependencyInjection;
using Simple.Blazor.Toasts.Services;
using Simple.Blazor.Toasts.Models;

namespace Simple.Blazor.Toasts.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Simple Blazor Toasts services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSimpleBlazorToasts(this IServiceCollection services)
    {
        services.AddSingleton<ToastService>();
        return services;
    }

    /// <summary>
    /// Adds Simple Blazor Toasts services with configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSimpleBlazorToasts(
        this IServiceCollection services, 
        Action<ToastConfiguration> configure)
    {
        var config = new ToastConfiguration();
        configure(config);
        
        services.AddSingleton(config);
        services.AddSingleton<ToastService>();
        return services;
    }
}

/// <summary>
/// Configuration options for Simple Blazor Toasts
/// </summary>
public class ToastConfiguration
{
    public int MaxVisibleToasts { get; set; } = 5;
    public ToastPosition DefaultPosition { get; set; } = ToastPosition.TopRight;
    public ToastTheme DefaultTheme { get; set; } = ToastTheme.Dark;
    public ToastAnimation DefaultAnimation { get; set; } = ToastAnimation.SlideAndFade;
}