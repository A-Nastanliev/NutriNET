using System.Windows.Input;

namespace NutriNET.Maui.Views.Settings;

public partial class UserItemView : ContentView
{
	public UserItemView()
	{
		InitializeComponent();
	}

    public static readonly BindableProperty ActionCommandProperty =
      BindableProperty.Create(
          nameof(ActionCommand),
          typeof(ICommand),
          typeof(UserItemView),
          default(ICommand));

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public static readonly BindableProperty ActionCommandParameterProperty =
        BindableProperty.Create(
            nameof(ActionCommandParameter),
            typeof(object),
            typeof(UserItemView));

    public object ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    public static readonly BindableProperty NameTapCommandProperty =
        BindableProperty.Create(
            nameof(NameTapCommand),
            typeof(ICommand),
            typeof(UserItemView));

    public ICommand NameTapCommand
    {
        get => (ICommand)GetValue(NameTapCommandProperty);
        set => SetValue(NameTapCommandProperty, value);
    }

    public static readonly BindableProperty ButtonTextProperty =
        BindableProperty.Create(
            nameof(ButtonText),
            typeof(string),
            typeof(UserItemView),
            string.Empty);

    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }
}