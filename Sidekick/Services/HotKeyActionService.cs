using System.Diagnostics;
using System.Windows;

namespace Sidekick.Services
{
    public class HotKeyActionService : IHotKeyActionService
    {
        private readonly INotificationService _notificationService;

        public HotKeyActionService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public void GenerateAndCopyGuidToClipboard()
        {
            Debug.WriteLine("CopyGuid action triggered via HotkeyActionsService.");
            try
            {
                var newGuid = Guid.NewGuid().ToString();

                // Action to perform clipboard operation on UI thread
                var copyAction = () => Clipboard.SetText(newGuid);

                if (Application.Current?.Dispatcher.CheckAccess() ?? true)
                {
                    copyAction(); // Already on UI thread
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(copyAction); // Dispatch
                }

                Debug.WriteLine($"Copied new GUID via service: {newGuid}");
                _notificationService.ShowNotification("GUID Copied", $"Copied {newGuid} to clipboard.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR copying GUID to clipboard via service: {ex.Message}");
                _notificationService.ShowNotification("Error", "Failed to copy GUID to clipboard.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
        }
    }
}