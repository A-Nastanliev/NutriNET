using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Views.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.ViewModels.Authentication
{
    public partial class ForgotPasswordVM : ObservableObject
    {
        [ObservableProperty]
        string emailAddress;

        readonly UserClient _userClient;
        public ForgotPasswordVM(UserClient userClient)
        {
            _userClient = userClient;
        }

        [RelayCommand]
        public async Task SendCode()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;

            if (!Validator.IsEmailValid(EmailAddress) || string.IsNullOrWhiteSpace(EmailAddress))
            {
                string title = LocalizationResourceManager.Instance["InvalidEmailTitle"].ToString();
                message = LocalizationResourceManager.Instance["InvalidEmailMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
                return;
            }

            try
            {
                string language = Preferences.Get("app_language", "en-US");
                var result = await _userClient.ForgotPasswordAsync(EmailAddress, language);
                if (!result.Success)
                {
                    string title = LocalizationResourceManager.Instance["LoginFailedTitle"].ToString();
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(title, message, "OK");
                }
                else
                {
                    message = string.Format(LocalizationResourceManager.Instance["CodeSent"].ToString());
                    _ = Toast.Make(message).Show();
                    await Shell.Current.GoToAsync($"{nameof(ResetCodePage)}", true, new Dictionary<string, object> { [nameof(ResetCodeVM.EmailAddress)] = EmailAddress });
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }
    }
}
