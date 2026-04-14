using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualBasic;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Managers;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using System.Collections.ObjectModel;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class RecipeCatalogVM : RecipesLoadingVM, IRecipient<RecipeCreatedMessage>, IRecipient<RecipeUpdatedMessage>, IRecipient<RecipeDeletedMessage>
    {
        public RecipeCatalogVM(RecipeClient recipeClient) : base(recipeClient)
        {
            UserClient.OnLogout += Clear;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(RecipeCreatedMessage message)
        {
            if (message.PrivacyLevel == PrivacyLevel.Public)
            {
                var recipeFood = message.Value;
                Recipes.Insert(0, recipeFood);
                if (string.IsNullOrWhiteSpace(EntrySearch) || EntrySearch.Contains(recipeFood.Name))
                {
                    CurrentRecipes.Insert(0, recipeFood);
                }
            }
        }

        public void Receive(RecipeUpdatedMessage message)
        {
            var isPublic = message.PrivacyLevel == PrivacyLevel.Public;

            var existing = Recipes.FirstOrDefault(r => r.Id == message.Value.Id);

            if (isPublic)
            {
                if (existing != null)
                {
                    var index = Recipes.IndexOf(existing);
                    Recipes[index] = message.Value;
                }
                else if(!CanLoadMore)
                {
                    Recipes.Add(message.Value);
                }
            }
            else
            {
                if (existing != null)
                {
                    Recipes.Remove(existing);
                }
            }

            if(string.IsNullOrWhiteSpace(EntrySearch) || EntrySearch.Contains(message.Value.Name)) 
            {
                existing = CurrentRecipes.FirstOrDefault(r => r.Id == message.Value.Id);
                if (isPublic)
                {
                    if (existing != null)
                    {
                        var index = CurrentRecipes.IndexOf(existing);
                        CurrentRecipes[index] = message.Value;
                    }
                    else if (!CanLoadMore)
                    {
                        CurrentRecipes.Add(message.Value);
                    }
                }
                else
                {
                    if (existing != null)
                    {
                        CurrentRecipes.Remove(existing);
                    }
                } }
        }

        public void Receive(RecipeDeletedMessage message)
        {
            var existing = Recipes.FirstOrDefault(r => r.Id == message.Value.Id);
            if(existing != null)
            {
                Recipes.Remove(existing);
            }
            existing = CurrentRecipes.FirstOrDefault(r=>r.Id == message.Value.Id);
            if(existing != null)
            {
                Recipes.Remove(existing);
            }
            
        }

        protected override Task<(RequestResult, List<FoodVM>, DateTime?, int?)> FetchRecipes(
            int batchSize, DateTime? cursorDate, int? cursorId, string search)
        {
            return _recipeClient.GetNextPublicRecipesAsync(batchSize, cursorDate, cursorId, search);
        }
    }
}
