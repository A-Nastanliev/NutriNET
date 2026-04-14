using CommunityToolkit.Mvvm.Messaging;
using NutriNET.Maui.ApiClients;
using NutriNET.Maui.Messages.Recipes;
using NutriNET.Maui.Models.Food;

namespace NutriNET.Maui.ViewModels.Recipes
{
    public partial class FollowingRecipesVM : RecipesLoadingVM, IRecipient<FollowChangedMessage>
    {
        public FollowingRecipesVM(RecipeClient recipeClient) : base(recipeClient)
        {
            UserClient.OnLogout += Clear;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(FollowChangedMessage message)
        {
            Clear();
        }

        protected override Task<(RequestResult, List<FoodVM>, DateTime?, int?)> FetchRecipes(
            int batchSize, DateTime? cursorDate, int? cursorId, string search)
        {
            return _recipeClient.GetNextFollowingRecipesAsync(batchSize, cursorDate, cursorId, search);
        }
    }
}