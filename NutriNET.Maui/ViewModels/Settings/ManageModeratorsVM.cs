using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class ManageModeratorsVM : PagedLoadingVM
    {
        [ObservableProperty]
        ObservableCollection<PublicUserVM> moderators = new();

        UserClient _userClient;

        public ManageModeratorsVM(UserClient userClient)
        {
            _userClient = userClient;
            UserClient.OnLogout += Clear;
        }

        private Task Clear()
        {
            Moderators.Clear();
            CanLoadMore = true;
            Loading = false;
            CursorId = null;
            CursorDate = null;
            return Task.CompletedTask;
        }

        [RelayCommand]
        public override async Task Load()
        {
            if (!CanStartLoading())
                return;

            BeginLoading();

            try
            {
                var (result, moderators, cursorDate) = await _userClient.GetUsersAsync(UserRole.Moderator, BatchSize, CursorDate, CursorId);

                if (result.Success)
                {
                    foreach (var m in moderators)
                    {
                        Moderators.Add(m);
                    }

                    if (moderators.Any())
                    {
                        EndLoading(moderators.Count, cursorDate, Moderators.Last().Id);
                        return;
                    }

                    EndLoading(0, null, null);
                }
                else
                {
                    Loading = false;
                    string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                    string error = LocalizationResourceManager.Instance["Error"].ToString();
                    string message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                Loading = false;
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }

        }

        [RelayCommand]
        public async Task VisitProfile(PublicUserVM user)
        {
        }

        public async Task OnAppearingAsync()
        {
            await Load();
        }

        [RelayCommand]
        public override async Task Refresh()
        {
            try
            {
                await Clear();
                await Load();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task RemoveModerator(PublicUserVM user)
        {
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            string message = LocalizationResourceManager.Instance["DemoteMessage"].ToString();
            string title = String.Format(LocalizationResourceManager.Instance["DemoteTitle"].ToString(), user.Username);
            bool confirm = await Shell.Current.DisplayAlertAsync(title, message, accept, decline);

            if (!confirm)
                return;

            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var result = await _userClient.UpdateUserRoleAsync(user.Id, UserRole.User);
                if (result.Success)
                {
                    message = String.Format(LocalizationResourceManager.Instance["Demoted"].ToString(), user.Username);
                    _ = Toast.Make(message).Show();
                    Moderators.Remove(user);
                    return;
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
