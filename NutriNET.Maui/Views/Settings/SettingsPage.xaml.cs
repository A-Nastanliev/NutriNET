using NutriNET.Maui.ViewModels.Settings;
using System.Diagnostics;
using System.Globalization;

namespace NutriNET.Maui.Views.Settings;

public partial class SettingsPage : ContentPage
{
	readonly SettingsVM _vm;
    bool _isBottomSheetOpen;

    public SettingsPage(SettingsVM vm)
	{
		InitializeComponent();
        BindingContext = vm;
		_vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
		_vm?.OnAppearing();
    }

    protected override async void OnDisappearing()
    {
        await CloseBottomSheetAsync();
        base.OnDisappearing();
        _ = Task.Run(async () =>
        {
            try
            {
                await _vm.OnDisappearingAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in disappearing cleanup: {ex}");
            }
        });
    }

    public async Task OpenBottomSheet()
    {
        if (_isBottomSheetOpen)
            return;

        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;
        _isBottomSheetOpen = true;
        BottomSheetContent.Opacity = 0;
        BottomSheetContent.Margin = new Thickness(0, 0, 0, -40);
        await Task.WhenAll(BottomSheetContent.FadeToAsync(1, 200), BottomSheetContent.AnimateBottomMargin(-40, 0, 200));
    }

    private async Task CloseBottomSheetAsync()
    {
        if (!_isBottomSheetOpen)
            return;

        await Task.WhenAll(BottomSheetContent.FadeToAsync(0, 200), BottomSheetContent.AnimateBottomMargin(0, -40, 200));
        Overlay.InputTransparent = true;
        Overlay.IsVisible = false;
        _isBottomSheetOpen = false;
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseBottomSheetAsync();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }

    private async void MacronutrientTheme_Tapped(object sender, TappedEventArgs e)
    {
        await OpenBottomSheet();
    }
}