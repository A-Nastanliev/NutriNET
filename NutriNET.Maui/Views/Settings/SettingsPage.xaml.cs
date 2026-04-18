using NutriNET.Maui.Managers;
using NutriNET.Maui.ViewModels;
using NutriNET.Maui.ViewModels.Settings;
using System.Diagnostics;
using System.Globalization;

namespace NutriNET.Maui.Views.Settings;

public partial class SettingsPage : ContentPage
{
	readonly SettingsVM _vm;

	public SettingsPage(SettingsVM vm)
	{
		InitializeComponent();
        var savedLanguage = Preferences.Get("app_language", "en-US");

        if (savedLanguage == "bg-BG")
            LanguagePicker.SelectedIndex = 1;
        else
            LanguagePicker.SelectedIndex = 0;

        BindingContext = vm;
		_vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
		_vm?.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = Task.Run(async () =>
        {
            try
            {
                await _vm.OnDisappearingAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in disappearing cleanup: {ex}");
            }
        });
    }

    private void LanguagePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (LanguagePicker.SelectedIndex == -1)
            return;

        CultureInfo culture;

        if (LanguagePicker.SelectedIndex == 0)
            culture = new CultureInfo("en-US");
        else 
            culture = new CultureInfo("bg-BG");

        LocalizationResourceManager.Instance.Culture = culture;

        Preferences.Set("app_language", culture.Name);

#if ANDROID
        try
        {
            var todayVM = IPlatformApplication.Current?.Services
                .GetService<ViewModels.Meals.TodayVM>();

            if (todayVM != null)
            {
                Platforms.Android.NutriWidgetPreferences.SaveAndRefresh
                    (todayVM.MealDay.Calories, todayVM.MealDay.Proteins,todayVM.MealDay.Carbohydrates,todayVM.MealDay.Fats);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SettingsPage] Widget language refresh failed: {ex}");
        }
#endif
    }
}