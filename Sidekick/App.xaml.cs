using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sidekick.ViewModels;

namespace Sidekick;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _notifyIcon;
    private MainWindow? _currentMainWindow;
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // --- Ensure only one instance runs (optional but recommended for tray apps) ---
        var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "Sidekick";
        bool createdNew;
        _ = new Mutex(true, appName, out createdNew);
        if (!createdNew)
        {
            // App is already running! Exiting.
            MessageBox.Show("Sidekick is already running.", "Sidekick", MessageBoxButton.OK, MessageBoxImage.Information);
            Application.Current.Shutdown();
            return;
        }

        base.OnStartup(e);

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
        
        _notifyIcon = (TaskbarIcon?)TryFindResource("AppTrayIcon");

        if (_notifyIcon == null)
        {
            Debug.WriteLine("ERROR: Could not find TaskbarIcon resource.");
            //Handle error?
        }
        
        _currentMainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        _currentMainWindow.Show();
        
        ShowStartupNotification();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);
        services.AddSingleton(Configuration.GetSection("HotKey").Get<HotKeySettings>() ?? new HotKeySettings());
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<GuidGeneratorViewModel>();
        
        services.AddTransient<MainWindow>();
    }
    
    private void ShowStartupNotification()
    {
        _notifyIcon?.ShowBalloonTip("Sidekick Running", "Sidekick is running in the background. Press the hotkey to open the window.", BalloonIcon.Info);
    }
    
    // --- Tray Icon Event Handlers ---

    private void ShowHideMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleMainWindow();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _notifyIcon?.Dispose(); // Clean up tray icon
        Application.Current.Shutdown();
    }

    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
    {
        // Optional: Toggle window on double-click
        ToggleMainWindow();
    }
    
    private void ToggleMainWindow()
    {
        if (_currentMainWindow == null)
        {
            // If MainWindow was closed somehow, maybe recreate? Or handle error.
            // For Singleton, we can try getting it again, though ideally it persists.
            _currentMainWindow = ServiceProvider?.GetService<MainWindow>();
        }

        // Call the public method on MainWindow
        _currentMainWindow?.ToggleOverlayVisibility(); 
    }

    // --- Application Exit Cleanup ---

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose(); // Ensure tray icon is removed on exit
        base.OnExit(e);
    }

}

