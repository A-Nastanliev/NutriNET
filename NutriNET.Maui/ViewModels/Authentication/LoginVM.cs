using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Meals;
using NutriNET.Maui.Views.Settings;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace NutriNET.Maui.ViewModels.Authentication
{
    public partial class LoginVM : ObservableObject
    {
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        private readonly UserClient _userClient;

        public LoginVM(UserClient userClient)
        {
            _userClient = userClient;
        }

        [RelayCommand]
        private async Task Login()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                return;
            }

            if (!Validator.IsEmailValid(Email))
            {
                string title = LocalizationResourceManager.Instance["InvalidEmailTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidEmailMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            try
            {
                var result = await _userClient.EmailLoginAsync(Email, Password);

                if (!result.Success)
                {
                    string title = LocalizationResourceManager.Instance["LoginFailedTitle"].ToString();
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(title, message, "OK");
                    return;
                }

                await Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        private async Task GoToSignUp()
        {
            await Shell.Current.GoToAsync($"//{nameof(SignUpPage)}");
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            await Shell.Current.GoToAsync($"{nameof(ForgotPasswordPage)}");
        }
    }
}
