using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.ViewModels.Recipes;
using NutriNET.Maui.Views.Food;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Food
{
    public partial class FoodRequestsVM : PagedLoadingVM, ILocalize
    {
        [ObservableProperty]
        ObservableCollection<KeyValuePair<string, RequestStatus>> requestStatuses = new();

        [ObservableProperty]
        KeyValuePair<string, RequestStatus> selectedRequestStatus;

        [ObservableProperty]
        FoodRequestVM selectedRequest;

        [ObservableProperty]
        ObservableCollection<FoodRequestVM> foodRequests = new();

        public Func<Task> OpenBottomSheet;

        public Func<Task> CloseBottomSheet;

        FoodClient _foodClient;

        public FoodRequestsVM(FoodClient foodClient)
        {
            _foodClient = foodClient;
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
            foreach(var fr in FoodRequests)
            {
                fr.OnLocalize();
            }
        }


        [RelayCommand]
        private async Task SelectRequestStatus()
        {
            if (RequestStatuses == null || !RequestStatuses.Any())
                return;

            var options = RequestStatuses.Select(rs => rs.Key).ToArray();
            string cancelText = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["FoodRequestsPickerTitle"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(title, cancelText, null, options);

            if (string.IsNullOrEmpty(selected) || selected == cancelText || selected == SelectedRequestStatus.Key)
                return;

            var selectedStatus = RequestStatuses.FirstOrDefault(rs => rs.Key == selected);
            SelectedRequestStatus = selectedStatus;
            await Clear();
            LoadCommand.Execute(null);
        }

        private Task Clear()
        {
            FoodRequests.Clear();
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
            if(user.Id ==  0) return;

            await Shell.Current.GoToAsync(nameof(ProfilePage), true,
                new Dictionary<string, object> { [nameof(ProfileVM.User)] = user });
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
                var (result, foodRequests) = await _foodClient.GetNextFoodRequestsAsync(BatchSize, CursorDate, CursorId, SelectedRequestStatus.Value);

                if (result.Success)
                {
                    foreach (var fr in foodRequests )
                    {
                        FoodRequests.Add(fr);
                    }

                    if (foodRequests.Any())
                    {
                        EndLoading(foodRequests.Count, foodRequests.Last().DateSent, foodRequests.Last().Id);
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
        public async Task AcceptFoodRequest()
        {
            if (SelectedRequest == null) return;

            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
                var result = await _foodClient.UpdateFoodRequestStatusAsync(SelectedRequest.Id, RequestStatus.Accepted);
                if (result.Success && !string.IsNullOrWhiteSpace(result.Error))
                {
                    message = String.Format(LocalizationResourceManager.Instance[result.Error].ToString(), SelectedRequest.Barcode);
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
                else if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
                else
                {
                    await Shell.Current.GoToAsync(nameof(FoodFormPage), true, new Dictionary<string, object> { [nameof(FoodRequestVM)] = SelectedRequest });
                }

                FoodRequests.Remove(SelectedRequest);
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
        public async Task DeclineFoodRequest()
        {
            if (SelectedRequest == null) return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var result = await _foodClient.UpdateFoodRequestStatusAsync(SelectedRequest.Id, RequestStatus.Declined);

                if (result.Success && !string.IsNullOrWhiteSpace(result.Error))
                {
                    message = String.Format(LocalizationResourceManager.Instance[result.Error].ToString(), SelectedRequest.Barcode);
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
                else if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                FoodRequests.Remove(SelectedRequest);
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
        public async Task SelectFoodRequest(FoodRequestVM foodRequestVM)
        {
            if (SelectedRequest == foodRequestVM)
            {
                SelectedRequest = null;
            }
            else
            {
                SelectedRequest = foodRequestVM;
                await OpenBottomSheet?.Invoke();
            }
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
