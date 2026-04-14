using NutriNET.Maui.Views.Food;
using System.Windows.Input;

namespace NutriNET.Maui.Views.Recipes;

public partial class CommentView : ContentView
{

    public static readonly BindableProperty HoldCommandProperty =
        BindableProperty.Create(
            nameof(HoldCommand),
            typeof(ICommand),
            typeof(CommentView));

    public ICommand HoldCommand
    {
        get => (ICommand)GetValue(HoldCommandProperty);
        set => SetValue(HoldCommandProperty, value);
    }

    public static readonly BindableProperty NameTapCommandProperty =
        BindableProperty.Create(
            nameof(NameTapCommand),
            typeof(ICommand),
            typeof(CommentView));

    public ICommand NameTapCommand
    {
        get => (ICommand)GetValue(NameTapCommandProperty);
        set => SetValue(NameTapCommandProperty, value);
    }


    public CommentView()
	{
		InitializeComponent();
	}
}