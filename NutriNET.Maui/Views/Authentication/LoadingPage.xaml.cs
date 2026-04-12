using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Authentication;

namespace NutriNET.Maui.Views.Authentication;

public partial class LoadingPage : ContentPage
{
    readonly LoadingVM _vm;

    public LoadingPage(LoadingVM loadingVM)
    {
        InitializeComponent();
        BindingContext = loadingVM;
        _vm = loadingVM;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }
}