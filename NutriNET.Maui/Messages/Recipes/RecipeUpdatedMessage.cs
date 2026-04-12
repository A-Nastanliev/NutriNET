using CommunityToolkit.Mvvm.Messaging.Messages;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;

namespace NutriNET.Maui.Messages.Recipes
{
    public class RecipeUpdatedMessage : ValueChangedMessage<FoodVM>
    {
        public PrivacyLevel PrivacyLevel { get; set; }
        public RecipeUpdatedMessage(FoodVM recipeFood, PrivacyLevel privacyLevel) : base(recipeFood)
        {
            PrivacyLevel = privacyLevel;
        }
    }
}
