using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.ViewPager2.Widget;
using NutriNET.Maui.Managers;
using NutriNET.Maui.ViewModels.Recipes;
using NutriNET.Maui.Views.Recipes;
using NutriNET.Maui.Models.Food;
using static Android.Icu.Text.CaseMap;

namespace NutriNET.Maui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, Exported =true)]
    [IntentFilter(
        new[] { Intent.ActionView },
        AutoVerify =true,
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "https",
        DataHost = "",
        DataPathPrefix = "/recipe")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            if (intent?.Action != Intent.ActionView) return;
            var uriString = intent.DataString;
            if (string.IsNullOrWhiteSpace(uriString)) return;
            if (!RecipeShareTokenManager.TryParseToken(uriString, out string token)) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(300);
                await Shell.Current.GoToAsync($"{nameof(RecipeDetailPage)}?deepLinkToken={token}");
            });
        }

        protected override void AttachBaseContext(Context @base)
        {
            var configuration = new Android.Content.Res.Configuration(@base.Resources.Configuration);
            configuration.FontScale = 1.0f;

            var context = @base.CreateConfigurationContext(configuration);
            base.AttachBaseContext(context);
        }

        private static void HandleIntent(Intent? intent)
        {
            if (intent?.Action != Intent.ActionView) return;

            var uriString = intent.DataString;
            if (string.IsNullOrWhiteSpace(uriString)) return;

            App.PendingDeepLink = uriString;
        }
    }
}
