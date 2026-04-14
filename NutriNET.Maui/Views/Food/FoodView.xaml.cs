using System.Windows.Input;

namespace NutriNET.Maui.Views.Food;

public partial class FoodView : ContentView
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(FoodView));

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public static readonly BindableProperty HoldCommandProperty =
        BindableProperty.Create(
            nameof(HoldCommand),
            typeof(ICommand),
            typeof(FoodView));

    public ICommand HoldCommand
    {
        get => (ICommand)GetValue(HoldCommandProperty);
        set => SetValue(HoldCommandProperty, value);
    }

    public FoodView()
	{
		InitializeComponent();
	}
}