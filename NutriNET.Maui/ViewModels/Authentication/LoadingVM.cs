using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.ViewModels.Settings;
using NutriNET.Maui.Views.Authentication;
using NutriNET.Maui.Views.Meals;
using NutriNET.Maui.Views.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.ViewModels.Authentication
{
    public partial class LoadingVM : ObservableObject
    {
        readonly UserClient _userClient;

        public LoadingVM(UserClient userClient)
        {
            _userClient = userClient;
        }

        public async Task OnAppearingAsync()
        {
            string ok;
            string error;
            string message;

            try
            {
                var result = await _userClient.TokenLoginAsync();

                if (result.Success)
                {
                    await Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
                }
                else
                {
                    await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
                }
            }
            catch (Exception ex)
            {
                ok = LocalizationResourceManager.Instance["Ok"].ToString();
                error = LocalizationResourceManager.Instance["Error"].ToString();
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                await OnAppearingAsync();
            }
        }
    }
}
