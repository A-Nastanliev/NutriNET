using NutriNET.Maui.Views.Food;
using System.Windows.Input;

namespace NutriNET.Maui.Views.Recipes;

public partial class RecipeCatalogView : ContentView
{

    public static readonly BindableProperty SpanProperty =
        BindableProperty.Create(
            nameof(Span),
            typeof(int),
            typeof(RecipeCatalogView),
            2);

    public int Span
    {
        get => (int)GetValue(SpanProperty);
        set => SetValue(SpanProperty, value);
    }

    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(RecipeCatalogView),
            default(DataTemplate));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly BindableProperty ItemTapCommandProperty =
        BindableProperty.Create(
            nameof(ItemTapCommand),
            typeof(ICommand),
            typeof(RecipeCatalogView));

    public ICommand ItemTapCommand
    {
        get => (ICommand)GetValue(ItemTapCommandProperty);
        set => SetValue(ItemTapCommandProperty, value);
    }

    public static readonly BindableProperty CanSearchProperty =
    BindableProperty.Create(
        nameof(CanSearch),
        typeof(bool),
        typeof(RecipeCatalogView),
        false);

    public bool CanSearch
    {
        get => (bool)GetValue(CanSearchProperty);
        set => SetValue(CanSearchProperty, value);
    }

    public RecipeCatalogView()
	{
		InitializeComponent();
	}
}