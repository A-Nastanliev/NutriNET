using NutriNET.Maui.ViewModels.Food;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace NutriNET.Maui.Views.Food;

public partial class MyFoodRequestsPage : ContentPage
{
	MyFoodRequestsVM _vm;
    CameraBarcodeReaderView? _scanner;
    bool _isSheetOpen;
    bool _isScannerOpen;
    bool _isProcessingScan;

    public MyFoodRequestsPage(MyFoodRequestsVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm = vm;
        _vm.OnRequestCreated = CloseBottomSheetAsync;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        await CloseBottomSheetAsync();
        base.OnDisappearing();
    }

    private async void PlusButtonBase_Clicked(object sender, EventArgs e)
    {
        if (_isSheetOpen)
            return;

        BottomSheetOverlay.IsVisible = true;
        BottomSheetOverlay.InputTransparent = false;
        _isSheetOpen = true;

        BottomSheetContent.Opacity = 0;
        BottomSheetContent.Margin = new Thickness(0, 0, 0, -40);

        await Task.WhenAll(
            BottomSheetContent.FadeToAsync(1, 200),
            BottomSheetContent.TranslateToAsync(0, 0, 0),
            BottomSheetContent.AnimateBottomMargin(-40, 0, 200)
        );
    }


    private void barcodeReaderView_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessingScan)
            return;

        var result = e.Results?.FirstOrDefault();
        if (result == null)
            return;

        _isProcessingScan = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                BarcodeEntry.Text = result.Value?.Trim();
                CloseScanner();
            }
            finally
            {
                _isProcessingScan = false;
            }
        });
    }

    private async Task CloseBottomSheetAsync()
    {
        ScannerHost.IsVisible = false;

        if (!_isSheetOpen)
            return;

        await Task.WhenAll(BottomSheetContent.FadeToAsync(0, 200), BottomSheetContent.AnimateBottomMargin( 0, -40, 200));
        CloseScanner();

        BottomSheetOverlay.InputTransparent = true;
        BottomSheetOverlay.IsVisible = false;

        _isSheetOpen = false;
    }

    private async void OnBottomSheetOverlayTapped(object sender, EventArgs e)
    {
        await CloseBottomSheetAsync();
    }

    private void ScanButtonBase_Clicked(object sender, EventArgs e)
    {
        if (_isScannerOpen)
        {
            CloseScanner();
            return;
        }

        ScannerHost.IsVisible = true;
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
            HeightRequest = 300,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End
        };

        _scanner.BarcodesDetected += barcodeReaderView_BarcodesDetected;

        ScannerHost.Children.Clear();
        ScannerHost.Children.Add(_scanner);
        _isScannerOpen = true;
    }

    public void CloseScanner()
    {
        if (_scanner != null)
        {
            _scanner.IsDetecting = false;
            _scanner.BarcodesDetected -= barcodeReaderView_BarcodesDetected;
            _scanner.Handler?.DisconnectHandler();
            ScannerHost.IsVisible = false;
            ScannerHost.Children.Clear();
            _scanner = null;
            _isScannerOpen = false;
        }
    }
}