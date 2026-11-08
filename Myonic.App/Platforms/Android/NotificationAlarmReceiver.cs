using Android.App;
using Android.Content;
using Myonic.Core.Models;

namespace Myonic.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificationAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        int id = intent.GetIntExtra("id", 0);
        string title = intent.GetStringExtra("title") ?? "Myonic";
        string body = intent.GetStringExtra("body") ?? string.Empty;
        var category = (NotificationCategory)intent.GetIntExtra("category", (int)NotificationCategory.General);

        AndroidNotificationHelper.ShowNotification(context, id, title, body, category);
    }
}
