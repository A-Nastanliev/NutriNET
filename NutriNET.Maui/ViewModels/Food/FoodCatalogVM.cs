using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Food;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.Views.Food;
using NutriNET.Maui.Views.Settings;
using Syncfusion.Maui.Toolkit.BottomSheet;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using ZXing.Net.Maui;

namespace NutriNET.Maui.ViewModels.Food
{
    public partial class FoodCatalogVM : PagedLoadingVM, IRecipient<FoodCreatedMessage>, IRecipient<FoodDeletedMessage>
    {
        [ObservableProperty]
        ObservableCollection<FoodVM> foods = new();

        [ObservableProperty]
        ObservableCollection<FoodVM> currentFoods = new();

        [ObservableProperty]
        FoodVM selectedFood;

        [ObservableProperty]
        string entrySearch;
        public DateTime? FoodsCursorDate { get; set; }

        CancellationTokenSource? _searchCts;

        public Func<Task>  OnSelectFood;
        public Func<Task> OnDeselectFood;

        [ObservableProperty]
        UserVM user;

        FoodClient _foodClient;

        public FoodCatalogVM(FoodClient foodClient, UserVM user)
        {
            _foodClient = foodClient;
            User = user;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }


        [RelayCommand]
        public override async Task Load()
        {
            if (!CanStartLoading())
                return;

            BeginLoading();

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var(result, foods, date) = await _foodClient.GetNextFoodsAsync(BatchSize, CursorDate, CursorId, EntrySearch);

                if (!result.Success)
                {
                    message = (LocalizationResourceManager.Instance[result.Error].ToString());
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    Loading = false;
                    return;
                }

                if (foods.Any())
                {
                    if (string.IsNullOrWhiteSpace(EntrySearch))
                    {
                        FoodsCursorDate = date;
                        foreach (var f in foods)
                        {
                            CurrentFoods.Add(f);
                            Foods.Add(f);
                        }
                    }
                    else
                    {
                        foreach (var f in foods)
                        {
                            CurrentFoods.Add(f);
                        }
                    }

                    EndLoading(foods.Count, date, foods.Last().Id);
                    return;
                }

                EndLoading(0, null, null);
            }
            catch (Exception ex)
            {
                Loading = false;
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        private async Task HandleSearchAsync(string search, CancellationToken token)
        {
            try
            {
                await Task.Delay(500, token); 

                if (token.IsCancellationRequested)
                    return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    CursorDate = null;
                    CursorId = null;
                    Loading = false;
                    CurrentFoods.Clear();
                    CanLoadMore = true;

                    if (string.IsNullOrWhiteSpace(search))
                    {
                        foreach (var f in Foods)
                        {
                            CurrentFoods.Add(f);
                        }
                        CursorDate = FoodsCursorDate;
                        CursorId = Foods.LastOrDefault()?.Id;
                        return;
                    }

                    await Load();
                });
            }
            catch (TaskCanceledException)
            {
            }
        }

        partial void OnEntrySearchChanged(string oldValue, string newValue)
        {
            if (IsRefreshing)
                return;

            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            _ = HandleSearchAsync(newValue, _searchCts.Token);
        }


        [RelayCommand]
        public async Task SelectFood(FoodVM food)
        {
            SelectedFood = food;
            await OnSelectFood?.Invoke();
        }

        [RelayCommand]
        public async Task EditFood()
        {
            await Shell.Current.GoToAsync(nameof(FoodFormPage), true, new Dictionary<string, object> { [nameof(FoodFormVM.NavigationFood)] = SelectedFood });
        }

        [RelayCommand]
        public async Task DeleteFood()
        {
            string alertTitle = String.Format(LocalizationResourceManager.Instance["DeleteName"].ToString(), SelectedFood.Name);
            string message = String.Format(LocalizationResourceManager.Instance["DeleteNameMessage"].ToString(), SelectedFood.Name);
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            bool confirm = await Shell.Current.DisplayAlertAsync(alertTitle, message, accept, decline);

            if (!confirm)
                return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
                var result = await _foodClient.DeleteFoodAsync(SelectedFood.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["DeletedFood"].ToString(), SelectedFood.Name);
                _ = Toast.Make(message, ToastDuration.Short).Show();

                WeakReferenceMessenger.Default.Send(new FoodDeletedMessage(SelectedFood.Id));

                await OnDeselectFood?.Invoke();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public override async Task Refresh()
        {
            try
            {
                FoodsCursorDate = null;
                CursorDate = null;
                CursorId = null;
                CanLoadMore = true;
                CurrentFoods.Clear();
                Foods.Clear();
                EntrySearch = null;
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

        public async Task HandleBarcodeAsync(string barcode)
        {
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;

            try
            {
                var (result, food) = await _foodClient.GetFoodByBarcodeAsync(barcode);              
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                if (food != null && food.Barcode == barcode)
                {
                    SelectedFood = food;
                    await OnSelectFood?.Invoke();
                }
                else
                {
                    message = String.Format(LocalizationResourceManager.Instance["BarcodeNotFound"].ToString(), barcode);
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        public void Receive(FoodCreatedMessage message)
        {
            var food = message.Value;
            Foods.Insert(0, food);
            if (string.IsNullOrWhiteSpace(EntrySearch) || EntrySearch.Contains(food.Name))
            {
                CurrentFoods.Insert(0, food);
            }
        }

        public void Receive(FoodDeletedMessage message)
        {
            var id = message.Value;
            var foodInFoods = Foods.FirstOrDefault(f => f.Id == id);
            if (foodInFoods != null)
                Foods.Remove(foodInFoods);

            var foodInSearched = CurrentFoods.FirstOrDefault(f => f.Id == id);
            if (foodInSearched != null)
                CurrentFoods.Remove(foodInSearched);
        }
    }
}
