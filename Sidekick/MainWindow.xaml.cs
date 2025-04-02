using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Sidekick.ViewModels;

namespace Sidekick;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : IDisposable
{
    private bool _isWindowVisible;
    private Storyboard? _slideInAnimation;
    private Storyboard? _slideOutAnimation;
    private bool _isDisposed; // To detect redundant calls
    private double _originalWindowHeight;
    private bool _isInitialized;

    public MainWindow( ShellViewModel shellViewModel)
    {
        InitializeComponent();
        
        DataContext = shellViewModel ?? throw new ArgumentNullException(nameof(shellViewModel));

        Width = SystemParameters.PrimaryScreenWidth;
        _originalWindowHeight = Height;
        Left = 0;
        Top = 0;
        
        if (WindowTranslateTransform != null) 
        {
            WindowTranslateTransform.Y = -_originalWindowHeight;
        }
        
        Opacity = 0;
        IsHitTestVisible = false; // Start non-interactive

    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Window_Loaded fired.");
    }
    
    private void Window_ContentRendered(object sender, EventArgs e)
    {
        Debug.WriteLine("Window_ContentRendered fired.");
        // Ensure one-time setup happens after content is rendered
        EnsureInitialized();
    }
    
    private void EnsureInitialized()
    {
        if (_isInitialized) return; // Run only once

        Debug.WriteLine("--- EnsureInitialized START ---");
        SetupAnimations();
        
        if (new WindowInteropHelper(this).Handle == IntPtr.Zero) {
            Debug.WriteLine("WARNING: Handle still zero in EnsureInitialized! Hotkeys might fail.");
        } else {
            Debug.WriteLine("EnsureInitialized: Window handle confirmed.");
        }

        _isInitialized = true;
        Debug.WriteLine("--- EnsureInitialized END ---");
    }

    public void ToggleOverlayVisibility()
    {
        Debug.WriteLine($"ToggleOverlayVisibility called. _isWindowVisible = {_isWindowVisible}");
        
        EnsureInitialized();
        
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
        Debug.WriteLine("Enter ShowWindow method.");

        if (!_isInitialized)
        {
            Debug.WriteLine("ERROR: ShowWindow called before EnsureInitialized completed!"); return;
        }

        if (_slideInAnimation == null)
        {
            Debug.WriteLine("ERROR: _slideInAnimation is NULL in ShowWindow!"); return;
        } 
        
        Visibility = Visibility.Visible;

        try {
            Activate();
            Debug.WriteLine("ShowWindow: Activate() called.");
        } catch (InvalidOperationException ioex) {
            Debug.WriteLine($"ERROR calling Activate: {ioex.Message}");
            // If Activate fails consistently, the handle might *still* not be ready
            // despite ContentRendered. This points to deeper WPF lifecycle issues.
        }
        
        Opacity = 0; // Start transparent
        if (WindowTranslateTransform != null) { WindowTranslateTransform.Y = -_originalWindowHeight; } // Start off-screen
        Debug.WriteLine($"ShowWindow: State BEFORE animation: Opacity={Opacity}, TransformY={WindowTranslateTransform?.Y}");
        
        IsHitTestVisible = true;
        try
        {
            
            _slideInAnimation.Begin();
            Debug.WriteLine("ShowWindow: _slideInAnimation.Begin() called.");
        } catch(Exception ex) { Debug.WriteLine($"ERROR calling _slideInAnimation.Begin(): {ex.Message}"); }


        _isWindowVisible = true;
        Debug.WriteLine($"ShowWindow: _isWindowVisible set to {_isWindowVisible}");
    }
    private void HideWindow()
    {
        if (!_isInitialized) { Debug.WriteLine("ERROR: HideWindow called before EnsureInitialized completed!"); return; }
        if (_slideOutAnimation == null) { Debug.WriteLine("ERROR: _slideOutAnimation is NULL in HideWindow!"); return; }

        IsHitTestVisible = false;
        try
        {
            _slideOutAnimation.Begin();
        } catch (Exception ex) { Debug.WriteLine($"ERROR calling _slideOutAnimation.Begin(): {ex.Message}");}

        _isWindowVisible = false;
        Debug.WriteLine($"HideWindow: _isWindowVisible set to {_isWindowVisible}");
    }

    private void SlideOutAnimationCompleted(object sender, EventArgs e)
    {
        if (!_isWindowVisible)
        {
            IsHitTestVisible = false;
        }

        Debug.WriteLine("Slide out complete.");
    }

    private void SetupAnimations()
    {
        _slideInAnimation = (Storyboard)FindResource("SlideInAnimation");
        _slideOutAnimation = (Storyboard)FindResource("SlideOutAnimation");
        
        var slideInYAnimation = _slideInAnimation.Children[1] as DoubleAnimation;
        if (slideInYAnimation != null)
        {
            slideInYAnimation.From = -_originalWindowHeight;
            slideInYAnimation.To = 0;
        }
        
        var slideOutYAnimation = _slideOutAnimation.Children[1] as DoubleAnimation;
        if (slideOutYAnimation != null)
        {
            slideOutYAnimation.From = 0;
            slideOutYAnimation.To = -_originalWindowHeight;
        }
        
        _slideOutAnimation.Completed += SlideOutAnimationCompleted!;
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
                HotKeyManager.UnregisterAllHotkeys();

                // Remove event handlers to prevent memory leaks
                if (_slideOutAnimation != null)
                {
                    _slideOutAnimation.Completed -= SlideOutAnimationCompleted!;
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
