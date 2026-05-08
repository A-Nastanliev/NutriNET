#if ANDROID
using Android.Appwidget;
using Android.Content;
using AndroidApp = Android.App.Application;

namespace NutriNET.Maui.Platforms.Android
{
    public static class NutriWidgetPreferences
    {
        private const string PrefsName = "nutrinet_widget_prefs";
        private const string KeyDate = "date";
        private const string KeyCalories = "calories";
        private const string KeyProtein = "protein";
        private const string KeyCarbs = "carbs";
        private const string KeyFat = "fat";
        private const string KeyLanguage = "language";
        private const string KeyProteinColor = "protein_color";
        private const string KeyCarbsColor = "carbs_color";
        private const string KeyFatColor = "fat_color"; 

        public static void SaveAndRefresh(double calories, double protein, double carbs, double fat)
        {
            var context = AndroidApp.Context;
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            var editor = prefs.Edit()!;

            editor.PutString(KeyDate, DateTime.Now.ToString("yyyy-MM-dd"));
            editor.PutFloat(KeyCalories, (float)calories);
            editor.PutFloat(KeyProtein, (float)protein);
            editor.PutFloat(KeyCarbs, (float)carbs);
            editor.PutFloat(KeyFat, (float)fat);
            editor.PutString(KeyLanguage, Microsoft.Maui.Storage.Preferences.Get("app_language", "en-US"));

            editor.Apply();
            RefreshAll(context);
        }

        public static void SaveThemeAndRefresh(string proteinColor, string carbsColor, string fatColor)
        {
            var context = AndroidApp.Context;
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            var editor = prefs.Edit()!;

            editor.PutString(KeyProteinColor, proteinColor);
            editor.PutString(KeyCarbsColor, carbsColor);
            editor.PutString(KeyFatColor, fatColor);

            editor.Apply();
            RefreshAll(context);
        }

        public static bool IsToday(ISharedPreferences prefs)
            => prefs.GetString(KeyDate, "") == DateTime.Now.ToString("yyyy-MM-dd");


        public static string GetProteinColor(ISharedPreferences prefs) => prefs.GetString(KeyProteinColor, "#FFFF7A8A")!;
        public static string GetCarbsColor(ISharedPreferences prefs) => prefs.GetString(KeyCarbsColor, "#FF5BC0FF")!;
        public static string GetFatColor(ISharedPreferences prefs) => prefs.GetString(KeyFatColor, "#FFFFC857")!;

        public static double GetCalories(ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyCalories, 0f) : 0;
        public static double GetProtein(ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyProtein, 0f) : 0;
        public static double GetCarbs(ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyCarbs, 0f) : 0;
        public static double GetFat(ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyFat, 0f) : 0;

        public static Context GetLocalizedContext(ISharedPreferences prefs, Context context)
        {
            var lang = prefs.GetString(KeyLanguage, "en-US") ?? "en-US";
            var locale = Java.Util.Locale.ForLanguageTag(lang);
            var config = new global::Android.Content.Res.Configuration(context.Resources!.Configuration);
            config.SetLocale(locale);
            return context.CreateConfigurationContext(config);
        }

        public static ISharedPreferences OpenPrefs()
            => AndroidApp.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;

        private static void RefreshAll(Context context)
        {
            RefreshWidget<CaloriesWidgetClass>(context);
            RefreshWidget<ProteinWidgetClass>(context);
            RefreshWidget<CarbsWidgetClass>(context);
            RefreshWidget<FatWidgetClass>(context);
            RefreshWidget<MacrosSummaryWidgetClass>(context);
        }

        private static void RefreshWidget<T>(Context context) where T : AppWidgetProvider
        {
            var manager = AppWidgetManager.GetInstance(context)!;
            var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(T)));
            var ids = manager.GetAppWidgetIds(component);

            if (ids == null || ids.Length == 0)
                return;

            var intent = new Intent(AppWidgetManager.ActionAppwidgetUpdate, null, context, typeof(T));
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
        }
    }
}
#endif