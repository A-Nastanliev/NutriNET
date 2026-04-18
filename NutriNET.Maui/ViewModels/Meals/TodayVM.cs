using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Meal;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.ViewModels.Recipes;
using SkiaSharp;

namespace NutriNET.Maui.ViewModels.Meals
{
    public partial class TodayVM : ObservableObject, ILocalize
    {
        [ObservableProperty]
        MealVM selectedMeal;

        [ObservableProperty]
        MealDayVM mealDay = new();

        public Func<Task> OnSelectMeal;
        public Func<Task> OnDeselectMeal;
        public Action CreateBarcodeScanner;

        [ObservableProperty]
        RecipeListLoaderVM recipeListLoader;

        MealClient _mealClient;
        RecipeClient _recipeClient;
   
        [ObservableProperty]
        bool loading;

        [ObservableProperty]
        bool isRefreshing;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelecting))]
        bool isSelectingFood;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelecting))]
        bool isBarcodeScanning;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelecting))]
        bool isSelectingRecipe;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelecting))]
        bool isSelectingSavedRecipe;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelecting))]
        bool isSelectingPersonalRecipe;

        public bool IsSelecting => IsSelectingFood || IsSelectingRecipe || IsBarcodeScanning || IsSelectingPersonalRecipe || IsSelectingSavedRecipe;

        bool _isDeletingMealFood;

        [ObservableProperty]
        PieChart macroChart;

        public TodayVM(MealClient mealClient, RecipeClient recipeClient, RecipeListLoaderVM listLoader)
        {
            _mealClient = mealClient;
            _recipeClient = recipeClient;
            RecipeListLoader = listLoader;
            UserClient.OnLogout += Clear;
            MacroChart = new PieChart
            {
                Entries = new[]
                {
                    new ChartEntry(0),
                    new ChartEntry(0),
                    new ChartEntry(0), 
                },
                AnimationDuration = TimeSpan.FromSeconds(0.5),
                LabelMode = LabelMode.None,
                BackgroundColor = SKColor.Empty
            };
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };

        }

        public void OnLocalize()
        {
            MealDay.OnLocalize();
#if ANDROID
            NutriNET.Maui.Platforms.Android.NutriWidgetPreferences.SaveAndRefresh(
                MealDay.Calories, MealDay.Proteins, MealDay.Carbohydrates, MealDay.Fats);
#endif
        }

        void UpdateChart()
        {
            var protein = (float)(MealDay?.ProteinRatio ?? 0);
            var carbs = (float)(MealDay?.CarbsRatio ?? 0);
            var fat = (float)(MealDay?.FatRatio ?? 0);

            var total = protein + carbs + fat;

            if (total == 0)
            {
                protein = carbs = fat = 1;
            }

            var entries = new[]
            {
                new ChartEntry(protein)
                {
                    Color = SKColor.Parse(((Color)Application.Current.Resources["ProteinColor"]).ToHex())
                },
                new ChartEntry(carbs)
                {
                    Color = SKColor.Parse(((Color)Application.Current.Resources["CarbsColor"]).ToHex())
                },
                new ChartEntry(fat)
                {
                    Color = SKColor.Parse(((Color)Application.Current.Resources["FatColor"]).ToHex())
                },
            };

            MacroChart.Entries = entries;
#if ANDROID
            NutriNET.Maui.Platforms.Android.NutriWidgetPreferences.SaveAndRefresh(
                MealDay.Calories, MealDay.Proteins, MealDay.Carbohydrates, MealDay.Fats);
#endif
            OnPropertyChanged(nameof(MacroChart));
        }

        private Task Clear()
        {
            MealDay.Meals.Clear();
            Loading = false;
            MealDay.Date = new DateTime();
            MealDay.RecalculateMacros();
            UpdateChart();
            return Task.CompletedTask;
        }


        [RelayCommand]
        private async Task Load()
        {
            if (Loading)
                return;

            Loading = true;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                var result = await _mealClient.GetTodaysMealsAsync(offset, MealDay);

                if (!result.Success)
                {
                    message = (LocalizationResourceManager.Instance[result.Error].ToString());
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    Loading = false;
                    return;
                }
                MealDay.RecalculateMacros();
                UpdateChart();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
            finally
            {
                Loading = false;
            }

        }

        [RelayCommand]
        public async Task AddMeal()
        {
            var mealTypes = Enum.GetValues(typeof(MealType)).Cast<MealType>().ToList();

            var map = mealTypes.ToDictionary( m => LocalizationResourceManager.Instance[m.ToString()].ToString(),m => m);

            var options = map.Keys.ToArray();

            string selected = await Shell.Current.DisplayActionSheetAsync(
                LocalizationResourceManager.Instance["SelectMealType"].ToString(),
                LocalizationResourceManager.Instance["Cancel"].ToString(),
                null,
                options);

            if (string.IsNullOrEmpty(selected) ||
                selected == LocalizationResourceManager.Instance["Cancel"].ToString())
                return;

            var selectedMealType = map[selected];

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;
            try
            {
                MealVM meal = new MealVM(selectedMealType);
                var result = await _mealClient.CreateMealAsync(meal);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                MealDay.Meals.Insert(0, meal);
                await SelectMeal(meal);
            }
            catch (Exception ex) 
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task UpdateMeal()
        {
            var mealTypes = Enum.GetValues(typeof(MealType)).Cast<MealType>().ToList();

            var map = mealTypes.ToDictionary(m => LocalizationResourceManager.Instance[m.ToString()].ToString(),m => m);

            var options = map.Keys.ToArray();

            string selected = await Shell.Current.DisplayActionSheetAsync(
                LocalizationResourceManager.Instance["SelectMealType"].ToString(),
                LocalizationResourceManager.Instance["Cancel"].ToString(),
                null,
                options);

            if (string.IsNullOrEmpty(selected) ||
                selected == LocalizationResourceManager.Instance["Cancel"].ToString())
                return;

            var selectedMealType = map[selected];
            if (selectedMealType == SelectedMeal.Type)
                return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;
            var type = SelectedMeal.Type;
            try
            {
                SelectedMeal.Type = selectedMealType;
                var result = await _mealClient.UpdateMealAsync(SelectedMeal);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    SelectedMeal.Type = type;
                    return;
                }           
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                SelectedMeal.Type = type;
            }
        }

        [RelayCommand]
        public async Task DeleteMeal()
        {
            string alertTitle = String.Format(LocalizationResourceManager.Instance["DeleteName"].ToString(),
                LocalizationResourceManager.Instance[SelectedMeal.Type.ToString()].ToString().ToLower());

            DateTime utcTime = SelectedMeal.DateTime; 
            DateTime localTime = utcTime.ToLocalTime();
            string message = String.Format(LocalizationResourceManager.Instance["DeleteMealMessage"].ToString(),
                LocalizationResourceManager.Instance[SelectedMeal.Type.ToString()].ToString().ToLower(),
                localTime.ToString("HH:mm"));
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            bool confirm = await Shell.Current.DisplayAlertAsync(alertTitle, message, accept, decline);

            if (!confirm)
                return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
                var result = await _mealClient.DeleteMealAsync(SelectedMeal.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["DeletedMeal"].ToString(),
                    LocalizationResourceManager.Instance[SelectedMeal.Type.ToString()].ToString().ToLower(),
                    localTime.ToString("HH:mm"));
                _ = Toast.Make(message, ToastDuration.Short).Show();
                MealDay.Meals.Remove(SelectedMeal);
                MealDay.RecalculateMacros();
                UpdateChart();
                await OnDeselectMeal?.Invoke();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        [RelayCommand]
        public async Task EditMealFood(MealFoodVM mealFoodVM)
        {
            if (_isDeletingMealFood) return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message = String.Format(LocalizationResourceManager.Instance["EnterWeightMessage"].ToString(),
                mealFoodVM.Food.Name);
            string title = LocalizationResourceManager.Instance["EditWeight"].ToString();

            string result = await Shell.Current.DisplayPromptAsync(title, message,
             keyboard: Keyboard.Numeric, initialValue: mealFoodVM.Weight.ToString());

            if (string.IsNullOrWhiteSpace(result))
                return;

            string normalized = result.Replace('.', ',');
            if (double.TryParse(normalized, out double value))
            {
                if (value == mealFoodVM.Weight || value <= 0 || (value >= 2000 && mealFoodVM.Weight == 2000)) return;

                double rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                if (rounded > 2000)
                    rounded = 2000;

                try
                {
                    var apiResult = await _mealClient.UpdateMealFoodAsync(mealFoodVM, rounded);
                    if (!apiResult.Success)
                    {
                        message = LocalizationResourceManager.Instance[apiResult.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        return;
                    }

                    mealFoodVM.RecalculateMacros();
                    SelectedMeal.RecalculateMacros();
                    MealDay.RecalculateMacros();
                    UpdateChart();
                }
                catch (Exception ex) 
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }

            }
        }

        [RelayCommand]
        public async Task SelectFood(FoodVM food)
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message = String.Format(LocalizationResourceManager.Instance["EnterWeightMessage"].ToString(),
               food.Name);
            string title = LocalizationResourceManager.Instance["EnterWeightTitle"].ToString();

            string result = await Shell.Current.DisplayPromptAsync(title, message,
             keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(result))
                return;

            string normalized = result.Replace('.', ',');
            if (double.TryParse(normalized, out double value))
            {
                if (value <= 0) return;

                double rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                if (rounded > 2000)
                    rounded = 2000;

                try
                {
                    MealFoodVM mealFood = new MealFoodVM(rounded, food);
                    var apiResult = await _mealClient.AddMealFoodAsync(SelectedMeal.Id, mealFood);
                    if (!apiResult.Success)
                    {
                        message = LocalizationResourceManager.Instance[apiResult.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        return;
                    }

                    mealFood.RecalculateMacros();
                    SelectedMeal.MealFoods.Add(mealFood);
                    SelectedMeal.RecalculateMacros();
                    MealDay.RecalculateMacros();
                    UpdateChart();
                    await Cancel();
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }

            }
        }

        [RelayCommand]
        public async Task DeleteMealFood(MealFoodVM mealFood)
        {
            _isDeletingMealFood = true;
            string alertTitle = String.Format(LocalizationResourceManager.Instance["DeleteName"].ToString(),mealFood.Food.Name);

            string message = String.Format(LocalizationResourceManager.Instance["DeleteMealFoodMessage"].ToString(),
                mealFood.Weight.ToString(), mealFood.Food.Name);
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            bool confirm = await Shell.Current.DisplayAlertAsync(alertTitle, message, accept, decline);

            if (!confirm)
            {
                _isDeletingMealFood = false;
                return;
            }

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();

            try
            {
                var result = await _mealClient.DeleteMealFoodAsync(mealFood.Id);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                SelectedMeal.MealFoods.Remove(mealFood);
                SelectedMeal.RecalculateMacros();
                MealDay.RecalculateMacros();
                UpdateChart();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
            finally
            {
                _isDeletingMealFood = false;
            }
        }

        [RelayCommand]
        public async Task Cancel()
        {
            IsSelectingRecipe = false;
            IsSelectingPersonalRecipe = false;
            IsSelectingSavedRecipe = false;
            IsSelectingFood = false;
            IsBarcodeScanning = false;
        }

        [RelayCommand]
        public async Task ChooseSelection()
        {
            var selectFood = LocalizationResourceManager.Instance["SelectFoodMessage"].ToString();
            var selectRecipe = LocalizationResourceManager.Instance["SelectRecipeMessage"].ToString();
            var selectBarcode = LocalizationResourceManager.Instance["SelectBarcodeScanMessage"].ToString();
            var cancel = LocalizationResourceManager.Instance["Cancel"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(LocalizationResourceManager.Instance["FoodSelectionTitle"].ToString(), 
                cancel, null, selectFood, selectRecipe, selectBarcode);

            if (string.IsNullOrEmpty(selected) || selected == cancel)
                return;

            if (selected == selectFood)
            {
                IsSelectingFood = true;
            }
            else if (selected == selectRecipe)
            {
                var search = LocalizationResourceManager.Instance["SearchRecipesMessage"].ToString();
                var saved = LocalizationResourceManager.Instance["SavedRecipesMessage"].ToString();
                var mine = LocalizationResourceManager.Instance["MyRecipesMessage"].ToString();

                string recipeChoice = await Shell.Current.DisplayActionSheetAsync
                    (LocalizationResourceManager.Instance["SelectRecipeTitle"].ToString(), cancel, null, search, saved, mine);

                if (string.IsNullOrEmpty(recipeChoice) || recipeChoice == cancel)
                    return;

                if (recipeChoice == search)
                {
                    IsSelectingRecipe = true;
                }
                else if (recipeChoice == saved)
                {
                    await PickRecipeList();
                    IsSelectingSavedRecipe = RecipeListLoader.RecipeList.Id != 0;
                }
                else if (recipeChoice == mine)
                {
                    IsSelectingPersonalRecipe = true;
                }
            }
            else if (selected == selectBarcode)
            {
                IsBarcodeScanning = true;
                CreateBarcodeScanner?.Invoke();
            }
        }

        [RelayCommand]
        public async Task PickRecipeList()
        {
            var cancel = LocalizationResourceManager.Instance["Cancel"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string message;
            List<RecipeListVM> lists = new();
            try
            {
                (var result, lists) = await _recipeClient.GetAllRecipeListsAsync();

                if (!result.Success || lists == null)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                return;
            }

            var nameCounts = lists.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
            var seen = new Dictionary<string, int>();
            var displayNames = new string[lists.Count];

            for (int i = 0; i < lists.Count; i++)
            {
                var name = lists[i].Name;
                if (nameCounts.Contains(name))
                {
                    seen.TryGetValue(name, out int count);
                    seen[name] = count + 1;
                    displayNames[i] = $"{name} ({count + 1})";
                }
                else
                {
                    displayNames[i] = name;
                }
            }
            string title = LocalizationResourceManager.Instance["PickListTitle"].ToString();

            string? chosen = await Shell.Current.DisplayActionSheetAsync(title, cancel, null, displayNames);

            if (chosen == null || chosen == cancel) return;

            int chosenIndex = Array.IndexOf(displayNames, chosen);
            if (chosenIndex < 0) return;

            var pickedList = lists[chosenIndex];
            if (RecipeListLoader.RecipeList.Id == pickedList.Id) return;

            RecipeListLoader.RecipeList =  pickedList;
            await RecipeListLoader.Refresh();
        }

        [RelayCommand]
        private async Task SelectMeal(MealVM meal)
        {
            SelectedMeal = meal;
            await OnSelectMeal?.Invoke();
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                await Load();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        public async Task OnAppearing()
        {
            if (MealDay.Date != DateTime.Now.Date)
            {
               await Refresh();
            }
        }
    }
}
