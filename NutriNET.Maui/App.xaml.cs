using Microsoft.Extensions.DependencyInjection;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.Resources.Styles.MacronutrientThemes;
using Syncfusion.Maui.Toolkit.Themes;
using System.Globalization;

namespace NutriNET.Maui
{
    public partial class App : Application
    {
        public static string? PendingDeepLink { get; set; }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            ImageManager.CleanupAllTempImages();

            var savedTheme = Preferences.Get("macro_theme", nameof(Default));

            ResourceDictionary newTheme = savedTheme switch
            {
                nameof(Default) => new Default(),
                nameof(Cyberpunk) => new Cyberpunk(),
                nameof(Aurora) => new Aurora(),
                nameof(Candy) => new Candy(),
                nameof(Lavender) => new Lavender(),
                nameof(Lava) => new Lava(),
                nameof(NeonRose) => new NeonRose(),
                nameof(Retro) => new Retro(),
                _ => new Default()
            };

            var existing = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d is Default);

            if (existing != null)
                Application.Current.Resources.MergedDictionaries.Remove(existing);
            Application.Current.Resources.MergedDictionaries.Add(newTheme);

            var savedLanguage = Preferences.Get("app_language", "en-US");
            LocalizationResourceManager.Instance.Culture = new CultureInfo(savedLanguage);

            var user = activationState?.Context?.Services?.GetService<UserVM>();

            var userClient = activationState?.Context?.Services?.GetService<UserClient>();
            if (userClient != null)
            {
                ApiErrorParser.Initialize(userClient.Logout, userClient.GetMyContextAsync);
            }

            if (user != null)
                Resources["User"] = user.PublicUser;

            return new Window(new AppShell());
        }
    }
}