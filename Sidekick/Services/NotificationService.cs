using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Diagnostics;

namespace Sidekick.Services;

    public class NotificationService : INotificationService
    {
        private TaskbarIcon? _notifyIconRef;
        private bool _isInitialized;

        // Initialize with the actual icon reference after it's created
        public void Initialize(TaskbarIcon? notifyIcon)
        {
            _notifyIconRef = notifyIcon;
            if (_notifyIconRef != null)
            {
                _isInitialized = true;
                // Optionally hook into an event like TrayBalloonTipShown/Closed/Clicked if needed
            } else {
                Debug.WriteLine("WARNING: NotificationService Initialize called with null TaskbarIcon.");
            }
        }

        public void ShowNotification(string title, string message, BalloonIcon icon)
        {
            if (!_isInitialized || _notifyIconRef == null)
            {
                Debug.WriteLine($"NotificationService: Cannot show notification '{title}'. Not initialized or icon is null.");
                return;
            }
            
            Action showAction = () =>
            {
                try
                {
                    _notifyIconRef.ShowBalloonTip(title, message, icon);
                }
                catch (Exception ex)
                {
                    // Catch potential errors if the icon isn't fully ready
                    Debug.WriteLine($"ERROR showing balloon tip '{title}': {ex.Message}");
                }
            };

            if (Application.Current?.Dispatcher.CheckAccess() ?? true) { showAction(); }
            else { Application.Current.Dispatcher.Invoke(showAction); }
        }
    }
