#if ANDROID
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using AGBitmap = Android.Graphics.Bitmap;
using AGCanvas = Android.Graphics.Canvas;
using AGColor = Android.Graphics.Color;
using AGPaint = Android.Graphics.Paint;
using AGRectF = Android.Graphics.RectF;

namespace NutriNET.Maui.Platforms.Android
{
    [BroadcastReceiver(Label = "Macros Summary", Exported = true)]
    [IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/today_macros_summary_widget_provider")]
    public class MacrosSummaryWidgetClass : AppWidgetProvider
    {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            var prefs = NutriWidgetPreferences.OpenPrefs();
            foreach (var id in appWidgetIds)
                appWidgetManager.UpdateAppWidget(id, BuildViews(context, prefs));
        }

        internal static RemoteViews BuildViews(Context context, ISharedPreferences prefs)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.today_macros_summary_widget_layout);
            var lc = NutriWidgetPreferences.GetLocalizedContext(prefs, context);

            var kcal = lc.GetString(Resource.String.kcal);
            var g = lc.GetString(Resource.String.g);
            var protein = NutriWidgetPreferences.GetProtein(prefs);
            var carbs = NutriWidgetPreferences.GetCarbs(prefs);
            var fat = NutriWidgetPreferences.GetFat(prefs);
            var calories = NutriWidgetPreferences.GetCalories(prefs);

            var proteinColor = AGColor.ParseColor(NutriWidgetPreferences.GetProteinColor(prefs));
            var carbsColor = AGColor.ParseColor(NutriWidgetPreferences.GetCarbsColor(prefs));
            var fatColor = AGColor.ParseColor(NutriWidgetPreferences.GetFatColor(prefs));

            views.SetTextViewText(Resource.Id.summary_calories_label, lc.GetString(Resource.String.calories));
            views.SetTextViewText(Resource.Id.summary_calories_value, $"{Math.Round(calories, 1)} {kcal}");

            views.SetTextViewText(Resource.Id.summary_protein_label, lc.GetString(Resource.String.protein));
            views.SetTextViewText(Resource.Id.summary_carbs_label, lc.GetString(Resource.String.carbs));
            views.SetTextViewText(Resource.Id.summary_fat_label, lc.GetString(Resource.String.fat));

            views.SetTextViewText(Resource.Id.summary_protein_value, $"{Math.Round(protein, 1)}{g}");
            views.SetTextViewText(Resource.Id.summary_carbs_value, $"{Math.Round(carbs, 1)}{g}");
            views.SetTextViewText(Resource.Id.summary_fat_value, $"{Math.Round(fat, 1)}{g}");

            views.SetTextColor(Resource.Id.summary_protein_value, proteinColor);
            views.SetTextColor(Resource.Id.summary_carbs_value, carbsColor);
            views.SetTextColor(Resource.Id.summary_fat_value, fatColor);

            views.SetImageViewBitmap(Resource.Id.summary_pie_chart,
                DrawDonutChart(protein, carbs, fat, 300, proteinColor, carbsColor, fatColor));


            return views;
        }

        private static AGBitmap DrawDonutChart(double protein, double carbs, double fat, int size,
              AGColor proteinColor, AGColor carbsColor, AGColor fatColor)
        {
            var bitmap = AGBitmap.CreateBitmap(size, size, AGBitmap.Config.Argb8888)!;
            var canvas = new AGCanvas(bitmap);

            float stroke = size * 0.18f;
            float inset = stroke / 2f + 2f;
            var rect = new AGRectF(inset, inset, size - inset, size - inset);

            var paint = new AGPaint { AntiAlias = true, StrokeWidth = stroke };
            paint.SetStyle(AGPaint.Style.Stroke);
            paint.StrokeCap = AGPaint.Cap.Butt;

            double proteinKcal = protein * 4;
            double carbsKcal = carbs * 4;
            double fatKcal = fat * 9;

            double total = proteinKcal + carbsKcal + fatKcal;

            if (total <= 0)
            {
                paint.Color = AGColor.ParseColor("#33FFFFFF");
                canvas.DrawOval(rect, paint);
                return bitmap;
            }

            float proteinSweep = Math.Max((float)(proteinKcal / total * 360), 0f);
            float carbsSweep = Math.Max((float)(carbsKcal / total * 360), 0f);
            float fatSweep = Math.Max((float)(fatKcal / total * 360), 0f);

            float startAngle = -90f;

            paint.Color = proteinColor;
            canvas.DrawArc(rect, startAngle, proteinSweep, false, paint);
            startAngle += proteinSweep;

            paint.Color = carbsColor;
            canvas.DrawArc(rect, startAngle, carbsSweep, false, paint);
            startAngle += carbsSweep;

            paint.Color = fatColor;
            canvas.DrawArc(rect, startAngle, fatSweep, false, paint);

            return bitmap;
        }
    }
}
#endif