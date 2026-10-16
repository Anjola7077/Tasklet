#if ANDROID
[assembly: Android.App.UsesPermission(
    Android.Manifest.Permission.PostNotifications)]
#endif

using Plugin.LocalNotification;
using Tasklet.Services;

namespace Tasklet;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseLocalNotification();

        builder.Services.AddSingleton<TaskStore>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}