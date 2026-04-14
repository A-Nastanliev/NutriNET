using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class ProfilePage : ContentPage
{
	public ProfilePage(ProfileVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}