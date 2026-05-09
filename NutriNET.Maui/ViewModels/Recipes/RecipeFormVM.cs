using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microcharts;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Meal;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Models.User;
using NutriNET.Maui.Views.Recipes;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ZXing.Net.Maui;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class RecipeFormVM : ObservableObject, IQueryAttributable, ILocalize, IRecipient<MacroThemeChanged>
    {
        readonly RecipeClient _recipeClient;
        string _selectedImagePath;

        [ObservableProperty]
        RecipeVM recipe= new();

        [ObservableProperty]
        string title;
        [ObservableProperty]
        string submitButtonText;

        [ObservableProperty]
        RecipeVM navigationRecipe = new();

        [ObservableProperty]
        FoodVM navigationRecipeFood = new();

        [ObservableProperty]
        ObservableCollection<KeyValuePair<string, PrivacyLevel>> privacyLevels = new();

        [ObservableProperty]
        KeyValuePair<string, PrivacyLevel> selectedPrivacyLevel;

        bool isEditMode;

        bool isImageChanged;
        bool _isDeletingIngredient;

        [ObservableProperty]
        PieChart macroChart;

        readonly UserVM _user;

        public RecipeFormVM(RecipeClient recipeClient, UserVM _userVM)
        {
            _user = _userVM;
            _recipeClient = recipeClient;
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
            Title = LocalizationResourceManager.Instance["CreateRecipeTitle"].ToString();
            SubmitButtonText = LocalizationResourceManager.Instance["Add"].ToString();
            var allLevels = Enum.GetValues<PrivacyLevel>().Where(p => p != PrivacyLevel.Archieved);
            foreach (var level in allLevels)
            {
                string localized = LocalizationResourceManager.Instance[$"PrivacyLevel{level}"].ToString();
                PrivacyLevels.Add(new KeyValuePair<string, PrivacyLevel>(localized, level));
            }
            SelectedPrivacyLevel = PrivacyLevels.First();
            Recipe.PrivacyLevel = SelectedPrivacyLevel.Value;
            LocalizationResourceManager.Instance.PropertyChanged += (sender, e) =>
            {
                OnLocalize();
            };
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void OnLocalize()
        {
            Recipe.OnLocalize();
            if (isEditMode)
            {
                Title = LocalizationResourceManager.Instance["UpdateRecipeTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["SaveChanges"].ToString();
            }
            else
            {
                Title = LocalizationResourceManager.Instance["CreateRecipeTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["Add"].ToString();
            }
            var currentValue = SelectedPrivacyLevel.Value;
            PrivacyLevels.Clear();
            var allLevels = Enum.GetValues<PrivacyLevel>() .Where(p => p != PrivacyLevel.Archieved);
            foreach (var level in allLevels)
            {
                string localized = LocalizationResourceManager.Instance[$"PrivacyLevel{level}"].ToString();
                PrivacyLevels.Add(new KeyValuePair<string, PrivacyLevel>(localized, level));
            }
            SelectedPrivacyLevel = PrivacyLevels.FirstOrDefault(p => p.Value == currentValue);
            Recipe.PrivacyLevel = SelectedPrivacyLevel.Value;
        }


        void UpdateChart()
        {
            var protein = (float)(Recipe?.TotalProteins ?? 0);
            var carbs = (float)(Recipe?.TotalCarbohydrates ?? 0);
            var fat = (float)(Recipe?.TotalFats ?? 0);

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
            OnPropertyChanged(nameof(MacroChart));
        }

        [RelayCommand]
        private async Task PickRecipeImage()
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

            Recipe.ImageSource = ImageSource.FromFile(localFilePath);

            if (isEditMode)
                isImageChanged = true;
        }

        [RelayCommand]
        public async Task Submit()
        {
            var validateResult = Validate();
            if (validateResult != null)
            {
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                await Shell.Current.DisplayAlertAsync(error, validateResult, ok);
                return;
            }

            if (!isEditMode)
            {
                await CreateRecipe();
            }
            else
            {
                await UpdateRecipe();
            }
        }

        private async Task CreateRecipe()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var result = await _recipeClient.CreateRecipeAsync(Recipe, _selectedImagePath);

                if (result.Success)
                {
                    message = String.Format(LocalizationResourceManager.Instance["CreatedRecipe"].ToString(), Recipe.Name);
                    _ = Toast.Make(message).Show();
                    await GoBack();
                }
                else
                {
                    message = LocalizationResourceManager.Instance[result.Error ?? "GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
            }
        }

        private async Task UpdateRecipe()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message;

            try
            {
                var result = await _recipeClient.UpdateRecipeAsync(Recipe, isImageChanged ? _selectedImagePath : null);
                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error ?? "GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    return;
                }

                message = String.Format(LocalizationResourceManager.Instance["UpdatedRecipe"].ToString(), Recipe.Name);
                _ = Toast.Make(message).Show();
                if (NavigationRecipe != null && NavigationRecipe?.Id != 0)
                {
                    NavigationRecipe.CopyFrom(Recipe);
                }
                else if (NavigationRecipeFood != null && NavigationRecipeFood?.Id != 0) 
                {
                    NavigationRecipeFood.Image = Recipe.Image;
                    NavigationRecipeFood.ImageSource = Recipe.ImageSource;
                    NavigationRecipeFood.Name = Recipe.Name;
                    NavigationRecipeFood.Calories = Recipe.NormalizedCalories;
                    NavigationRecipeFood.Fats = Recipe.NormalizedFats;
                    NavigationRecipeFood.Carbohydrates = Recipe.NormalizedCarbohydrates;
                    NavigationRecipeFood.Proteins = Recipe.NormalizedProteins;
                }
                WeakReferenceMessenger.Default.Send(new RecipeUpdatedMessage(NavigationRecipeFood, Recipe.PrivacyLevel));
                await GoBack();
            }
            catch (Exception ex)
            {
                message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                await Shell.Current.DisplayAlertAsync(error, message, ok);
                await Shell.Current.DisplayAlertAsync(error, ex.Message, ok);
            }
        }

        string? Validate()
        {
            if (!isEditMode && _selectedImagePath == null)
                return LocalizationResourceManager.Instance["RecipeImageMissing"].ToString();

            if (Recipe.Ingredients == null || !Recipe.Ingredients.Any())
            {
                return LocalizationResourceManager.Instance["RecipeNeedsIngredients"].ToString();
            }

            if (!string.IsNullOrWhiteSpace(Recipe.Name))
            {
                Recipe.Name = Regex.Replace(Recipe.Name, @"\s{2,}", " ").Trim();
            }
            const int minNameLength = 3;
            if (string.IsNullOrWhiteSpace(Recipe.Name) || Recipe.Name.Length < minNameLength)
            {
                return string.Format(
                    LocalizationResourceManager.Instance["RecipeNameTooShort"].ToString(), minNameLength);
            }

            if (!string.IsNullOrWhiteSpace(Recipe.Description))
            {
                Recipe.Description = Regex.Replace(Recipe.Description, @"[ ]{2,}", " ");
                Recipe.Description = Regex.Replace(Recipe.Description, @"(\r?\n){2,}", "\n");
                Recipe.Description = Recipe.Description.Trim();
            }
            const int minLength = 10;
            if (string.IsNullOrWhiteSpace(Recipe.Description) || Recipe?.Description?.Length < minLength)
            {
                return String.Format(LocalizationResourceManager.Instance["RecipeDescriptionTooShort"].ToString(), minLength);
            }

            return null;
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue($"{nameof(NavigationRecipe)}", out var obj) && obj is RecipeVM recipe)
            {
                NavigationRecipe = recipe;
                Recipe.CopyFrom(recipe);
                Title = LocalizationResourceManager.Instance["UpdateRecipeTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["SaveChanges"].ToString();
                isEditMode = true; 
                var selectedLevel = PrivacyLevels.FirstOrDefault(p => p.Value == recipe.PrivacyLevel);
                if (!selectedLevel.Equals(default(KeyValuePair<string, PrivacyLevel>)))
                {
                    SelectedPrivacyLevel = selectedLevel;
                }
            }
            else if (query.TryGetValue($"{nameof(NavigationRecipeFood)}", out var recipeFood) && recipeFood is FoodVM food)
            {
                Title = LocalizationResourceManager.Instance["UpdateRecipeTitle"].ToString();
                SubmitButtonText = LocalizationResourceManager.Instance["SaveChanges"].ToString();
                isEditMode = true;
                NavigationRecipeFood = food;
                string message;
                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                try
                {
                    var (result, detailedRecipe, _,_,_) = await _recipeClient.GetRecipeDetailsAsync(food.Id);
                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        await GoBack();
                        return;
                    }

                    Recipe = detailedRecipe;
                    var selectedLevel = PrivacyLevels.FirstOrDefault(p => p.Value == Recipe.PrivacyLevel);
                    if (!selectedLevel.Equals(default(KeyValuePair<string, PrivacyLevel>)))
                    {
                        SelectedPrivacyLevel = selectedLevel;
                    }
                }
                catch (Exception ex)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
            }

            UpdateChart();
            query.Clear();
        }

        [RelayCommand]
        public async Task GoBack()
        {
            if (!isEditMode && Recipe.Id !=0)
            {
                NavigationRecipeFood.Id = Recipe.Id;
                NavigationRecipeFood.FoodType = FoodType.RecipeFood;
                NavigationRecipeFood.Image = Recipe.Image;
                NavigationRecipeFood.ImageSource = Recipe.ImageSource; 
                NavigationRecipeFood.Calories = Recipe.NormalizedCalories;
                NavigationRecipeFood.Fats = Recipe.NormalizedFats;
                NavigationRecipeFood.Carbohydrates = Recipe.NormalizedCarbohydrates;
                NavigationRecipeFood.Proteins = Recipe.NormalizedProteins;
                NavigationRecipeFood.ExtraInfo = _user.PublicUser.Username;
                NavigationRecipeFood.Name = Recipe.Name;
                WeakReferenceMessenger.Default.Send(new RecipeCreatedMessage(NavigationRecipeFood, Recipe.PrivacyLevel));
            }

            await Shell.Current.GoToAsync("..", true);
        }


        [RelayCommand]
        public async Task SelectFood(FoodVM food)
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message = String.Format(LocalizationResourceManager.Instance["EnterWeightMessage"].ToString(),
               food.Name);
            string title = LocalizationResourceManager.Instance["EnterWeightTitle"].ToString();

            string result = await Shell.Current.DisplayPromptAsync(title, message, keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(result))
                return;

            if (!double.TryParse(result.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value) &&
                !double.TryParse(result.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return;

            if (value <= 0) return;

            double rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            if (rounded > 2000)
                rounded = 2000;

            RecipeIngredientVM newIgredient = new RecipeIngredientVM();
            newIgredient.Food.CopyFrom(food);
            newIgredient.Weight = rounded;
            Recipe.Ingredients.Add(newIgredient);
            newIgredient.RecalculateMacros();
            Recipe.RecalculateMacros();
            UpdateChart();
            message = String.Format(LocalizationResourceManager.Instance["IngredientAdded"].ToString(), rounded, food.Name);
            _ = Toast.Make(message).Show();
        }

        [RelayCommand]
        public async Task EditIngredient(RecipeIngredientVM ingredient)
        {
            if (_isDeletingIngredient) return;

            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string message = String.Format(LocalizationResourceManager.Instance["EnterWeightMessage"].ToString(),
                ingredient.Food.Name);
            string title = LocalizationResourceManager.Instance["EditWeight"].ToString();

            string result = await Shell.Current.DisplayPromptAsync(title, message,
             keyboard: Keyboard.Numeric, initialValue: ingredient.Weight.ToString());

            if (string.IsNullOrWhiteSpace(result))
                return;

            if (!double.TryParse(result.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double value) &&
                !double.TryParse(result.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value))
                return;

            if (value == ingredient.Weight || value <= 0 || (value >= 2000 && ingredient.Weight == 2000)) return;

            double rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
            if (rounded > 2000)
                rounded = 2000;

            ingredient.Weight = rounded;
            ingredient.RecalculateMacros();
            Recipe.RecalculateMacros();
            UpdateChart();
        }

        [RelayCommand]
        public async Task DeleteIngredient(RecipeIngredientVM ingredient)
        {
            _isDeletingIngredient = true;
            string alertTitle = String.Format(LocalizationResourceManager.Instance["DeleteName"].ToString(), ingredient.Food.Name);

            string message = String.Format(LocalizationResourceManager.Instance["DeleteIngredientMessage"].ToString(),
                ingredient.Weight.ToString(), ingredient.Food.Name);
            string accept = LocalizationResourceManager.Instance["Yes"].ToString();
            string decline = LocalizationResourceManager.Instance["No"].ToString();
            bool confirm = await Shell.Current.DisplayAlertAsync(alertTitle, message, accept, decline);

            if (!confirm)
            {
                _isDeletingIngredient = false;
                return;
            }

            Recipe.Ingredients.Remove(ingredient);
            Recipe.RecalculateMacros();
            UpdateChart();
            _isDeletingIngredient = false;
        }

        [RelayCommand]
        private async Task SelectPrivacyLevel()
        {
            if (!PrivacyLevels.Any())
                return;

            var options = PrivacyLevels.Select(p => p.Key).ToArray();
            string cancelText = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = LocalizationResourceManager.Instance["SelectPrivacyLevelTitle"].ToString();

            string selected = await Shell.Current.DisplayActionSheetAsync(title, cancelText, null, options);

            if (string.IsNullOrEmpty(selected) || selected == cancelText || selected == SelectedPrivacyLevel.Key)
                return;

            var selectedLevel = PrivacyLevels.First(p => p.Key == selected);
            SelectedPrivacyLevel = selectedLevel;
            Recipe.PrivacyLevel = selectedLevel.Value;
        }

        public void Receive(MacroThemeChanged message)
        {
            UpdateChart();
        }
    }
}
