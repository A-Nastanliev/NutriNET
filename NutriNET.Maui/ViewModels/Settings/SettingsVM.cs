using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.Resources.Languages;
using NutriNET.Maui.Resources.Styles.MacronutrientThemes;
using NutriNET.Maui.ViewModels;
using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Settings;
using Syncfusion.Maui.Toolkit.Themes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class SettingsVM : ObservableObject
    {
        [ObservableProperty]
        UserVM user;

        [ObservableProperty]
        bool isPictureChanged;

        [ObservableProperty]
        private ImageSource profileImageSource;

        string _selectedImagePath;

        [ObservableProperty]
        string oldPassword;

        [ObservableProperty]
        string newPassword;

        [ObservableProperty]
        string newUsername;

        [ObservableProperty]
        string newEmail;

        [ObservableProperty]
        string changeEmailPassword;

        [ObservableProperty]
        int followersCount;

        [ObservableProperty]
        int followingCount;

        [ObservableProperty]
        private int selectedLanguageIndex;

        [ObservableProperty]
        ObservableCollection<MacronutrientTheme> macroThemes = new();

        [ObservableProperty]
        MacronutrientTheme selectedMacroTheme;

        readonly UserClient _userClient;

        public SettingsVM(UserVM userVM, UserClient userClient) 
        {
            User = userVM;
            _userClient = userClient;

            var savedLanguage = Preferences.Get("app_language", "en-US");
            SelectedLanguageIndex = savedLanguage == "bg-BG" ? 1 : 0;

            macroThemes.Add(new MacronutrientTheme(nameof(Default), new Default()));
            macroThemes.Add(new MacronutrientTheme(nameof(Cyberpunk), new Cyberpunk()));
            macroThemes.Add(new MacronutrientTheme(nameof(Aurora), new Aurora()));
            macroThemes.Add(new MacronutrientTheme(nameof(Candy), new Candy()));
            macroThemes.Add(new MacronutrientTheme(nameof(Lavender), new Lavender()));
            macroThemes.Add(new MacronutrientTheme(nameof(Lava), new Lava()));
            var savedTheme = Preferences.Get("macro_theme", nameof(Default));
            selectedMacroTheme = MacroThemes.FirstOrDefault(t => t.Name == savedTheme) ?? MacroThemes[0];
            OnPropertyChanged(nameof(selectedMacroTheme));
  
        }

        partial void OnSelectedMacroThemeChanged(MacronutrientTheme value)
        {
            var existing = Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.ContainsKey("ProteinColor"));
            Application.Current.Resources.MergedDictionaries.Remove(existing);
            Application.Current.Resources.MergedDictionaries.Add(value.Theme);
            Preferences.Set("macro_theme", value.Name);
#if ANDROID
            Platforms.Android.NutriWidgetPreferences.SaveThemeAndRefresh(
                $"#{(int)(value.Protein.Alpha * 255):X2}{(int)(value.Protein.Red * 255):X2}{(int)(value.Protein.Green * 255):X2}{(int)(value.Protein.Blue * 255):X2}",
                $"#{(int)(value.Carbs.Alpha * 255):X2}{(int)(value.Carbs.Red * 255):X2}{(int)(value.Carbs.Green * 255):X2}{(int)(value.Carbs.Blue * 255):X2}",
                $"#{(int)(value.Fat.Alpha * 255):X2}{(int)(value.Fat.Red * 255):X2}{(int)(value.Fat.Green * 255):X2}{(int)(value.Fat.Blue * 255):X2}");
#endif
            WeakReferenceMessenger.Default.Send(new MacroThemeChanged());
        }

        partial void OnSelectedLanguageIndexChanged(int value)
        {
            CultureInfo culture = value == 0
                ? new CultureInfo("en-US")
                : new CultureInfo("bg-BG");

            LocalizationResourceManager.Instance.Culture = culture;

            Preferences.Set("app_language", culture.Name);

#if ANDROID
            try
            {
                var todayVM = IPlatformApplication.Current?.Services.GetService<ViewModels.Meals.TodayVM>();

                if (todayVM != null)
                {
                    Platforms.Android.NutriWidgetPreferences.SaveAndRefresh(
                        todayVM.MealDay.Calories,
                        todayVM.MealDay.Proteins,
                        todayVM.MealDay.Carbohydrates,
                        todayVM.MealDay.Fats);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsVM] Widget language refresh failed: {ex}");
            }
#endif
        }

        [RelayCommand]
        private async Task PickProfilePicture()
        {
            var options = new MediaPickerOptions
            {
                SelectionLimit = 1,
            };
            var results = await MediaPicker.Default.PickPhotosAsync(options);

            if (results == null || results.Count == 0)
                return;

            var result = results[0];

            await using var sourceStream = await result.OpenReadAsync();
            var localFilePath = await ImageManager.SaveTempImageAsync(sourceStream, Path.GetExtension(result.FileName));

            ImageManager.CleanupTempImage(_selectedImagePath);
            _selectedImagePath = localFilePath;

            ProfileImageSource = ImageSource.FromFile(localFilePath);
            IsPictureChanged = true;
        }

        [RelayCommand]
        public async Task ConfirmProfilePicture()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;
            try
            {
                var result = await _userClient.UpdateProfilePictureAsync(_selectedImagePath);

                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
                User.PublicUser.ProfilePictureSource = _userClient.GetProfilePicture(User.PublicUser.ProfilePicture);
                IsPictureChanged = false;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task CancelProfilePicture() 
        {
            ImageManager.CleanupTempImage(_selectedImagePath);
            _selectedImagePath = null;
            ProfileImageSource = User.PublicUser.ProfilePictureSource;
            IsPictureChanged = false;
        }

        [RelayCommand]
        public async Task Logout()
        {
            await _userClient.Logout();
        }

        [RelayCommand]
        public async Task DeleteAccount()
        {
            string message;
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            if (User.PublicUser.Role == UserRole.Administrator)
            {
                await Shell.Current.DisplayAlertAsync(LocalizationResourceManager.Instance["ActionNotAllowed"].ToString(),
                    LocalizationResourceManager.Instance["AdminCannotDeleteSelf"].ToString(), ok);
                return;
            }

            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            string title = string.Format(LocalizationResourceManager.Instance["ConfirmDeleteProgressTitle"].ToString(), 1, 2);
            message = LocalizationResourceManager.Instance["DeleteMyAccountConfirmMessage"].ToString();

            bool confirm = await Shell.Current.DisplayAlertAsync( title, message, accept, decline);

            if (!confirm)
                return;

            title = string.Format(LocalizationResourceManager.Instance["ConfirmDeleteProgressTitle"].ToString(), 2, 2);
            confirm = await Shell.Current.DisplayAlertAsync(title, message, accept, decline);

            if (!confirm)
                return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var result = await _userClient.DeleteSelfAsync();
                if (!result.Success) 
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["DeletedUser"].ToString(), User.PublicUser.Username);
                _ = Toast.Make(message, ToastDuration.Short).Show();
                await _userClient.Logout();              
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task UpdatePassword()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            OldPassword = OldPassword.Trim();
            if (!Validator.IsPasswordValid(OldPassword))
            {
                string title = LocalizationResourceManager.Instance["InvalidPasswordTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidPasswordMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            NewPassword = NewPassword.Trim();
            if (!Validator.IsPasswordValid(NewPassword))
            {
                string title = LocalizationResourceManager.Instance["InvalidPasswordTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidPasswordMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            try
            {
                var result = await _userClient.UpdatePasswordAsync(OldPassword, NewPassword);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = LocalizationResourceManager.Instance["PasswordUpdated"].ToString();
                _ = Toast.Make(message).Show();
                OldPassword = null;
                NewPassword = null;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task UpdateUsername()
        {
            if (string.IsNullOrWhiteSpace(NewUsername))
            {
                return;
            }

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            NewUsername = NewUsername.Trim().Replace(" ", "");
            const int minUsernameLength = 4;
            if (NewUsername.Length < minUsernameLength)
            {
                message = string.Format(LocalizationResourceManager.Instance["UsernameTooShort"].ToString(), minUsernameLength);
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            try
            {
                var result = await _userClient.UpdateUsernameAsync(NewUsername);

                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = LocalizationResourceManager.Instance["UsernameUpdated"].ToString();
                _ = Toast.Make(message).Show();
                NewUsername = null;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task UpdateEmail()
        {
            string message;
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            if (!Validator.IsEmailValid(NewEmail))
            {
                string title = LocalizationResourceManager.Instance["InvalidEmailTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidEmailMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var result = await _userClient.UpdateEmailAsync(NewEmail, ChangeEmailPassword);

                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = LocalizationResourceManager.Instance["EmailUpdated"].ToString();
                _ = Toast.Make(message).Show();
                NewEmail = null;
                ChangeEmailPassword = null;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public async Task OnAppearing()
        {
            try
            {
                FollowersCount = User.FollowerIds.Count;
                FollowingCount = User.FollowingIds.Count;
                NewUsername = null;
                NewEmail = null;
                NewPassword = null;
                ChangeEmailPassword = null;
                OldPassword = null;

                ProfileImageSource = User.PublicUser.ProfilePictureSource;

                var result = await _userClient.GetMyContextAsync();
                if (!result.Success)
                {
                    throw new Exception();
                }
            }
            catch (Exception ex) 
            {
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public async Task OnDisappearingAsync()
        {
            await CancelProfilePicture();
        }

        [RelayCommand]
        private async Task OpenFollowers()
        {
            await Shell.Current.GoToAsync(nameof(FollowersPage));
        }

        [RelayCommand]
        private async Task OpenFollowing()
        {
            await Shell.Current.GoToAsync(nameof(FollowingPage));
        }

        [RelayCommand]
        private async Task OpenCreateModeratorRequest()
        {
            await Shell.Current.GoToAsync(nameof(CreateModeratorRequestPage));
        }
    }
}
