using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Settings;

namespace NutriNET.Maui.Views.Settings;

public partial class ManageModeratorsPage : ContentPage
{
    ManageModeratorsVM _vm;

	public ManageModeratorsPage(ManageModeratorsVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm = vm;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

}