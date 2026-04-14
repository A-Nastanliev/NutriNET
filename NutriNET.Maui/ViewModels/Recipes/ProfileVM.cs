using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class ProfileVM : RecipesLoadingVM, IQueryAttributable, IRecipient<FollowChangedMessage>, IRecipient<RecipeDeletedMessage>
    {
        [ObservableProperty]
        PublicUserVM user;

        [ObservableProperty]
        int followers;

        [ObservableProperty]
        int following;

        [ObservableProperty]
        bool canDeleteUser;

        [ObservableProperty]
        bool showFollowButton;

        readonly UserClient _userClient;
        readonly UserVM _user;

        public ProfileVM(RecipeClient recipeClient, UserClient userClient, UserVM user) : base(recipeClient)
        {
            _userClient = userClient;
            _user = user;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        protected override Task<(RequestResult, List<FoodVM>, DateTime?, int?)> FetchRecipes(
            int batchSize, DateTime? cursorDate, int? cursorId, string search)
        {
            return _recipeClient.GetUserRecipesAsync(User.Id,batchSize, cursorDate, cursorId);
        }

        [RelayCommand]
        public async Task DeleteUser()
        {
            string message;
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            string title = string.Format(LocalizationResourceManager.Instance["ConfirmDeleteProgressTitle"].ToString(), 1, 2);
            message = string.Format(LocalizationResourceManager.Instance["DeleteUserConfirmMessage"].ToString(),User.Username );

            bool confirm = await Shell.Current.DisplayAlertAsync(title, message, accept, decline);

            if (!confirm)
                return;

            title = string.Format(LocalizationResourceManager.Instance["ConfirmDeleteProgressTitle"].ToString(), 2, 2);
            confirm = await Shell.Current.DisplayAlertAsync(title, message, accept, decline);

            if (!confirm)
                return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var result = await _userClient.DeleteUserAsync(User.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["DeletedUser"].ToString(), User.Username);
                _ = Toast.Make(message, ToastDuration.Short).Show();
                var currentTab = Shell.Current.CurrentItem?.CurrentItem;
                if (currentTab != null)
                {
                    foreach (var content in currentTab.Items)
                    {
                        await content.Navigation.PopToRootAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        private async Task ToggleFollow()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;
            try
            {
                if (User.MyFollowing)
                {
                    var result = await _userClient.UnfollowAsync(User.Id);
                    if (result.Success)
                    {
                        WeakReferenceMessenger.Default.Send(new FollowChangedMessage(User, false));
                    }
                    else
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                    }
                }
                else
                {
                    var result = await _userClient.FollowAsync(User.Id);
                    if (result.Success)
                    {
                        WeakReferenceMessenger.Default.Send(new FollowChangedMessage(User, true));
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

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue($"{nameof(User)}", out var publicUser) && publicUser is PublicUserVM user)
            {
                User = user;
                string message;
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                ShowFollowButton = User.Id != _user.PublicUser.Id;
                CanDeleteUser = User.Id != _user.PublicUser.Id && User.Role != UserRole.Administrator && _user.PublicUser.Role == UserRole.Administrator;
                try
                {
                     (var result, Followers, Following) = await _userClient.GetUserStatsAsync(User);
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        await Shell.Current.GoToAsync("..");
                        return;
                    }
                    await Load();
                    query.Clear();
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
        }

        public override async Task Refresh()
        {
            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            try
            {
                (var result, Followers, Following) = await _userClient.GetUserStatsAsync(User);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    await Shell.Current.GoToAsync("..");
                    return;
                }
                Clear();
                await Load();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public void Receive(FollowChangedMessage message)
        {
            int userId = message.User.Id;   
            bool isFollowing = message.Value;
            if (userId == User.Id)
            {
                if (isFollowing)
                {
                    User.MyFollowing = true;
                    Followers += 1;
                }
                else
                {
                    User.MyFollowing = false;
                    Followers -= 1;
                }
            }
        }

        public void Receive(RecipeDeletedMessage message)
        {
            var recipe = CurrentRecipes.FirstOrDefault(r => r.Id == message.Value.Id);
            if (recipe != null)
            {
                CurrentRecipes.Remove(recipe);
                Recipes.Remove(recipe);
            }
        }
    }
}
