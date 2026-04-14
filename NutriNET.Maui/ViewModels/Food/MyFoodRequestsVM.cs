using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Food
{
    public partial class MyFoodRequestsVM : ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<FoodRequestVM> myFoodRequests = new();

        [ObservableProperty]
        FoodRequestVM foodRequest;

        [ObservableProperty]
        bool canRequest;

        public Func<Task> OnRequestCreated;

        UserVM _user;
        readonly FoodClient _foodClient;

        public MyFoodRequestsVM(UserVM userVM, FoodClient foodClient)
        {
            _user = userVM;
            MyFoodRequests = userVM.PendingFoodRequests;
            _foodClient = foodClient;
            FoodRequest = new FoodRequestVM(userVM.PublicUser);
        }

        public async Task OnAppearingAsync()
        {
            string message;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
               var result = await _foodClient.GetMyPendingFoodRequestsAsync();
                if (result.Success)
                {
                    CanRequest = MyFoodRequests.Count < 3;
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


        [RelayCommand]
        private async Task CreateFoodRequest()
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;

            if (string.IsNullOrWhiteSpace(FoodRequest.Name))
            {
                message = LocalizationResourceManager.Instance["FoodRequestValidationName"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            if (string.IsNullOrWhiteSpace(FoodRequest.Brand))
            {
                message = LocalizationResourceManager.Instance["FoodRequestValidationBrand"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            if (string.IsNullOrWhiteSpace(FoodRequest.Barcode) || FoodRequest.Barcode?.Trim()?.Length < 8)
            {
                message = LocalizationResourceManager.Instance["FoodRequestValidationBarcode"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            try
            {
                FoodRequest.Barcode = FoodRequest.Barcode.Trim();
                FoodRequest.Name = FoodRequest.Name.Trim();
                FoodRequest.Brand = FoodRequest.Brand.Trim();
                var result = await _foodClient.CreateFoodRequestAsync(FoodRequest);

                if (result.Success)
                {
                    message = String.Format(LocalizationResourceManager.Instance["FoodRequestCreated"].ToString(), FoodRequest.Name);
                    _ = Toast.Make(message).Show();
                    MyFoodRequests.Add(FoodRequest);
                    FoodRequest.DateSent = DateTime.UtcNow;
                    FoodRequest = new FoodRequestVM(_user.PublicUser);
                    OnRequestCreated?.Invoke();
                    CanRequest = MyFoodRequests.Count < 3;
                }
                else
                {
                    message = String.Format(LocalizationResourceManager.Instance[result.Error].ToString(), FoodRequest.Barcode);
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
