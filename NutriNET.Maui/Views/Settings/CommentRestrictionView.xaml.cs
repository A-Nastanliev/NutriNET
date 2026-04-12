using System.Windows.Input;

namespace NutriNET.Maui.Views.Settings;

public partial class CommentRestrictionView : ContentView
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(CommentRestrictionView));

    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public static readonly BindableProperty NameTapCommandProperty =
        BindableProperty.Create(
            nameof(NameTapCommand),
            typeof(ICommand),
            typeof(CommentRestrictionView));

    public ICommand NameTapCommand
    {
        get => (ICommand)GetValue(NameTapCommandProperty);
        set => SetValue(NameTapCommandProperty, value);
    }

    public CommentRestrictionView()
	{
		InitializeComponent();
	}
}