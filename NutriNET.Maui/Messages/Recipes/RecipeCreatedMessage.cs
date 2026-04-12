using CommunityToolkit.Mvvm.Messaging.Messages;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;

namespace NutriNET.Maui.Messages.Recipes
{
    public class RecipeCreatedMessage : ValueChangedMessage<FoodVM>
    {
        public PrivacyLevel PrivacyLevel { get; set; }
        public RecipeCreatedMessage(FoodVM recipeFood, PrivacyLevel privacyLevel) : base(recipeFood) 
        {
            PrivacyLevel = privacyLevel;
        }
    }
}
