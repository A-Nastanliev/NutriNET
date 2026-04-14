using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class RecipeListPage : ContentPage
{
	public RecipeListPage(RecipeListLoaderVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}