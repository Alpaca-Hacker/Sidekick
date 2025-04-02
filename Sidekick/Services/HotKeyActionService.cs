using System.Diagnostics;
using System.Windows;
using Microsoft.Extensions.Logging;
using WindowsInput.Events;

namespace Sidekick.Services
{
    public class HotKeyActionService : IHotKeyActionService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<HotKeyActionService> _logger;

        public HotKeyActionService(INotificationService notificationService, ILogger<HotKeyActionService> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
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
                        _logger.LogError("ERROR setting clipboard text: {ClipExMessage}", clipEx.Message);
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
                _logger.LogError("ERROR copying GUID to clipboard via service: {ExMessage}", ex.Message);
                _notificationService.ShowNotification("Error", "Failed to copy GUID to clipboard.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            }
        }
    }
}