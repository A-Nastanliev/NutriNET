namespace NutriNET.Maui.Views;

public partial class PasswordVisibilityToggle : ContentView
{
    public static readonly BindableProperty TargetEntryProperty =
        BindableProperty.Create(
            nameof(TargetEntry),
            typeof(Entry),
            typeof(PasswordVisibilityToggle),
            propertyChanged: OnTargetEntryChanged);

    public Entry TargetEntry
    {
        get => (Entry)GetValue(TargetEntryProperty);
        set => SetValue(TargetEntryProperty, value);
    }

    public PasswordVisibilityToggle()
    {
        InitializeComponent();
    }

    void OnClicked(object sender, EventArgs e)
    {
        if (TargetEntry == null)
            return;

        TargetEntry.IsPassword = !TargetEntry.IsPassword;
        UpdateIcon();
    }

    static void OnTargetEntryChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (PasswordVisibilityToggle)bindable;
        control.UpdateIcon();
    }

    void UpdateIcon()
    {
        if (TargetEntry == null)
            return;

        Icon.Source = TargetEntry.IsPassword ? "hide_password.png"  : "show_password.png";
    }
}