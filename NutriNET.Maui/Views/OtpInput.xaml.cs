namespace NutriNET.Maui.Views;

public partial class OtpInput : ContentView
{
    public static readonly BindableProperty CodeProperty =
        BindableProperty.Create(nameof(Code), typeof(string), typeof(OtpInput), "",
            propertyChanged: OnCodePropertyChanged);

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    private static void OnCodePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (OtpInput)bindable;
        if (string.IsNullOrEmpty((string)newValue))
            control.ClearBoxes();
    }

    private Entry[] _entries;

    public OtpInput()
    {
        InitializeComponent();
        _entries = new[] { Entry0, Entry1, Entry2, Entry3, Entry4, Entry5 };
    }

    public void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = (Entry)sender;
        var index = Array.IndexOf(_entries, entry);

        if (!string.IsNullOrEmpty(entry.Text) && !char.IsDigit(entry.Text[0]))
        {
            entry.Text = "";
            return;
        }

        if (!string.IsNullOrEmpty(entry.Text) && index < 5)
            _entries[index + 1].Focus();

        if (string.IsNullOrEmpty(entry.Text) && index > 0)
            _entries[index - 1].Focus();

        UpdateCode();
    }

    public void OnFocused(object sender, FocusEventArgs e)
    {
        var entry = (Entry)sender;
        entry.CursorPosition = 0;
        entry.SelectionLength = entry.Text?.Length ?? 0;
    }

    private void UpdateCode()
    {
        Code = string.Concat(_entries.Select(e => e.Text ?? ""));
    }

    private void ClearBoxes()
    {
        foreach (var entry in _entries)
            entry.Text = "";
    }
}