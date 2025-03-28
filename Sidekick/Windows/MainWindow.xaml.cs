using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Sidekick;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    private readonly HotKeySettings _hotkeySettings;

    private bool _isWindowVisible = false;
    private Storyboard _slideInAnimation;
    private Storyboard _slideOutAnimation;
    private bool _isDisposed = false; // To detect redundant calls

    public MainWindow(HotKeySettings hotKeySettings)
    {
        _hotkeySettings = hotKeySettings ?? throw new ArgumentNullException(nameof(hotKeySettings));
        
        InitializeComponent();
        
        // Position window (e.g., top right corner) - Adjust as needed
        // Ensure Width/Height are set or SizeToContent is used appropriately
        Left = SystemParameters.WorkArea.Left;
        Top = SystemParameters.WorkArea.Top;
        
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Find animations defined in XAML
        _slideInAnimation = (Storyboard)FindResource("SlideInAnimation");
        _slideOutAnimation = (Storyboard)FindResource("SlideOutAnimation");
        _slideOutAnimation.Completed += SlideOutAnimationCompleted;

        // Ensure initial state (redundant with XAML but safe)
        Opacity = 0;
        IsHitTestVisible = false;
        _isWindowVisible = false;
    }
    
    private void Window_ContentRendered(object sender, EventArgs e)
    {
        RegisterHotKey();
    }

    private void RegisterHotKey()
    {
        Debug.WriteLine($"Attempting to register hotkey from config: Key='{_hotkeySettings.Key}', Modifiers='{_hotkeySettings.Modifiers}'");
        
        if (HotKeyManager.TryParseHotkey(_hotkeySettings.Key, _hotkeySettings.Modifiers, out Key key, out ModifierKeys modifiers))
        {
            var registered = HotKeyManager.RegisterHotKey(this, key, modifiers, ToggleWindowVisibility);
            if (!registered)
            {
                MessageBox.Show($"Failed to register configured hotkey ({modifiers}+{key}). It might be in use by another application or invalid.",
                    "Hotkey Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Debug.WriteLine($"Hotkey {modifiers}+{key} registered successfully.");
            }
        }
        else
        {
            MessageBox.Show($"Invalid hotkey configuration in appsettings.json: Key='{_hotkeySettings.Key}', Modifiers='{_hotkeySettings.Modifiers}'. Using defaults or disabling.",
                "Hotkey Config Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            HotKeyManager.RegisterHotKey(this, Key.F12, ModifierKeys.Control | ModifierKeys.Shift, ToggleWindowVisibility);
        }
    }

    private void ToggleWindowVisibility()
    {
        if (_isWindowVisible)
        {
            HideWindow();
        }
        else
        {
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        // Bring window to front (optional, Topmost=True usually handles it)
        Activate();

        IsHitTestVisible = true; 
        _slideInAnimation.Begin(); 
        _isWindowVisible = true;
        Debug.WriteLine("Showing window.");
    }

    private void HideWindow()
    {
        IsHitTestVisible = false; // Prevent interaction DURING animation
        _slideOutAnimation.Begin();
        _isWindowVisible = false;
        Debug.WriteLine("Hiding window.");
        // Note: IsHitTestVisible remains false after animation via Completed event
    }

    private void SlideOutAnimationCompleted(object sender, EventArgs e)
    {
        if (!_isWindowVisible)
        {
            IsHitTestVisible = false;
        }

        Debug.WriteLine("Slide out complete.");
    }


    // --- Cleanup ---
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Dispose(); 
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects).
                Debug.WriteLine("Disposing MainWindow resources...");
                HotKeyManager.UnregisterHotKey();

                // Remove event handlers to prevent memory leaks
                if (_slideOutAnimation != null)
                {
                    _slideOutAnimation.Completed -= SlideOutAnimationCompleted;
                }
            }

            _isDisposed = true;
        }
    }

    // Optional Finalizer (only if you have unmanaged resources directly in this class)
    // ~MainWindow()
    // {
    //     // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //     Dispose(false);
    // }
}
