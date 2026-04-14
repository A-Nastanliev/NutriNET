using NutriNET.Maui.Views.Settings;
using System.Windows.Input;

namespace NutriNET.Maui.Views.Food;

public partial class FoodRequestView : ContentView
{
    public static readonly BindableProperty TapCommandProperty =
      BindableProperty.Create(
          nameof(TapCommand),
          typeof(ICommand),
          typeof(FoodRequestView));

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public static readonly BindableProperty NameTapCommandProperty =
    BindableProperty.Create(
        nameof(NameTapCommand),
        typeof(ICommand),
        typeof(FoodRequestView));

    public ICommand NameTapCommand
    {
        get => (ICommand)GetValue(NameTapCommandProperty);
        set => SetValue(NameTapCommandProperty, value);
    }
    public FoodRequestView()
	{
		InitializeComponent();
	}
}