#if ANDROID
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using AGColor = Android.Graphics.Color;

namespace NutriNET.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Protein", Exported = true)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/today_protein_widget_provider")]
    public class ProteinWidgetClass : AppWidgetProvider
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            var prefs = NutriWidgetPreferences.OpenPrefs();
            foreach (var id in appWidgetIds)
                appWidgetManager.UpdateAppWidget(id, BuildViews(context, prefs));
        }

        internal static RemoteViews BuildViews(Context context, ISharedPreferences prefs)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.today_protein_widget_layout);
            var lc = NutriWidgetPreferences.GetLocalizedContext(prefs, context);

            views.SetTextViewText(Resource.Id.protein_label, lc.GetString(Resource.String.protein));
            views.SetTextViewText(Resource.Id.protein_value,
                $"{Math.Round(NutriWidgetPreferences.GetProtein(prefs), 1)}{lc.GetString(Resource.String.g)}");
            views.SetTextColor(Resource.Id.protein_value, AGColor.ParseColor(NutriWidgetPreferences.GetProteinColor(prefs)));
            return views;
        }
    }
}
#endif
