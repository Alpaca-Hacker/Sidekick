using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
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
    private List<HotkeyDefinition>? _hotkeyDefinitions; 
    private IServiceProvider? _serviceProviderInternal;
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
        _serviceProviderInternal = ServiceProvider;
        
        _notifyIcon = (TaskbarIcon?)TryFindResource("AppTrayIcon");

        if (_notifyIcon == null)
        {
            Debug.WriteLine("ERROR: Could not find TaskbarIcon resource.");
            //Handle error?
        }
        
        _currentMainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        _hotkeyDefinitions = ServiceProvider.GetService<List<HotkeyDefinition>>(); 
        HotKeyManager.Initialize(_currentMainWindow);
        
        RegisterConfiguredHotkeys();
        
        //_currentMainWindow.Show();
        
        ShowStartupNotification();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);
        services.AddSingleton(Configuration.GetSection("Hotkeys")
            .Get<List<HotkeyDefinition>>() ?? new List<HotkeyDefinition>());
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
        
        _currentMainWindow?.ToggleOverlayVisibility(); 
    }
    
            private void RegisterConfiguredHotkeys()
        {
            if (_hotkeyDefinitions == null || !_hotkeyDefinitions.Any())
            {
                Debug.WriteLine("No hotkey definitions found in configuration.");
                return;
            }
            if (_currentMainWindow == null)
            {
                 Debug.WriteLine("ERROR: MainWindow instance is null, cannot register hotkeys.");
                 return; // Cannot proceed without window handle reference
            }

            Debug.WriteLine($"Found {_hotkeyDefinitions.Count} hotkey definitions. Registering...");

            foreach (var definition in _hotkeyDefinitions)
            {
                if (string.IsNullOrWhiteSpace(definition.Name) || string.IsNullOrWhiteSpace(definition.Key))
                {
                    Debug.WriteLine($"Skipping invalid hotkey definition (missing Name or Key).");
                    continue;
                }

                if (HotKeyManager.TryParseHotkey(definition.Key, definition.Modifiers, out Key key, out ModifierKeys modifiers))
                {
                    Action? actionToRegister = null;

                    // --- Map Action based on Name ---
                    switch (definition.Name.ToLowerInvariant())
                    {
                        case "togglewindow":
                            actionToRegister = () => _currentMainWindow?.ToggleOverlayVisibility(); // Use lambda
                            Debug.WriteLine($"Mapping action for '{definition.Name}'.");
                            break;

                        case "copyguid":
                            actionToRegister = GenerateAndCopyGuid; // Reference the method below
                            Debug.WriteLine($"Mapping action for '{definition.Name}'.");
                            break;

                        default:
                            Debug.WriteLine($"WARNING: Unknown hotkey action name '{definition.Name}'.");
                            break;
                    }

                    if (actionToRegister != null)
                    {
                        // Register the hotkey with its specific action
                        int registeredId = HotKeyManager.RegisterHotKey(key, modifiers, actionToRegister);
                        if (registeredId == 0)
                        {
                            Debug.WriteLine($"Failed to register hotkey '{definition.Name}' ({modifiers}+{key}).");
                            // Maybe show a warning to the user?
                        }
                        // No need to store the ID here unless needed for individual unregistration elsewhere
                    }
                }
                else
                {
                    Debug.WriteLine($"Failed to parse hotkey definition for '{definition.Name}': Key='{definition.Key}', Modifiers='{definition.Modifiers}'.");
                }
            }
        }
        private void GenerateAndCopyGuid()
        {
            // This method will be called when the "CopyGuid" hotkey is pressed
            Debug.WriteLine("CopyGuid hotkey action triggered.");
            try
            {
                var newGuid = Guid.NewGuid().ToString();
                // WPF UI thread is STA, Clipboard access should be okay directly
                Clipboard.SetText(newGuid);
                Debug.WriteLine($"Copied new GUID: {newGuid}");

                // Show feedback notification
                ShowTrayNotification("GUID Copied", $"Copied {newGuid} to clipboard.", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR copying GUID to clipboard: {ex.Message}");
                ShowTrayNotification("Error", "Failed to copy GUID to clipboard.", BalloonIcon.Error);
            }
        }
        
        private void ShowTrayNotification(string title, string message, BalloonIcon icon)
        {
            // Use Dispatcher if calling from non-UI thread (not needed here, but good practice)
            Dispatcher.Invoke(() => {
            _notifyIcon?.ShowBalloonTip(title, message, icon);
            });
        }

    // --- Application Exit Cleanup ---

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose(); // Ensure tray icon is removed on exit
        base.OnExit(e);
    }

}

