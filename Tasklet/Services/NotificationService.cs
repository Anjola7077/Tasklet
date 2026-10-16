using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace Tasklet.Services;

public sealed class NotificationService
{
    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            if (await LocalNotificationCenter.Current
                    .AreNotificationsEnabled())
            {
                return true;
            }

            return await LocalNotificationCenter.Current
                .RequestNotificationPermission();
        }
        catch
        {
            return false;
        }
    }

    public async Task ScheduleReminderAsync(
        int notificationId,
        string title,
        DateTime reminderDateTime)
    {
        if (reminderDateTime <= DateTime.Now)
            return;

        var notification = new NotificationRequest
        {
            NotificationId = notificationId,
            Title = "Tasklet reminder",
            Description = title,

            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = reminderDateTime
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }

    public void CancelReminder(int notificationId)
    {
        LocalNotificationCenter.Current.Cancel(notificationId);
    }
}