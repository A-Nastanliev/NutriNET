using NutriNET.Maui.Models.Food;
using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.ViewModels.Recipes;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace NutriNET.Maui.Views.Recipes;

public partial class RecipeFormPage : ContentPage
{
    FoodCatalogVM _catalogVM;
    RecipeFormVM _recipeFormVM;
    private CameraBarcodeReaderView? _scanner;
    private bool _isBottomSheetOpen;
    private bool _isProcessingScan;

    public RecipeFormPage(FoodCatalogVM catalogVM, RecipeFormVM vm)
	{
		InitializeComponent();
        FoodCatalog.BindingContext = catalogVM;
        catalogVM.OnSelectFood = async () => { };
        _catalogVM = catalogVM;
        BindingContext = vm;
        _recipeFormVM = vm;
	}

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _catalogVM.OnAppearingAsync();
    }

    private void BarcodeButton_Clicked(object sender, EventArgs e)
    {
        OpenBottomSheet(showBarcodeScanner: true);
    }

    private void SearchIngredientButton_Clicked(object sender, EventArgs e)
    {
        OpenBottomSheet(showBarcodeScanner: false);
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseBottomSheetAsync();
    }

    protected override async void OnDisappearing()
    {
        await CloseBottomSheetAsync();
        base.OnDisappearing();
    }


    private async Task CloseBottomSheetAsync()
    {
        if (!_isBottomSheetOpen)
            return;

        await Task.WhenAll(BottomSheetContent.FadeToAsync(0, 200), BottomSheetContent.AnimateBottomMargin(0, -40, 200));

        Overlay.InputTransparent = true;
        Overlay.IsVisible = false;
        _isBottomSheetOpen = false;

        FoodCatalog.IsVisible = false;
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

    private async void OpenBottomSheet(bool showBarcodeScanner)
    {
        if (_isBottomSheetOpen)
            return;

        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;
        _isBottomSheetOpen = true;

        BottomSheetContent.Opacity = 0;
        BottomSheetContent.Margin = new Thickness(0, 0, 0, -40);

        FoodCatalog.IsVisible = !showBarcodeScanner;
        BarcodeScannerHost.IsVisible = showBarcodeScanner;

        if (showBarcodeScanner)
            CreateBarcodeScanner();

        await Task.WhenAll(
            BottomSheetContent.FadeToAsync(1, 200),
            BottomSheetContent.AnimateBottomMargin(-40, 0, 200)
        );
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
        if (_isProcessingScan) return;

        var result = e.Results?.FirstOrDefault();
        if (result == null) return;

        _isProcessingScan = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                string barcode = result.Value?.Trim();
                if (!string.IsNullOrEmpty(barcode))
                {
                    await _catalogVM.HandleBarcodeAsync(barcode);
                    if (_catalogVM.SelectedFood?.Barcode == barcode)
                    {
                        await _recipeFormVM.SelectFood(_catalogVM.SelectedFood);
                    }
                }
            }
            finally
            {
                _catalogVM.SelectedFood = new FoodVM();
                _isProcessingScan = false;
            }
        });
    }
}