using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Settings;

namespace NutriNET.Maui.Views.Settings;

public partial class FollowersPage : ContentPage
{
	FollowersVM _vm;

	public FollowersPage(FollowersVM vm)
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