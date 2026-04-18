#if ANDROID
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;

namespace NutriNET.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Calories", Exported = true)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/today_calories_widget_provider")]
    public class CaloriesWidgetClass : AppWidgetProvider
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            var prefs = NutriWidgetPreferences.OpenPrefs();
            foreach (var id in appWidgetIds)
                appWidgetManager.UpdateAppWidget(id, BuildViews(context, prefs));
        }

        internal static RemoteViews BuildViews(Context context, ISharedPreferences prefs)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.today_calories_widget_layout);
            views.SetTextViewText(Resource.Id.calories_label, NutriWidgetPreferences.GetString(prefs, "Calories"));
            var kcal = NutriWidgetPreferences.GetString(prefs, "kcal");
            views.SetTextViewText(Resource.Id.calories_value, $"{Math.Round(NutriWidgetPreferences.GetCalories(prefs), 1)} {kcal}");
            return views;
        }
    }
}
#endif
