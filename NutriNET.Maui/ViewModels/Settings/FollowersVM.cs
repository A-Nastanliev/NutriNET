using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class FollowersVM : PagedLoadingVM
    {
        readonly UserClient _userClient;

        public ObservableCollection<PublicUserVM> Followers { get; } = new();

        public FollowersVM(UserClient userClient)
        {
            _userClient = userClient;
        }

        [RelayCommand]
        public async Task VisitProfile(PublicUserVM user)
        {
        }

        [RelayCommand]
        public override async Task Load()
        {
            if (!CanStartLoading())
                return;

            BeginLoading();

            try
            {
                var (result,users, nextCursorDate, nextCursorId) = await _userClient.GetMyFollowersAsync(BatchSize, CursorDate, CursorId);

                if (result.Success)
                {
                    foreach (var u in users)
                        Followers.Add(u);

                    if (users.Any())
                    {
                        EndLoading(users.Count, nextCursorDate, nextCursorId);
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
        private async Task ToggleFollow(PublicUserVM user)
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;
            try
            {
                if (user.MyFollowing)
                {
                    var result = await _userClient.UnfollowAsync(user.Id);
                    if (result.Success)
                    {
                        user.MyFollowing = false;
                        WeakReferenceMessenger.Default.Send(new FollowChangedMessage(user, false));
                    }
                    else
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                    }
                }
                else
                {
                    var result = await _userClient.FollowAsync(user.Id);
                    if (result.Success)
                    {
                        user.MyFollowing = true;
                        WeakReferenceMessenger.Default.Send(new FollowChangedMessage(user, true));
                    }
                    else
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                    }
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public override Task Refresh()
        {
            throw new NotImplementedException();
        }

        public async Task OnAppearingAsync()
        {
            await Load();
        }
    }
}
    