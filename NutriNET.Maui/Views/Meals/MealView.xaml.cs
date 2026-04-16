using System.Windows.Input;

namespace NutriNET.Maui.Views.Meals;

public partial class MealView : ContentView
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(MealView));

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public MealView()
	{
		InitializeComponent();
	}
}