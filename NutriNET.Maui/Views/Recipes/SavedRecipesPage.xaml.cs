using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class SavedRecipesPage : ContentPage
{
	SavedRecipesVM _vm;
	bool _appeared;

	public SavedRecipesPage(SavedRecipesVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm = vm;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
		if (!_appeared)
		{
			await _vm.Load();
			_appeared = true;
		}
    }
}