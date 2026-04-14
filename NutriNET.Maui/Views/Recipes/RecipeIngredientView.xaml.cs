using System.Windows.Input;

namespace NutriNET.Maui.Views.Recipes;

public partial class RecipeIngredientView : ContentView
{

    public static readonly BindableProperty EditCommandProperty =
        BindableProperty.Create(
            nameof(EditCommand),
            typeof(ICommand),
            typeof(RecipeIngredientView));

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public static readonly BindableProperty DeleteCommandProperty =
         BindableProperty.Create(
             nameof(DeleteCommand),
             typeof(ICommand),
             typeof(RecipeIngredientView));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public RecipeIngredientView()
	{
		InitializeComponent();
	}
}