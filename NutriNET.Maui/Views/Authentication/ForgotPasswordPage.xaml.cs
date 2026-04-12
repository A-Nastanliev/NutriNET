using NutriNET.Maui.ViewModels.Authentication;

namespace NutriNET.Maui.Views.Authentication;

public partial class ForgotPasswordPage : ContentPage
{
	public ForgotPasswordPage(ForgotPasswordVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}