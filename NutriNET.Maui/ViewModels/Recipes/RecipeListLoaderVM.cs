using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class RecipeListLoaderVM : PagedLoadingVM, IQueryAttributable
    {
        [ObservableProperty]
        RecipeListVM recipeList = new();

        [ObservableProperty]
        ObservableCollection<FoodVM> recipes = new ();

        readonly RecipeClient _recipeClient;
        bool _isHolding;

        public RecipeListLoaderVM(RecipeClient recipeClient)
        {
            _recipeClient = recipeClient;
        }

        public RecipeListLoaderVM(RecipeClient recipeClient, RecipeListVM recipeList)
        {
            RecipeList = recipeList;
            _recipeClient = recipeClient;
        }

        [RelayCommand]
        public async Task ViewRecipeList()
        {
            await Shell.Current.GoToAsync(nameof(RecipeListPage), true, new Dictionary<string, object> { [nameof(RecipeListLoaderVM)] = this });
        }

        [RelayCommand]
        public async Task SelectFood(FoodVM food)
        {
            if (_isHolding) return;

            await Shell.Current.GoToAsync(nameof(RecipeDetailPage), true,
                new Dictionary<string, object> { [nameof(RecipeDetailVM.NavigationRecipeFood)] = food });
        }
        [RelayCommand]
        public async Task RemoveFromList(FoodVM food)
        {
            _isHolding = true;
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string cancel = LocalizationResourceManager.Instance["Cancel"].ToString();
            string yes = LocalizationResourceManager.Instance["Yes"].ToString();
            string no = LocalizationResourceManager.Instance["No"].ToString();
            string title = string.Format(LocalizationResourceManager.Instance["RemoveFromListTitle"].ToString(), food.Name);
            string message = string.Format(LocalizationResourceManager.Instance["RemoveFromListMessage"].ToString(), food.Name);

            bool confirmed = await Shell.Current.DisplayAlertAsync(title, message, yes, no);
            if (!confirmed) 
            {
                _isHolding = false;
                return; 
            }

            try
            {
                var result = await _recipeClient.DeleteRecipeListItemAsync(RecipeList.Id, food.Id);
                if (result.Success)
                {
                    Recipes.Remove(food);
                    message = String.Format(LocalizationResourceManager.Instance["RemovedRecipeListItem"].ToString(), food.Name);
                    _ = Toast.Make(message, ToastDuration.Short).Show();
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
            finally
            {
                _isHolding = false;
            }
        }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue($"{nameof(RecipeListLoaderVM)}", out var obj) && obj is RecipeListLoaderVM recipeLoader)
            {
                RecipeList = recipeLoader.RecipeList;
                Recipes = recipeLoader.Recipes;
                CursorDate = recipeLoader.CursorDate;
                CursorId = recipeLoader.CursorId;
                CanLoadMore = recipeLoader.CanLoadMore;
                query.Clear();
            }
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
                var (result, recipes, cursorDate, cursorKey) =  await _recipeClient.GetNextRecipesInListAsync(RecipeList.Id, BatchSize, CursorDate, CursorId);

                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    Loading = false;
                    return;
                }

                if (recipes.Any())
                {
                    foreach (var r in recipes)
                    {
                        Recipes.Add(r);
                    }

                    EndLoading(recipes.Count, cursorDate, cursorKey);
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
                Recipes.Clear();
                await Load();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        public async Task UpdateName()
        {
            string error = LocalizationResourceManager.Instance["Error"].ToString();
            string ok = LocalizationResourceManager.Instance["Ok"].ToString();
            string title = LocalizationResourceManager.Instance["UpdateListTitle"].ToString();
            string message = LocalizationResourceManager.Instance["UpdateListMessage"].ToString();
            string cancel = LocalizationResourceManager.Instance["Cancel"].ToString();

            string? name = await Shell.Current.DisplayPromptAsync(title, message, ok, cancel,
                initialValue: RecipeList.Name, maxLength: 24, keyboard: Keyboard.Text);

            if (name == null) return;

            name = name.Trim();
            name = Regex.Replace(name, @" {2,}", " ");

            if (name.Length < 1)
            {
                string tooShort = LocalizationResourceManager.Instance["ListNameTooShort"].ToString();
                await Shell.Current.DisplayAlertAsync(error, tooShort, ok);
                return;
            }

            if (name == RecipeList.Name) return;

            try
            {
                string temp = RecipeList.Name;
                RecipeList.Name = name;
                var result = await _recipeClient.UpdateRecipeListAsync(RecipeList);
                if (!result.Success)
                {
                    RecipeList.Name = temp;
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
    }
}
