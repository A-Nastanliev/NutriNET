using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Authentication;

namespace NutriNET.Maui.Views.Authentication;

public partial class SignUpPage : ContentPage
{
	public SignUpPage(SignUpVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}