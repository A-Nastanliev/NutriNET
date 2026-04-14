using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Food;
using NutriNET.Maui.Views.Recipes;
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
            Routing.RegisterRoute(nameof(RecipeFormPage), typeof(RecipeFormPage));
            Routing.RegisterRoute(nameof(RecipeDetailPage), typeof(RecipeDetailPage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(RecipeListPage), typeof(RecipeListPage));
            Routing.RegisterRoute(nameof(SavedRecipesPage), typeof(SavedRecipesPage));
        }
    }
}
