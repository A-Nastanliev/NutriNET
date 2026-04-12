using NutriNET.Maui.ViewModels.Authentication;

namespace NutriNET.Maui.Views.Authentication;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}