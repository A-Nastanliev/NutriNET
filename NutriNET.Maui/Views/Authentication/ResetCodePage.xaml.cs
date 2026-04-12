using NutriNET.Maui.ViewModels.Authentication;

namespace NutriNET.Maui.Views.Authentication;

public partial class ResetCodePage : ContentPage
{
	public ResetCodePage(ResetCodeVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}