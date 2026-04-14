using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class FollowingRecipesPage : ContentPage
{
    FollowingRecipesVM _vm;

    public FollowingRecipesPage(FollowingRecipesVM vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _vm.Load();
    }
}