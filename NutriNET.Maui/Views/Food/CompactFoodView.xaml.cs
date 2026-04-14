using System.Windows.Input;

namespace NutriNET.Maui.Views.Food;

public partial class CompactFoodView : ContentView
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(CompactFoodView));

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }
    public CompactFoodView()
	{
		InitializeComponent();
	}
}