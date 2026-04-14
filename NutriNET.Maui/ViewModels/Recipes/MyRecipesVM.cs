using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Authentication;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Food;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.ViewModels.Food;
using NutriNET.Maui.Views.Recipes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class MyRecipesVM : RecipesLoadingVM, IRecipient<RecipeCreatedMessage>
    {
        bool _isHolding;

        public MyRecipesVM(RecipeClient recipeClient) : base(recipeClient)
        {
            UserClient.OnLogout += Clear;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        protected override Task<(RequestResult, List<FoodVM>, DateTime?, int?)> FetchRecipes(
            int batchSize, DateTime? cursorDate, int? cursorId, string search)
        {
            return _recipeClient.GetMyRecipesAsync(batchSize, cursorDate, cursorId);
        }

        [RelayCommand]
        public async Task AddRecipe()
        {
            await Shell.Current.GoToAsync(nameof(RecipeFormPage));
        }

        [RelayCommand]
        public async Task OpenSavedRecipes()
        {
            await Shell.Current.GoToAsync(nameof(SavedRecipesPage));
        }

        public async override Task SelectFood(FoodVM food)
        {
            if (_isHolding) return;

            await Shell.Current.GoToAsync(nameof(RecipeDetailPage), true,
                new Dictionary<string, object> { [nameof(RecipeDetailVM.NavigationRecipeFood)] = food });
        }

        [RelayCommand]
        public async Task HoldMyRecipe(FoodVM recipe)
        {
            _isHolding = true;
            string edit = LocalizationResourceManager.Instance["Edit"].ToString();
            string delete = LocalizationResourceManager.Instance["Delete"].ToString();
            string cancel = LocalizationResourceManager.Instance["Cancel"].ToString();
            string title = string.Format(LocalizationResourceManager.Instance["ManageRecipeTitle"].ToString(), recipe.Name);

            string action = await Shell.Current.DisplayActionSheetAsync(title, cancel, null, edit, delete);

            if (action == cancel || action == null)
            {
                _isHolding = false;
                return;
            }
            else if (action == delete) 
            {
                title = string.Format(LocalizationResourceManager.Instance["DeleteRecipeTitle"].ToString(), recipe.Name);
                string confirmMessage = LocalizationResourceManager.Instance["DeleteRecipeMessage"].ToString();

                string yes = LocalizationResourceManager.Instance["Yes"].ToString();
                string no = LocalizationResourceManager.Instance["No"].ToString();

                bool confirm = await Shell.Current.DisplayAlertAsync(title, confirmMessage, yes, no);

                if (!confirm)
                    return;

                string error = LocalizationResourceManager.Instance["Error"].ToString();
                string ok = LocalizationResourceManager.Instance["Ok"].ToString();
                string message;

                try
                {
                    var result = await _recipeClient.DeleteRecipeAsync(recipe.Id);

                    if (!result.Success)
                    {
                        message = LocalizationResourceManager.Instance[result.Error].ToString();
                        await Shell.Current.DisplayAlertAsync(error, message, ok);
                        return;
                    }

                    message = string.Format(LocalizationResourceManager.Instance["DeletedRecipe"].ToString(), recipe.Name);
                    _ = Toast.Make(message).Show();
                    WeakReferenceMessenger.Default.Send(new RecipeDeletedMessage(recipe));
                    Recipes.Remove(recipe);
                    CurrentRecipes.Remove(recipe);
                }
                catch (Exception)
                {
                    message = LocalizationResourceManager.Instance["GenericErrorMessage"].ToString();
                    await Shell.Current.DisplayAlertAsync(error, message, ok);
                }
                finally
                {
                    _isHolding = false;
                }
            }
            else if(action == edit)
            {
                _isHolding = false;
                await Shell.Current.GoToAsync(nameof(RecipeFormPage), true, new Dictionary<string, object> { [nameof(RecipeFormVM.NavigationRecipeFood)] = recipe });
            }
        }

        public void Receive(RecipeCreatedMessage message)
        {
            var recipeFood = message.Value;
            Recipes.Insert(0, recipeFood);
            if (string.IsNullOrWhiteSpace(EntrySearch) || EntrySearch.Contains(recipeFood.Name))
            {
                CurrentRecipes.Insert(0, recipeFood);
            }
        }
    }
}
