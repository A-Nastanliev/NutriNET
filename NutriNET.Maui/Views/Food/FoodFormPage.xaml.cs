using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Xaml;
using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Food;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace NutriNET.Maui.Views.Food;

public partial class FoodFormPage : ContentPage
{
	FoodFormVM _vm;
    bool _isProcessingScan;
    bool _isScannerOpen;

    public FoodFormPage(FoodFormVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm = vm;
    }

    protected override async void OnDisappearing()
    {
        await CloseScannerAsync();  
        barcodeReaderView.Handler?.DisconnectHandler();
        base.OnDisappearing();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
    }

    private void barcodeReaderView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
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
                await CloseScannerAsync();
            }
            finally
            {
                _isProcessingScan = false;
            }
        });
    }

    private async void ScanButtonBase_Clicked(object sender, EventArgs e)
    {
        if (_isScannerOpen)
            return;

        barcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.Ean13 | BarcodeFormat.UpcA | BarcodeFormat.Ean8,
            AutoRotate = true,
            Multiple = false,
        };
        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;
        ScannerPanel.Opacity = 0;
        ScannerPanel.Margin = new Thickness(0, 0, 0, -40);
        _isScannerOpen = true;
        barcodeReaderView.CameraLocation = CameraLocation.Front;
        barcodeReaderView.CameraLocation = CameraLocation.Rear;
        barcodeReaderView.IsEnabled = true;
        barcodeReaderView.IsDetecting = true;
        await Task.WhenAll(ScannerPanel.FadeToAsync(1, 200),ScannerPanel.AnimateBottomMargin(-40, 0, 200));
    }


    private async Task CloseScannerAsync()
    {
        if (!_isScannerOpen)
            return;

        await Task.WhenAll(ScannerPanel.FadeToAsync(0, 200),ScannerPanel.AnimateBottomMargin(0, -40, 200));
        Overlay.InputTransparent = true;
        Overlay.IsVisible = false;
        barcodeReaderView.IsDetecting = false; 
        barcodeReaderView.IsEnabled = false;
        _isScannerOpen = false;
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseScannerAsync();
    }


}