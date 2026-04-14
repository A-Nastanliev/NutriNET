using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.ViewModels.Recipes;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class CommentRestrictionsVM : PagedLoadingVM, ILocalize
    {
        [ObservableProperty]
        ObservableCollection<CommentRestrictionVM> commentRestrictions = new();

        [ObservableProperty]
        CommentRestrictionVM selectedCommentRestriction;

        [ObservableProperty]
        ObservableCollection<KeyValuePair<string, RestrictionStatus>> restrictionStatuses = new();

        [ObservableProperty]
        KeyValuePair<string, RestrictionStatus> selectedRestrictionStatus;

        public Func<Task> OpenBottomSheet;

        public Func<Task> CloseBottomSheet;

        [ObservableProperty]
        bool canEndRestriction;

        [ObservableProperty]
        UserVM user;

        UserClient _userClient;
   
        public CommentRestrictionsVM(UserClient userClient, UserVM userVM) 
        {
            _userClient = userClient;
            User = userVM;
            UserClient.OnLogout += Clear;
            RestrictionStatuses.Add(new(LocalizationResourceManager.Instance["RestrictionStatusActive"].ToString(), RestrictionStatus.ActiveTemporary));
            RestrictionStatuses.Add(new(LocalizationResourceManager.Instance["RestrictionStatusUndefined"].ToString(), RestrictionStatus.ActiveIndefinite));
            RestrictionStatuses.Add(new(LocalizationResourceManager.Instance["RestrictionStatusInactive"].ToString(), RestrictionStatus.Inactive));
            SelectedRestrictionStatus = RestrictionStatuses.First();
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };
        }

        private Task Clear()
        {
            CommentRestrictions.Clear();
            CanLoadMore = true;
            Loading = false;
            CursorId = null;
            CursorDate = null;
            return Task.CompletedTask;
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
                var (result, restrictions)  = await _userClient.GetCommentRestrictionsInternalAsync(SelectedRestrictionStatus.Value,BatchSize, CursorDate, CursorId);

                if (result.Success)
                {
                    foreach (var r in restrictions)
                    {
                        CommentRestrictions.Add(r);
                    }

                    if (restrictions.Any())
                    {
                        EndLoading(restrictions.Count, CommentRestrictions.Last().StartDate, CommentRestrictions.Last().Id);
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
        public async Task EndCommentRestriction()
        {
            if (SelectedCommentRestriction == null) return;

            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            string message = String.Format(LocalizationResourceManager.Instance["EndUserRestrictionConfirmation"].ToString(), 
                SelectedCommentRestriction.PublicUser.Username);
            string title = LocalizationResourceManager.Instance["Confirm"].ToString();
            bool confirm = await Shell.Current.DisplayAlertAsync(title, message, accept, decline);
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            if (!confirm)
            {
                await CloseBottomSheet?.Invoke();
                SelectedCommentRestriction = null;
                return;
            }

            try
            {
                var result = await _userClient.EndCommentRestrictionAsync(SelectedCommentRestriction.Id);
                if (result.Success)
                {
                    await CloseBottomSheet?.Invoke();
                    CommentRestrictions.Remove(SelectedCommentRestriction);
                    SelectedCommentRestriction = null;
                }
                else
                {
                    title = LocalizationResourceManager.Instance["Error"].ToString();
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(title, message, ok);
                }
            }
            catch (Exception ex)
            {
                title = LocalizationResourceManager.Instance["Error"].ToString();
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(title, message, ok);
            }
        }

        [RelayCommand]
        private async Task SelectCommentRestriction(CommentRestrictionVM commentRestrictionVM)
        {
            SelectedCommentRestriction = commentRestrictionVM;
            CanEndRestriction = ((commentRestrictionVM.EndDate > DateTime.UtcNow || commentRestrictionVM.EndDate == null) && User.PublicUser.Role == UserRole.Administrator);
            await OpenBottomSheet?.Invoke();  
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
        private async Task SelectRestrictionStatus()
        {
            if (RestrictionStatuses == null || !RestrictionStatuses.Any())
                return;

            var options = RestrictionStatuses.Select(rs => rs.Key).ToArray();
            string cancelText = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["CommentRestrictionsPickerTitle"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(title, cancelText, null, options);

            if (string.IsNullOrEmpty(selected) || selected == cancelText || selected == SelectedRestrictionStatus.Key)
                return;

            var selectedStatus = RestrictionStatuses.FirstOrDefault(rs => rs.Key == selected);
            SelectedRestrictionStatus = selectedStatus;
            await Clear();
            LoadCommand.Execute(null);
        }

        public async Task OnAppearingAsync()
        {
            await Load();
        }

        public void OnLocalize()
        {
            var selected = SelectedRestrictionStatus.Value;
            RestrictionStatuses[0] = new(LocalizationResourceManager.Instance["RestrictionStatusActive"].ToString(), RestrictionStatus.ActiveTemporary);
            RestrictionStatuses[1] = new(LocalizationResourceManager.Instance["RestrictionStatusUndefined"].ToString(), RestrictionStatus.ActiveIndefinite);
            RestrictionStatuses[2] = new(LocalizationResourceManager.Instance["RestrictionStatusInactive"].ToString(), RestrictionStatus.Inactive);
            SelectedRestrictionStatus = RestrictionStatuses.First(x => x.Value == selected);
            foreach(var cr in CommentRestrictions)
            {
                cr.OnLocalize();
            }
        }
    }
}
