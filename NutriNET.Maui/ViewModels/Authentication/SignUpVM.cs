using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Views.Authentication;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using CommunityToolkit.Maui.Alerts;
using System.Text;
using NutriNET.Maui.Managers;

namespace NutriNET.Maui.ViewModels.Authentication
{
    public partial class SignUpVM : ObservableObject
    {
        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string emailAddress;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private ImageSource profileImage;

        string _selectedImagePath;

        private readonly UserClient _userClient;

        public SignUpVM(UserClient userClient)
        {
            _userClient = userClient;
        }


        [RelayCommand]
        private async Task PickProfilePicture()
        {
            var options = new MediaPickerOptions
            {
                Title = "Pick a profile picture",
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

            ProfileImage = ImageSource.FromFile(localFilePath);
        }

        [RelayCommand]
        private async Task SignUp()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(EmailAddress) || string.IsNullOrWhiteSpace(Password))
            {
                message = LocalizationResourceManager.Instance["AllFieldsRequiredMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message , ok);
                return;
            }

            if (!Validator.IsEmailValid(EmailAddress))
            {
                string title = LocalizationResourceManager.Instance["InvalidEmailTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidEmailMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message , ok);
                return;
            }

            Password = Password.Trim();
            if (!Validator.IsPasswordValid(Password))
            {
                string title = LocalizationResourceManager.Instance["InvalidPasswordTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidPasswordMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            if (_selectedImagePath == null)
            {
                message = LocalizationResourceManager.Instance["ProfilePictureRequired"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            Username = Username.Trim().Replace(" ", "");
            const int minUsernameLength = 4;
            if(Username.Length < minUsernameLength)
            {
                message = string.Format(LocalizationResourceManager.Instance["UsernameTooShort"].ToString(), minUsernameLength);
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            try
            {
                var result = await _userClient.SignUpAsync(Username, EmailAddress, Password, _selectedImagePath);

                if (!result.Success)
                {
                    string title = LocalizationResourceManager.Instance["SignUpFailedTitle"].ToString();
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(title, message, ok);
                    return;
                }

                message = string.Format(LocalizationResourceManager.Instance["Created"].ToString(), Username);
                _ = Toast.Make(message).Show();
                await GoToLogin();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        private async Task GoToLogin()
        {
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
        }
    }
}
