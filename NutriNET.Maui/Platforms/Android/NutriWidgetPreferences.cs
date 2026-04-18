#if ANDROID
using Android.Appwidget;
using Android.Content;
using AndroidApp = Android.App.Application;

namespace NutriNET.Maui.Platforms.Android
{
    public static class NutriWidgetPreferences
    {
        private const string PrefsName   = "nutrinet_widget_prefs";
        private const string KeyDate     = "date";
        private const string KeyCalories = "calories";
        private const string KeyProtein  = "protein";
        private const string KeyCarbs    = "carbs";
        private const string KeyFat      = "fat";
        private const string KeyLanguage = "language";

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            ["en-US"] = new()
            {
                ["Calories"] = "Calories",
                ["Protein"] = "Protein",
                ["Carbs"] = "Carbs",
                ["Fat"]  = "Fat",
                ["kcal"] = "kcal",
                ["g"] = "g",
            },
            ["bg-BG"] = new()
            {
                ["Calories"] = "Калории",
                ["Protein"] = "Белтъци",
                ["Carbs"] = "Въглехидрати",
                ["Fat"] = "Мазнини",
                ["kcal"] = "ккал",
                ["g"] = "г",
            },
        };

        public static void SaveAndRefresh(double calories, double protein, double carbs, double fat)
        {
            var context = AndroidApp.Context;
            var prefs   = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            var editor  = prefs.Edit()!;

            editor.PutString(KeyDate,    DateTime.Now.ToString("yyyy-MM-dd"));
            editor.PutFloat(KeyCalories, (float)calories);
            editor.PutFloat(KeyProtein,  (float)protein);
            editor.PutFloat(KeyCarbs,    (float)carbs);
            editor.PutFloat(KeyFat,      (float)fat);

            var lang = Microsoft.Maui.Storage.Preferences.Get("app_language", "en-US");
            editor.PutString(KeyLanguage, lang);

            editor.Apply();
            RefreshAll(context);
        }

        public static void ClearAndRefresh()
        {
            var context = AndroidApp.Context;
            var prefs = context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
            var editor = prefs.Edit()!;

            editor.PutString(KeyDate, "");
            editor.PutFloat(KeyCalories, 0f);
            editor.PutFloat(KeyProtein, 0f);
            editor.PutFloat(KeyCarbs, 0f);
            editor.PutFloat(KeyFat, 0f);
            editor.Apply();

            RefreshAll(context);
        }

        public static bool IsToday(ISharedPreferences prefs)
            => prefs.GetString(KeyDate, "") == DateTime.Now.ToString("yyyy-MM-dd");

        public static double GetCalories (ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyCalories, 0f) : 0;
        public static double GetProtein (ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyProtein, 0f) : 0;
        public static double GetCarbs (ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyCarbs, 0f) : 0;
        public static double GetFat (ISharedPreferences prefs) => IsToday(prefs) ? prefs.GetFloat(KeyFat, 0f) : 0;

        public static ISharedPreferences OpenPrefs()
            => AndroidApp.Context.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;

        public static string GetString(ISharedPreferences prefs, string key)
        {
            var lang = prefs.GetString(KeyLanguage, "en-US") ?? "en-US";
            if (!Strings.TryGetValue(lang, out var table))
                table = Strings["en-US"];
            return table.TryGetValue(key, out var val) ? val : key;
        }

        private static void RefreshAll(Context context)
        {
            RefreshWidget<CaloriesWidgetClass>(context);
            RefreshWidget<ProteinWidgetClass>(context);
            RefreshWidget<CarbsWidgetClass>(context);
            RefreshWidget<FatWidgetClass>(context);
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
