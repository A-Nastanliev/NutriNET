using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Xaml;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.ViewModels.Recipes;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace NutriNET.Maui.Views.Food;

public partial class FoodFormPage : ContentPage
{
	FoodFormVM _vm;
    private CameraBarcodeReaderView? _scanner;
    private bool _isBottomSheetOpen;
    private bool _isProcessingScan;

    public FoodFormPage(FoodFormVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm = vm;
    }

    protected override async void OnDisappearing()
    {
        await CloseBottomSheetAsync();
        base.OnDisappearing();
    }

    protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        await CloseBottomSheetAsync();
        base.OnNavigatedFrom(args);
    }

    private async Task CloseBottomSheetAsync()
    {
        if (!_isBottomSheetOpen)
            return;

        await Task.WhenAll(BottomSheetContent.FadeToAsync(0, 200), BottomSheetContent.AnimateBottomMargin(0, -40, 200));

        Overlay.InputTransparent = true;
        Overlay.IsVisible = false;
        _isBottomSheetOpen = false;

        BarcodeScannerHost.IsVisible = false;

        if (_scanner != null)
        {
            _scanner.IsDetecting = false;
            _scanner.BarcodesDetected -= BarcodeReader_BarcodesDetected;
            _scanner.Handler?.DisconnectHandler();
            BarcodeScannerHost.Children.Clear();
            _scanner = null;
        }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
    }

    private void CreateBarcodeScanner()
    {
        _scanner = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.Ean13 | BarcodeFormat.UpcA | BarcodeFormat.Ean8,
                AutoRotate = true,
                Multiple = false
            },
            CameraLocation = CameraLocation.Rear,
            IsDetecting = true,
            IsEnabled = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End
        };

        _scanner.BarcodesDetected += BarcodeReader_BarcodesDetected;

        BarcodeScannerHost.Children.Clear();
        BarcodeScannerHost.Children.Add(_scanner);
    }

    private void BarcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessingScan)
            return;

        var result = e.Results?.FirstOrDefault();
        if (result == null)
            return;

        _isProcessingScan = true;

        _vm.Food.Barcode = result.Value.Trim();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await CloseBottomSheetAsync();
            }
            finally
            {
                _isProcessingScan = false;
            }
        });
    }

    private async void ScanButtonBase_Clicked(object sender, EventArgs e)
    {
        if (_isBottomSheetOpen)
            return;

        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;
        _isBottomSheetOpen = true;

        BottomSheetContent.Opacity = 0;
        BottomSheetContent.Margin = new Thickness(0, 0, 0, -40);
        BarcodeScannerHost.IsVisible = true;

        CreateBarcodeScanner();

        await Task.WhenAll(
            BottomSheetContent.FadeToAsync(1, 200),
            BottomSheetContent.AnimateBottomMargin(-40, 0, 200)
        );
    }


    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseBottomSheetAsync();
    }


}