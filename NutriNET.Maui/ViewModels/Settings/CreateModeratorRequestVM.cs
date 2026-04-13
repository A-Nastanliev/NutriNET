using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class CreateModeratorRequestVM : ObservableObject
    {
        [ObservableProperty]
        string description;

        readonly UserClient _userClient;

        public CreateModeratorRequestVM(UserClient userClient) 
        {
            _userClient = userClient;
        }

        [RelayCommand]
        public async Task SubmitRequestAsync()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;
            const int minLength = 10;

            if (string.IsNullOrWhiteSpace(Description))
            {
                message = string.Format(LocalizationResourceManager.Instance["ModeratorRequestValidation"].ToString(), minLength);
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            Description = Description.Trim();
            Description = Regex.Replace(Description, @"^[ \t]+$[\r\n]*", "", RegexOptions.Multiline);
            Description = Regex.Replace(Description, @"(\r?\n){2,}", "\n");
            Description = Regex.Replace(Description, @" {2,}", " ");
            
            if(Description.Length < minLength)
            {
                message = string.Format(LocalizationResourceManager.Instance["ModeratorRequestValidation"].ToString(), minLength);
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }
            try
            {
                var result = await _userClient.CreateModeratorRequestAsync(Description);

                if (result.Success)
                {
                    message = LocalizationResourceManager.Instance["ModeratorRequestSubmitted"].ToString();
                    _ = Toast.Make(message).Show();
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
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
