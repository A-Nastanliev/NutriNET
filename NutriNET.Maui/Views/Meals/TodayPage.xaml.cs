using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.ViewModels.Meals;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Meals;

public partial class TodayPage : ContentPage
{
	TodayVM _vm;
    FoodCatalogVM _catalogVM;
    MyRecipesVM _myRecipesVM;
    RecipeCatalogVM _recipeCatalogVM;
    bool _isBottomSheetOpen;
    CameraBarcodeReaderView? _scanner;
    bool _isProcessingScan;

    public TodayPage(TodayVM vm, FoodCatalogVM catalogVM, MyRecipesVM myRecipesVM, RecipeCatalogVM recipeCatalogVM)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm= vm;
        vm.OnSelectMeal = OpenBottomSheet;
        vm.OnDeselectMeal = CloseBottomSheetAsync;
        vm.CreateBarcodeScanner = CreateBarcodeScanner;
        FoodCatalog.BindingContext = catalogVM;
        catalogVM.OnSelectFood = async () => { };
        _catalogVM = catalogVM;
        _catalogVM.SelectedFood = new FoodVM();
        FoodCatalog.IsVisible = false;
        MyRecipeCatalog.BindingContext = myRecipesVM;
        _myRecipesVM = myRecipesVM;
        MyRecipeCatalog.IsVisible = false;
        RecipeCatalog.BindingContext = recipeCatalogVM;
        _recipeCatalogVM = recipeCatalogVM;
        RecipeCatalog.IsVisible = false;
        SavedRecipesCatalog.IsVisible = false;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
		await _vm.OnAppearing();
        await _catalogVM.OnAppearingAsync();
        await _recipeCatalogVM.Load();
        await _myRecipesVM.Load();
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
        FoodCatalog.IsVisible = false;
        BarcodeScannerHost.IsVisible = false;
        if (_scanner != null)
        {
            _scanner.IsDetecting = false;
            _scanner.BarcodesDetected -= barcodeReaderView_BarcodesDetected;
            _scanner.Handler?.DisconnectHandler();
            BarcodeScannerHost.Children.Clear();
            _scanner = null;
        }
        RecipeCatalog.IsVisible = false;
        MyRecipeCatalog.IsVisible = false;
        SavedRecipesCatalog.IsVisible = false;
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseBottomSheetAsync();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

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
                string barcode = result.Value?.Trim();
                await _catalogVM.HandleBarcodeAsync(barcode);
                if(_catalogVM?.SelectedFood?.Barcode == barcode)
                {
                    await _vm.SelectFood(_catalogVM.SelectedFood);
                }
            }
            finally
            {
                _catalogVM.SelectedFood = new FoodVM();
                _isProcessingScan = false;
            }
        });
    }

    public void CreateBarcodeScanner()
    {
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

        BarcodeScannerHost.Children.Clear();
        BarcodeScannerHost.Children.Add(_scanner);
    }
}