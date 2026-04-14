using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Food;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Views.Food;
using System;
using System.Collections.Generic;
using System.Text;
using ZXing.Net.Maui;

namespace NutriNET.Maui.ViewModels.Food
{
    public partial class FoodFormVM : ObservableObject, IQueryAttributable, ILocalize
    {
        readonly FoodClient _foodClient;
        string _selectedImagePath;

        [ObservableProperty]
        FoodVM food = new();

        [ObservableProperty]
        string title;
        [ObservableProperty]
        string submitButtonText;

        [ObservableProperty]
        FoodVM navigationFood;

        bool isEditMode;

        bool isImageChanged;

        public FoodFormVM(FoodClient foodClient)
        {
            _foodClient = foodClient;
            Title = LocalizationResourceManager.Instance["CreateFoodTitle"].ToString();
            SubmitButtonText = LocalizationResourceManager.Instance["Add"].ToString();
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };
        }

        public void OnLocalize()
        {
            if (isEditMode)
            {
                Title = LocalizationResourceManager.Instance["UpdateFoodTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["SaveChanges"].ToString();
            }
            else
            {
                Title = LocalizationResourceManager.Instance["CreateFoodTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["Add"].ToString();
            }
        }

        [RelayCommand]
        private async Task PickFoodImage()
        {
            var options = new MediaPickerOptions
            {
                SelectionLimit = 1,
            };
            var results = await MediaPicker.Default.PickPhotosAsync(options);

            if (results == null || results.Count == 0)
                return;

            var result = results[0];

            await using var sourceStream = await result.OpenReadAsync();
            var localFilePath = await ImageManager.SaveTempImageAsync(sourceStream, Path.GetExtension(result.FileName));

            ImageManager.CleanupTempImage(_selectedImagePath);
            _selectedImagePath = localFilePath;

            Food.ImageSource = ImageSource.FromFile(localFilePath);

            if (isEditMode)
                isImageChanged = true;
        }

        [RelayCommand]
        public async Task Submit()
        {
            var validateResult = Validate();
            if(validateResult != null)
            {
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                await Shell.Current.DisplayAlertAsync(error, validateResult, ok);
                return;
            }

            if (!isEditMode) 
            {
                await CreateFood();
            }
            else
            {
                await UpdateFood();
            }
        }

        private async Task CreateFood()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var result = await _foodClient.CreateFoodAsync(Food, _selectedImagePath);

                if (result.Success)
                {
                    message = String.Format(LocalizationResourceManager.Instance["CreatedFood"].ToString(), Food.Name);
                    _ = Toast.Make(message).Show();
                    await GoBack();
                }
                else
                {
                    message = string.Format(LocalizationResourceManager.Instance[result.Error].ToString(), Food.Barcode);
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        private async Task UpdateFood()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var result = await _foodClient.UpdateFoodAsync(Food, isImageChanged ? _selectedImagePath : null);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error ?? "GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["UpdatedFood"].ToString(), Food.Name);
                _ = Toast.Make(message).Show();
                NavigationFood.CopyFrom(Food);
                await GoBack();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        string? Validate()
        {
            if (string.IsNullOrWhiteSpace(Food.Name))
                return LocalizationResourceManager.Instance["FoodValidationName"].ToString();

            if (Food.Calories < 0 || Food.Calories > 900)
                return LocalizationResourceManager.Instance["FoodValidationCalories"].ToString();

            if (Food.Proteins < 0 || Food.Proteins > 100)
                return LocalizationResourceManager.Instance["FoodValidationProtein"].ToString();

            if (Food.Carbohydrates < 0 || Food.Carbohydrates > 100)
                return LocalizationResourceManager.Instance["FoodValidationCarbohydrates"].ToString();

            if (Food.Fats < 0 || Food.Fats > 100)
                return LocalizationResourceManager.Instance["FoodValidationFats"].ToString();

            if (!isEditMode && _selectedImagePath == null)
                return LocalizationResourceManager.Instance["FoodValidationImage"].ToString();

            if (!string.IsNullOrEmpty(Food.Barcode) && (Food.Barcode.Length < 8 || Food.Barcode.Length > 13))
                return LocalizationResourceManager.Instance["FoodValidationBarcode"].ToString();

            return null;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue($"{nameof(NavigationFood)}", out var obj) && obj is FoodVM food)
            {
                NavigationFood = food;
                Food.CopyFrom(food);
                Title = LocalizationResourceManager.Instance["UpdateFoodTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["SaveChanges"].ToString();
                isEditMode = true;
            }
            else if (query.TryGetValue(nameof(FoodRequestVM), out var request) && request is FoodRequestVM foodRequest)
            {
                Food.Barcode = foodRequest.Barcode;
                Food.Name = foodRequest.Name;
                Food.ExtraInfo = foodRequest.Brand;
            }

        }

        [RelayCommand]
        public async Task GoBack()
        {
            if (!isEditMode && Food.Id != 0)
            {
                Food.FoodType = FoodType.Food;
                WeakReferenceMessenger.Default.Send(new FoodCreatedMessage(Food));
                await Shell.Current.GoToAsync($"//{nameof(FoodCatalogPage)}");
            }
            else
            {
                await Shell.Current.GoToAsync($"//{nameof(FoodCatalogPage)}");
            }
        }
    }
}
