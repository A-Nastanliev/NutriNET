using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Views.Authentication;
using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Maui.Alerts;

namespace NutriNET.Maui.ViewModels.Authentication
{
    public partial class ResetCodeVM : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        string code;

        [ObservableProperty]
        string newPassword;

        [ObservableProperty]
        string emailAddress;

        readonly UserClient _userClient;

        public ResetCodeVM(UserClient userClient)
        {
            _userClient = userClient;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue($"{nameof(EmailAddress)}", out var obj) && obj is string email)
            {
                EmailAddress = email;
                query.Clear();
            }
        }

        [RelayCommand]
        public async Task Submit()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;

            if(Code.Length< 6)
            {
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
                var result = await _userClient.ResetPasswordAsync(EmailAddress, Code, NewPassword);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, "OK");
                }
                else
                {
                    message = string.Format(LocalizationResourceManager.Instance["PasswordUpdated"].ToString());
                    _ = Toast.Make(message).Show();
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
            }
            catch(Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }
    }
}
