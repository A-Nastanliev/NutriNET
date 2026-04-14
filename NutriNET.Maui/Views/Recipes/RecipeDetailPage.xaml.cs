using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.ViewModels.Recipes;

namespace NutriNET.Maui.Views.Recipes;

public partial class RecipeDetailPage : ContentPage
{
    bool _isBottomSheetOpen;

    public RecipeDetailPage(RecipeDetailVM vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.OpenBottomSheet = OpenBottomSheet;
        vm.CloseBottomSheet = CloseBottomSheetAsync;
    }

    private async Task OpenBottomSheet()
    {
        if (_isBottomSheetOpen)
            return;

        Overlay.IsVisible = true;
        Overlay.InputTransparent = false;
        _isBottomSheetOpen = true;

        BottomSheetContent.Opacity = 0;
        BottomSheetContent.Margin = new Thickness(0, 0, 0, -40);

        await Task.WhenAll(
            BottomSheetContent.FadeToAsync(1, 200),
            BottomSheetContent.AnimateBottomMargin(-40, 0, 200)
        );
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

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
        EditCommentGrid.IsVisible = false; 
        RestrictUserGrid.IsVisible = false;
        EditCommentEditor.Text = string.Empty;
        RestrictionEditor.Text = string.Empty;
    }
}