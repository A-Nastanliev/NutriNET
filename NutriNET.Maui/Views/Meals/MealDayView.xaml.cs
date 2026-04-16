using NutriNET.Maui.Views.Settings;
using System.Windows.Input;

namespace NutriNET.Maui.Views.Meals;

public partial class MealDayView : ContentView
{
    public static readonly BindableProperty MealTapCommandProperty =
        BindableProperty.Create(
            nameof(MealTapCommand),
            typeof(ICommand),
            typeof(MealDayView));

    public ICommand MealTapCommand
    {
        get => (ICommand)GetValue(MealTapCommandProperty);
        set => SetValue(MealTapCommandProperty, value);
    }


    public static readonly BindableProperty IsTodayProperty =
        BindableProperty.Create(
            nameof(IsToday),
            typeof(bool),
            typeof(MealDayView),
            false);

    public bool IsToday
    {
        get => (bool)GetValue(IsTodayProperty);
        set => SetValue(IsTodayProperty, value);
    }

    public MealDayView()
	{
		InitializeComponent();
	}
}