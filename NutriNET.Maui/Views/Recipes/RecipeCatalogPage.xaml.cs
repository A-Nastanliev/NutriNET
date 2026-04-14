using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class RecipeCatalogPage : ContentPage
{
	RecipeCatalogVM _vm;

	public RecipeCatalogPage(RecipeCatalogVM vm)
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