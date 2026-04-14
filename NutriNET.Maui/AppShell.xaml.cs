using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Food;
using NutriNET.Maui.Views.Settings;

namespace NutriNET.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(ResetCodePage), typeof(ResetCodePage));
            Routing.RegisterRoute(nameof(FollowersPage), typeof(FollowersPage));
            Routing.RegisterRoute(nameof(FollowingPage), typeof(FollowingPage));
            Routing.RegisterRoute(nameof(CreateModeratorRequestPage), typeof(CreateModeratorRequestPage));
            Routing.RegisterRoute(nameof(FoodFormPage), typeof(FoodFormPage));
        }
    }
}
