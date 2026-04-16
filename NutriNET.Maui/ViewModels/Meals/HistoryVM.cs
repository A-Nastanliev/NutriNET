using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Meal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Meals
{
    public partial class HistoryVM : PagedLoadingVM, ILocalize
    {
        [ObservableProperty]
        MealVM selectedMeal;

        [ObservableProperty]
        ObservableCollection<MealDayVM> mealDays = new();

        public Func<Task> OnSelectMeal;

        MealClient _mealClient;

        public HistoryVM(MealClient mealClient)
        {
            _mealClient = mealClient;
            UserClient.OnLogout += Clear;
            BatchSize = 8;
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };
        }

        public void OnLocalize()
        {
            foreach (ILocalize mealDay in MealDays)
            {
                mealDay.OnLocalize();
            }
        }

        private Task Clear()
        {
            MealDays.Clear();
            CanLoadMore = true;
            Loading = false;
            CursorId = null;
            CursorDate = null;
            return Task.CompletedTask;
        }


        [RelayCommand]
        private async Task SelectMeal(MealVM meal)
        {
            SelectedMeal = meal;
            await OnSelectMeal?.Invoke();
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
                var (result, days, cursor) = await _mealClient.GetNextMealDaysAsync(BatchSize, CursorDate, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));

                if (!result.Success)
                {
                    message = (LocalizationResourceManager.Instance[result.Error].ToString());
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    Loading = false;
                    return;
                }

                if (days.Any())
                {
                    foreach (var d in days)
                    {
                        MealDays.Add(d);
                    }           
                    EndLoading(days.Count, cursor , null);
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


        [RelayCommand]
        public override async Task Refresh()
        {
            try
            {
                CursorDate = null;
                CursorId = null;
                CanLoadMore = true;
                MealDays.Clear();
                await Load();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

    
    }
}
