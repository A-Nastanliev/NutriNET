using CommunityToolkit.Maui;
using Microcharts.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.ViewModels.Authentication;
using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.ViewModels.Recipes;
using NutriNET.Maui.ViewModels.Settings;
using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Food;
using NutriNET.Maui.Views.Recipes;
using NutriNET.Maui.Views.Settings;
using Syncfusion.Maui.Toolkit.Hosting;
using ZXing.Net.Maui.Controls;

namespace NutriNET.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionToolkit()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<UserVM>();

            builder.Services.AddSingleton<ITokenStore, TokenStore>();
            builder.Services.AddTransient<AuthMessageHandler>();

            void AddApiClient<T>() where T : class
            {
                builder.Services
                    .AddHttpClient<T>(client =>
                    {
                        client.BaseAddress = new Uri("");
                    })
                    .AddHttpMessageHandler<AuthMessageHandler>();
            }

            AddApiClient<UserClient>();
            AddApiClient<FoodClient>();
            AddApiClient<MealClient>();
            AddApiClient<RecipeClient>();

            builder.Services.AddHttpClient<RefreshClient>(client =>
            {
                client.BaseAddress = new Uri("");
            });

            builder.Services.AddTransient<LoginVM>();
            builder.Services.AddTransient<LoginPage>();

            builder.Services.AddTransient<SignUpVM>();
            builder.Services.AddTransient<SignUpPage>();

            builder.Services.AddTransient<LoadingVM>();
            builder.Services.AddTransient<LoadingPage>();

            builder.Services.AddSingleton<SettingsVM>();
            builder.Services.AddSingleton<SettingsPage>();

            builder.Services.AddTransient<ForgotPasswordVM>();
            builder.Services.AddTransient<ForgotPasswordPage>();

            builder.Services.AddTransient<ResetCodeVM>();
            builder.Services.AddTransient<ResetCodePage>();

            builder.Services.AddTransient<FollowersVM>();
            builder.Services.AddTransient<FollowersPage>();

            builder.Services.AddTransient<FollowingVM>();
            builder.Services.AddTransient<FollowingPage>();

            builder.Services.AddTransient<CreateModeratorRequestVM>();
            builder.Services.AddTransient<CreateModeratorRequestPage>();

            builder.Services.AddSingleton<ModeratorRequestsVM>();
            builder.Services.AddSingleton<ModeratorRequestsPage>();

            builder.Services.AddSingleton<CommentRestrictionsVM>();
            builder.Services.AddSingleton<CommentRestrictionsPage>();

            builder.Services.AddSingleton<ManageModeratorsVM>();
            builder.Services.AddSingleton<ManageModeratorsPage>();

            builder.Services.AddTransient<FoodCatalogVM>();
            builder.Services.AddSingleton<FoodCatalogPage>();

            builder.Services.AddTransient<FoodFormVM>();
            builder.Services.AddTransient<FoodFormPage>();

            builder.Services.AddSingleton<MyFoodRequestsVM>();
            builder.Services.AddSingleton<MyFoodRequestsPage>();

            builder.Services.AddSingleton<FoodRequestsVM>();
            builder.Services.AddSingleton<FoodRequestsPage>();

            builder.Services.AddTransient<RecipeCatalogVM>();
            builder.Services.AddSingleton<RecipeCatalogPage>();

            builder.Services.AddSingleton<FollowingRecipesVM>();
            builder.Services.AddSingleton<FollowingRecipesPage>();

            builder.Services.AddTransient<MyRecipesVM>();
            builder.Services.AddSingleton<MyRecipesPage>();

            builder.Services.AddTransient<RecipeFormVM>();
            builder.Services.AddTransient<RecipeFormPage>();

            builder.Services.AddTransient<RecipeDetailVM>();
            builder.Services.AddTransient<RecipeDetailPage>();

            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ProfileVM>();

            builder.Services.AddSingleton<SavedRecipesVM>();
            builder.Services.AddSingleton<SavedRecipesPage>();

            builder.Services.AddTransient<RecipeListLoaderVM>();
            builder.Services.AddTransient<RecipeListPage>();

            return builder.Build();
        }
    }
}
