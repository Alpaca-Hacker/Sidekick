using System.Diagnostics;
using System.Windows;
using WindowsInput.Events;

namespace Sidekick.Services
{
    public class HotKeyActionService : IHotKeyActionService
    {
        private readonly INotificationService _notificationService;
        
        public HotKeyActionService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task GenerateAndPasteGuid()
        {
            try
            {
                var newGuid = Guid.NewGuid().ToString();
                var guidToPaste = newGuid;
                
                var copyAction = () => {
                    try {
                        Clipboard.SetText(guidToPaste);
                    } catch (Exception clipEx) {
                        Debug.WriteLine($"ERROR setting clipboard text: {clipEx.Message}");
                        throw; // Re-throw to be caught below
                    }
                };

                if (Application.Current?.Dispatcher.CheckAccess() ?? true)
                {
                    copyAction(); // Already on UI thread
                }
                else
                {
                    Application.Current.Dispatcher.InvokeAsync(copyAction); // Dispatch
                }
                
                
                // Issue here with Hotkeys as these are hard coded.
                await WindowsInput.Simulate.Events()
                    .Wait(150)
                    .Release(KeyCode.Control, KeyCode.Shift, KeyCode.G)
                    .ClickChord(KeyCode.Control, KeyCode.V)
                    .Invoke();
                
                _notificationService.ShowNotification("GUID Pasted", $"Copied {newGuid} to clipboard.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR copying GUID to clipboard via service: {ex.Message}");
                _notificationService.ShowNotification("Error", "Failed to copy GUID to clipboard.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
        }
    }
}