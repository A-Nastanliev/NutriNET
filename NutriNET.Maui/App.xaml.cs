using Microsoft.Extensions.DependencyInjection;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models.User;
using System.Globalization;

namespace NutriNET.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            ImageManager.CleanupAllTempImages();
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