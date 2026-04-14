using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public abstract partial class RecipesLoadingVM : PagedLoadingVM
    {
        [ObservableProperty]
        ObservableCollection<FoodVM> recipes = new();

        [ObservableProperty]
        ObservableCollection<FoodVM> currentRecipes = new();

        [ObservableProperty]
        string entrySearch;

        public DateTime? RecipesCursorDate { get; set; }

        CancellationTokenSource? _searchCts;

        protected readonly RecipeClient _recipeClient;

        protected RecipesLoadingVM(RecipeClient recipeClient)
        {
            _recipeClient = recipeClient;
        }

        protected abstract Task<(RequestResult result, List<FoodVM> recipes, DateTime? cursorDate, int? cursorKey)>
            FetchRecipes(int batchSize, DateTime? cursorDate, int? cursorId, string search);

        [RelayCommand]
        public virtual async Task SelectFood(FoodVM food)
        {
            await Shell.Current.GoToAsync(nameof(RecipeDetailPage), true, 
                new Dictionary<string, object> { [nameof(RecipeDetailVM.NavigationRecipeFood)] = food });
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
                var (result, recipes, cursorDate, cursorKey) = await FetchRecipes(BatchSize, CursorDate, CursorId, EntrySearch);

                if (!result.Success)
                {
                    message = LocalizationResourceManager.Instance[result.Error].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                    Loading = false;
                    return;
                }

                if (recipes.Any())
                {
                    if (string.IsNullOrWhiteSpace(EntrySearch))
                    {
                        RecipesCursorDate = cursorDate;
                        foreach (var r in recipes)
                        {
                            CurrentRecipes.Add(r);
                            Recipes.Add(r);
                        }
                    }
                    else
                    {
                        foreach (var r in recipes)
                        {
                            CurrentRecipes.Add(r);
                        }
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
                    CurrentRecipes.Clear();
                    CanLoadMore = true;

                    if (string.IsNullOrWhiteSpace(search))
                    {
                        foreach (var r in Recipes)
                        {
                            CurrentRecipes.Add(r);
                        }
                        CursorDate = RecipesCursorDate;
                        CursorId = Recipes.LastOrDefault()?.Id;
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

        public Task Clear()
        {
            RecipesCursorDate = null;
            CursorDate = null;
            CursorId = null;
            CanLoadMore = true;
            CurrentRecipes.Clear();
            Recipes.Clear();
            EntrySearch = null;
            return Task.CompletedTask;
        }
    }
}
