using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;
using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Food;
using Syncfusion.Maui.Toolkit.BottomSheet;
using System.Security.Cryptography;
using ZXing.Net.Maui;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using ZXing.Net.Maui.Controls;

namespace NutriNET.Maui.Views.Food;

public partial class FoodCatalogPage : ContentPage
{
    bool _isScannerOpen;
    bool _isProcessingScan;
    bool _isFoodSheetOpen;
    CameraBarcodeReaderView? _scanner;
    FoodCatalogVM _vm;

	public FoodCatalogPage(FoodCatalogVM foodCatalogVM)
	{
		InitializeComponent();
		_vm = foodCatalogVM;
        FoodCatalog.BindingContext = foodCatalogVM;
        BindingContext = foodCatalogVM;
        foodCatalogVM.OnSelectFood = OpenFoodSheetAsync;
        foodCatalogVM.OnDeselectFood = CloseFoodSheetAsync;
        PlusButtonBase.Command = new Command(async () =>
        {
            await Shell.Current.GoToAsync(nameof(FoodFormPage));
        });
    
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }

    private async Task OpenFoodSheetAsync()
    {
        if (_isFoodSheetOpen)
            return;

        FoodSheetOverlay.IsVisible = true;
        FoodSheetOverlay.InputTransparent = false;
        _isFoodSheetOpen = true;

        FoodSheetContent.Opacity = 0;
        FoodSheetContent.Margin = new Thickness(0, 0, 0, -40);

        await Task.WhenAll(
            FoodSheetContent.FadeToAsync(1, 200),
            FoodSheetContent.TranslateToAsync(0, 0, 0),
            FoodSheetContent.AnimateBottomMargin(-40, 0, 200)
        );
    }

    private async Task CloseFoodSheetAsync()
    {
        if (!_isFoodSheetOpen)
            return;

        await Task.WhenAll(FoodSheetContent.FadeToAsync(0, 200), FoodSheetContent.AnimateBottomMargin(0, -40, 200));

        FoodSheetOverlay.InputTransparent = true;
        FoodSheetOverlay.IsVisible = false;

        _isFoodSheetOpen = false;
    }

    private async void OnFoodSheetOverlayTapped(object sender, EventArgs e)
    {
        await CloseFoodSheetAsync();
    }

    private async void ScanButtonBase_Clicked(object sender, EventArgs e)
    {
        if (_isScannerOpen)
            return;

        _scanner = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.Ean13 | BarcodeFormat.UpcA | BarcodeFormat.Ean8,
                AutoRotate = true,
                Multiple = false,
            },
            CameraLocation = CameraLocation.Rear,
            IsDetecting = true,
            IsEnabled = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End
        };

        _scanner.BarcodesDetected += barcodeReaderView_BarcodesDetected;

        ScannerHost.Children.Clear();
        ScannerHost.Children.Add(_scanner);
        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;
        _isScannerOpen = true;
        ScannerPanel.Opacity = 0;
        ScannerPanel.Margin = new Thickness(0, 0, 0, -40);
        await Task.WhenAll(ScannerPanel.FadeToAsync(1, 200), ScannerPanel.AnimateBottomMargin(-40, 0, 200));
    }


    private async Task CloseScannerAsync()
    {
        if (!_isScannerOpen)
            return;

        await Task.WhenAll(ScannerPanel.FadeToAsync(0, 200), ScannerPanel.AnimateBottomMargin(0, -40, 200));

        if (_scanner != null)
        {
            _scanner.IsDetecting = false;
            _scanner.BarcodesDetected -= barcodeReaderView_BarcodesDetected;
            _scanner.Handler?.DisconnectHandler();

            ScannerHost.Children.Clear();
            _scanner = null;
        }

        Overlay.InputTransparent = true;
        Overlay.IsVisible = false;

        _isScannerOpen = false;
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseScannerAsync();
    }

    private void barcodeReaderView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessingScan)
            return;

        var result = e.Results?.FirstOrDefault();
        if (result == null)
            return;

        _isProcessingScan = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            { 
                await CloseScannerAsync();
                await _vm.HandleBarcodeAsync(result.Value?.Trim());
            }
            finally
            {
                _isProcessingScan = false;
            }
        });
    }

    protected override async void OnDisappearing()
    {
        await CloseScannerAsync();
        base.OnDisappearing();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }
}