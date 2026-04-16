using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.ViewModels.Meals;

namespace NutriNET.Maui.Views.Meals;

public partial class HistoryPage : ContentPage
{
	HistoryVM _vm;
    bool _isBottomSheetOpen;

	public HistoryPage(HistoryVM vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
        vm.OnSelectMeal = OpenBottomSheet;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
		await _vm.Load();
    }

    protected override async void OnDisappearing()
    {
        await CloseBottomSheetAsync();
        base.OnDisappearing();
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
}