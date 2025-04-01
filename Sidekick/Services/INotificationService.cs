using Hardcodet.Wpf.TaskbarNotification;

namespace Sidekick.Services;

    public interface INotificationService
    {
        void ShowNotification(string title, string message, BalloonIcon icon);
    }
