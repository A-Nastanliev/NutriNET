using CommunityToolkit.Mvvm.Messaging.Messages;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;

namespace NutriNET.Maui.Messages.Recipes
{
    public class RecipeDeletedMessage : ValueChangedMessage<FoodVM>
    {
        public RecipeDeletedMessage(FoodVM recipeFood) : base(recipeFood) { }
    }
}
