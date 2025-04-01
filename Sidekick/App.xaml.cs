﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sidekick.Services;
using Sidekick.ViewModels;

namespace Sidekick;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private TaskbarIcon? _notifyIcon;
    private MainWindow? _currentMainWindow;
    private List<HotkeyDefinition>? _hotkeyDefinitions;
    [Required]
    private IServiceProvider ServiceProvider { get; set; }
    private IConfiguration Configuration { get; set; }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        // --- Ensure only one instance runs (optional but recommended for tray apps) ---
        var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "Sidekick";
        _ = new Mutex(true, appName, out var createdNew);
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
        _hotkeyDefinitions = ServiceProvider.GetService<List<HotkeyDefinition>>(); 
        HotKeyManager.Initialize(_currentMainWindow);
        
        RegisterConfiguredHotkeys(ServiceProvider);
        
        var notificationService = ServiceProvider.GetService<INotificationService>() as NotificationService;
        notificationService?.Initialize(_notifyIcon);
        
        ShowStartupNotification();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);
        services.AddSingleton(Configuration.GetSection("Hotkeys")
            .Get<List<HotkeyDefinition>>() ?? new List<HotkeyDefinition>());
        
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IHotKeyActionService, HotKeyActionService>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<GuidGeneratorViewModel>();
        
        services.AddSingleton<MainWindow>();
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
            _currentMainWindow = ServiceProvider.GetService<MainWindow>();
        }
        
        _currentMainWindow?.ToggleOverlayVisibility(); 
    }
    
            private void RegisterConfiguredHotkeys(IServiceProvider serviceProvider)
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
            var hotkeyActions = serviceProvider.GetRequiredService<IHotKeyActionService>();

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
                            actionToRegister = async void () => {
                                Debug.WriteLine($"Async lambda wrapper triggered for '{definition.Name}'.");
                                try
                                {
                                    // Call the async method (using the interface)
                                    await hotkeyActions.GenerateAndPasteGuid();
                                    Debug.WriteLine($"Async call to GenerateAndPasteGuid completed for '{definition.Name}'.");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"ERROR executing '{definition.Name}' action from async lambda: {ex.Message}");
                                    
                                    // Maybe show error notification via service?
                                    // var notifier = serviceProvider.GetService<INotificationService>();
                                    // notifier?.ShowNotification("Hotkey Action Error", $"Failed action '{definition.Name}'.", BalloonIcon.Error);
                                }
                            };
                            break;

                        default:
                            Debug.WriteLine($"WARNING: Unknown hotkey action name '{definition.Name}'.");
                            break;
                    }

                    if (actionToRegister != null)
                    {
                        // Register the hotkey with its specific action
                        var registeredId = HotKeyManager.RegisterHotKey(key, modifiers, actionToRegister);
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
    

    // --- Application Exit Cleanup ---

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose(); // Ensure tray icon is removed on exit
        base.OnExit(e);
    }

}

