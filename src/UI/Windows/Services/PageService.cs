using System.Windows;
using Wpf.Ui.Abstractions;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Service that provides pages for navigation.
/// </summary>
public class PageService : INavigationViewPageProvider
{
    /// <summary>
    /// Service which provides the instances of pages.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates new instance and attaches the <see cref="IServiceProvider"/>.
    /// </summary>
    public PageService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public object? GetPage(Type pageType)
    {
        if (!typeof(FrameworkElement).IsAssignableFrom(pageType))
            throw new InvalidOperationException("The page should be a WPF control.");

        return _serviceProvider.GetService(pageType) as FrameworkElement;
    }
}