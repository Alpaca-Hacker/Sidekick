using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Sidekick;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    // --- Configuration (Hardcoded Default for now) ---
    private readonly Key _hotKey = Key.OemTilde; // Backtick/Tilde key (`)
    private readonly ModifierKeys _hotKeyModifiers = ModifierKeys.Alt;
    // ---------------------------------------------

    private bool _isWindowVisible = false;
    private Storyboard _fadeInAnimation;
    private Storyboard _fadeOutAnimation;
    private bool _isDisposed = false; // To detect redundant calls

    public MainWindow()
    {
        InitializeComponent();

        // Position window (e.g., top right corner) - Adjust as needed
        // Ensure Width/Height are set or SizeToContent is used appropriately
        this.Left = SystemParameters.WorkArea.Right - this.Width - 10;
        this.Top = 10;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Find animations defined in XAML
        _fadeInAnimation = (Storyboard)this.FindResource("FadeInAnimation");
        _fadeOutAnimation = (Storyboard)this.FindResource("FadeOutAnimation");
        _fadeOutAnimation.Completed += FadeOutAnimation_Completed;

        // --- Register Hotkey ---
        bool registered = HotKeyManager.RegisterHotKey(this, _hotKey, _hotKeyModifiers, ToggleWindowVisibility);
        if (!registered)
        {
            MessageBox.Show(
                $"Failed to register hotkey ({_hotKeyModifiers}+{_hotKey}). It might be in use by another application.",
                "Hotkey Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            // Consider closing the app or disabling hotkey feature
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey {_hotKeyModifiers}+{_hotKey} registered.");
        }

        // Ensure initial state (redundant with XAML but safe)
        this.Opacity = 0;
        this.IsHitTestVisible = false;
        _isWindowVisible = false;
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
        this.Activate();

        this.IsHitTestVisible = true; // Allow interaction BEFORE animation starts
        _fadeInAnimation.Begin(); // Start fade-in
        _isWindowVisible = true;
        System.Diagnostics.Debug.WriteLine("Showing window.");
    }

    private void HideWindow()
    {
        this.IsHitTestVisible = false; // Prevent interaction DURING animation
        _fadeOutAnimation.Begin(); // Start fade-out
        _isWindowVisible = false;
        System.Diagnostics.Debug.WriteLine("Hiding window.");
        // Note: IsHitTestVisible remains false after animation via Completed event
    }

    private void FadeOutAnimation_Completed(object sender, EventArgs e)
    {
        // Explicitly ensure it's not interactive AFTER hiding animation finishes
        // This is mainly a safeguard
        if (!_isWindowVisible)
        {
            this.IsHitTestVisible = false;
        }

        System.Diagnostics.Debug.WriteLine("Fade out complete.");
    }


    // --- Cleanup ---
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Dispose(); // Call Dispose on closing
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
                System.Diagnostics.Debug.WriteLine("Disposing MainWindow resources...");
                HotKeyManager.UnregisterHotKey();

                // Remove event handlers to prevent memory leaks
                if (_fadeOutAnimation != null)
                {
                    _fadeOutAnimation.Completed -= FadeOutAnimation_Completed;
                }
            }

            // Free unmanaged resources (unmanaged objects) and override a finalizer below.
            // (Not needed in this specific example, but good pattern)

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
