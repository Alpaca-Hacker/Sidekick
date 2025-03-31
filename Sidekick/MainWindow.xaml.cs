using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Sidekick.ViewModels;

namespace Sidekick;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable
{
    

    private bool _isWindowVisible = false;
    private Storyboard _slideInAnimation;
    private Storyboard _slideOutAnimation;
    private bool _isDisposed = false; // To detect redundant calls
    private double _originalWindowHeight;

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

    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetupAnimations();
        
        // initial state
        Opacity = 0;
        IsHitTestVisible = false;
        _isWindowVisible = false;
    }
    
    private void Window_ContentRendered(object sender, EventArgs e)
    {
    }
    

    public void ToggleOverlayVisibility()
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
        
        _slideOutAnimation.Completed += SlideOutAnimationCompleted;
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
