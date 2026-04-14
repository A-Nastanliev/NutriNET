using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.ViewModels.Recipes;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class FollowingVM : PagedLoadingVM, IRecipient<FollowChangedMessage>
    {
        private readonly UserClient _userClient;

        public ObservableCollection<PublicUserVM> Following { get; } = new();

        public FollowingVM(UserClient userClient)
        {
            _userClient = userClient;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        [RelayCommand]
        public async Task VisitProfile(PublicUserVM user)
        {
            await Shell.Current.GoToAsync(nameof(ProfilePage), true,
                new Dictionary<string, object> { [nameof(ProfileVM.User)] = user });
        }

        [RelayCommand]
        public override async Task Load()
        {
            if (!CanStartLoading()) return;

            BeginLoading();

            try
            {
                var (result,users, cursorDate, cursorId) = await _userClient.GetMyFollowingAsync(BatchSize, CursorDate, CursorId);

                if (result.Success)
                {
                    foreach (var u in users)
                        Following.Add(u);


                    if (users.Any())
                    {
                        EndLoading(users.Count, cursorDate, cursorId);
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
            if (!user.MyFollowing)
                return;

            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var result = await _userClient.UnfollowAsync(user.Id);
                if (result.Success)
                {
                    user.MyFollowing = false;
                    Following.Remove(user);
                    WeakReferenceMessenger.Default.Send(new FollowChangedMessage(user, false));
                }
                else
                {
                    string message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
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

        public void Receive(FollowChangedMessage message)
        {
            var isFollowing = message.Value;
            var userId = message.User.Id;
            var user = Following.FirstOrDefault(u => u.Id == userId);
            if (user != null && isFollowing == false) 
            {
                Following.Remove(user);
            }
            else if(isFollowing == true)
            {
                Following.Insert(0, message.User);
            }
        }
    }
}
