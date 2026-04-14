using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class MyRecipesPage : ContentPage
{
    MyRecipesVM _vm;

    public MyRecipesPage(MyRecipesVM vm)
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