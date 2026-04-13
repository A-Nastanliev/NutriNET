using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core.Extensions;
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
using System.Runtime.CompilerServices;
using System.Text;

namespace NutriNET.Maui.ViewModels.Settings
{
    public partial class ModeratorRequestsVM : PagedLoadingVM, ILocalize
    {
        [ObservableProperty]
        ObservableCollection<KeyValuePair<string, RequestStatus>> requestStatuses = new();

        [ObservableProperty]
        KeyValuePair<string, RequestStatus> selectedRequestStatus;

        [ObservableProperty]
        ModeratorRequestVM selectedRequest;

        [ObservableProperty]
        ObservableCollection<ModeratorRequestVM> moderatorRequests = new();

        public Func<Task> OpenBottomSheet;

        public Func<Task> CloseBottomSheet;

        UserClient _userClient;

        public ModeratorRequestsVM(UserClient userClient) 
        {
            _userClient = userClient;
            UserClient.OnLogout += Clear;
            RequestStatuses.Add(new(LocalizationResourceManager.Instance["RequestStatusPending"].ToString(), RequestStatus.Pending));
            RequestStatuses.Add(new(LocalizationResourceManager.Instance["RequestStatusDeclined"].ToString(), RequestStatus.Declined));
            RequestStatuses.Add(new(LocalizationResourceManager.Instance["RequestStatusAccepted"].ToString(), RequestStatus.Accepted));
            SelectedRequestStatus = RequestStatuses.First();
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };
        }

        public void OnLocalize()
        {
            var selected = SelectedRequestStatus.Value;
            RequestStatuses[0] = new(LocalizationResourceManager.Instance["RequestStatusPending"].ToString(), RequestStatus.Pending);
            RequestStatuses[1] = new(LocalizationResourceManager.Instance["RequestStatusDeclined"].ToString(), RequestStatus.Declined);
            RequestStatuses[2] = new(LocalizationResourceManager.Instance["RequestStatusAccepted"].ToString(), RequestStatus.Accepted);
            SelectedRequestStatus = RequestStatuses.First(x => x.Value == selected);
            foreach (var mr in ModeratorRequests)
            {
                mr.OnLocalize();
            }
        }

        private Task Clear()
        {
            ModeratorRequests.Clear();
            CanLoadMore = true;
            Loading = false;
            CursorId = null;
            CursorDate = null;
            SelectedRequest = null;
            return Task.CompletedTask;
        }

        [RelayCommand]
        public async Task VisitProfile(PublicUserVM user)
        {
            if (user.Id == 0) return;

    
        }

        [RelayCommand]
        public override async Task Load()
        {
            if (!CanStartLoading())
                return;

            BeginLoading();

            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();

            try
            {
                var (result,modRequests) = await _userClient.GetModeratorRequestsAsync(SelectedRequestStatus.Value, BatchSize, CursorDate, CursorId);

                if (result.Success)
                {
                    foreach (var mr in modRequests)
                    {
                        ModeratorRequests.Add(mr);
                    }

                    if (modRequests.Any())
                    {
                        EndLoading(modRequests.Count, ModeratorRequests.Last().DateSent, ModeratorRequests.Last().Id);
                        return;
                    }

                    EndLoading(0, null, null);
                }
                else
                {
                    Loading = false;
                    string message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex) 
            {
                Loading = false;    
                string message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
           
        }

        [RelayCommand]
        public async Task AcceptModeratorRequest()
        {
            if (SelectedRequest == null) return;

            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            string title = LocalizationResourceManager.Instance["Confirm"].ToString();
            string message = String.Format(LocalizationResourceManager.Instance["PromoteUserToModeratorMessage"].ToString(),
               SelectedRequest.PublicUser.Username);
            bool confirm = await Shell.Current.DisplayAlertAsync(title,message, accept, decline);

            if (!confirm)
            {
                await CloseBottomSheet?.Invoke();
                SelectedRequest = null;
                return;
            }

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
                var result = await _userClient.UpdateModeratorRequestAsync(SelectedRequest.Id, RequestStatus.Accepted);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                ModeratorRequests.Remove(SelectedRequest);
                await CloseBottomSheet?.Invoke();
                SelectedRequest = null;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task DeclineModeratorRequest()
        {
            if(SelectedRequest == null) return;

            string error = LocalizationResourceManager.Instance["Error"].ToString(); 
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var result = await _userClient.UpdateModeratorRequestAsync(SelectedRequest.Id, RequestStatus.Declined);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                ModeratorRequests.Remove(SelectedRequest);
                await CloseBottomSheet?.Invoke();
                SelectedRequest = null;
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        private async Task SelectRequestStatus()
        {
            if (RequestStatuses == null || !RequestStatuses.Any())
                return;

            var options = RequestStatuses.Select(rs => rs.Key).ToArray();
            string cancelText = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["ModeratorRequestsPickerTitle"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(title, cancelText, null, options);

            if (string.IsNullOrEmpty(selected) || selected == cancelText || selected == SelectedRequestStatus.Key)
                return;

            var selectedStatus = RequestStatuses.FirstOrDefault(rs => rs.Key == selected);
            SelectedRequestStatus = selectedStatus;
            await Clear();
            LoadCommand.Execute(null);
        }

        [RelayCommand]
        public async Task SelectModeratorRequest(ModeratorRequestVM moderatorRequestVM)
        {
            SelectedRequest = moderatorRequestVM;
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

        public async Task OnAppearingAsync()
        {
            await Load();
        }
    }
}
