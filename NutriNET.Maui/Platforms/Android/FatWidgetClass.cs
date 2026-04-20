#if ANDROID
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace NutriNET.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Fat", Exported = true)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/today_fat_widget_provider")]
    public class FatWidgetClass : AppWidgetProvider
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            var prefs = NutriWidgetPreferences.OpenPrefs();
            foreach (var id in appWidgetIds)
                appWidgetManager.UpdateAppWidget(id, BuildViews(context, prefs));
        }

        internal static RemoteViews BuildViews(Context context, ISharedPreferences prefs)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.today_fat_widget_layout);
            var lc = NutriWidgetPreferences.GetLocalizedContext(prefs, context);

            views.SetTextViewText(Resource.Id.fat_label, lc.GetString(Resource.String.fat));
            views.SetTextViewText(Resource.Id.fat_value,
                $"{Math.Round(NutriWidgetPreferences.GetFat(prefs), 1)}{lc.GetString(Resource.String.g)}");

            return views;
        }
    }
}
#endif
