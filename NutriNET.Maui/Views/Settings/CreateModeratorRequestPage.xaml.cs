using NutriNET.Maui.ViewModels.Settings;

namespace NutriNET.Maui.Views.Settings;

public partial class CreateModeratorRequestPage : ContentPage
{
	public CreateModeratorRequestPage(CreateModeratorRequestVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}