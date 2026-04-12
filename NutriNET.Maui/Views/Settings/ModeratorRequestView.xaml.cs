using System.Windows.Input;

namespace NutriNET.Maui.Views.Settings;

public partial class ModeratorRequestView : ContentView
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(ModeratorRequestView));

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public static readonly BindableProperty NameTapCommandProperty =
        BindableProperty.Create(
            nameof(NameTapCommand),
            typeof(ICommand),
            typeof(ModeratorRequestView));

    public ICommand NameTapCommand
    {
        get => (ICommand)GetValue(NameTapCommandProperty);
        set => SetValue(NameTapCommandProperty, value);
    }

    public ModeratorRequestView()
	{
		InitializeComponent();
	}
}