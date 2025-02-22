using Microsoft.UI.Xaml;
using MI_GUI_WinUI.Models;
using MI_GUI_WinUI.ViewModels;
using MI_GUI_WinUI.Services;
using MI_GUI_WinUI.Pages;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MI_GUI_WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private readonly WindowManager _windowManager;
    private bool _isClosing;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        _windowManager = new WindowManager();

        // Configure services
        var services = new ServiceCollection();

        // Register window management
        services.AddSingleton(_windowManager);

        // Register navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Register view models
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SelectProfilesViewModel>();
        services.AddSingleton<ActionStudioViewModel>();
        services.AddSingleton<IconStudioViewModel>();
        services.AddSingleton<ProfileEditorViewModel>();

        // Register pages
        services.AddTransient<SelectProfilesPage>();
        services.AddTransient<ActionStudioPage>();
        services.AddTransient<IconStudioPage>();
        services.AddTransient<ProfileEditorPage>();

        // Build and configure services
        var serviceProvider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(serviceProvider);

        this.UnhandledException += App_UnhandledException;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Ensure we have a window created when the app launches
        if (_windowManager.MainWindow == null && !_isClosing)
        {
            _windowManager.InitializeMainWindow();
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception
        e.Handled = true;
    }

    public new void Exit()
    {
        _isClosing = true;
        base.Exit();
    }
}
