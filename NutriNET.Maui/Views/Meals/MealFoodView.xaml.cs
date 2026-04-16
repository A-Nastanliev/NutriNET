using System.Windows.Input;

namespace NutriNET.Maui.Views.Meals;

public partial class MealFoodView : ContentView
{
    public static readonly BindableProperty EditCommandProperty =
    BindableProperty.Create(
        nameof(EditCommand),
        typeof(ICommand),
        typeof(MealFoodView));

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public static readonly BindableProperty DeleteCommandProperty =
     BindableProperty.Create(
         nameof(DeleteCommand),
         typeof(ICommand),
         typeof(MealFoodView));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public MealFoodView()
	{
		InitializeComponent();
	}
}