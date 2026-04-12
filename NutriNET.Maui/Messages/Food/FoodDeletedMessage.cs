using CommunityToolkit.Mvvm.Messaging.Messages;

namespace NutriNET.Maui.Messages.Food
{
    public class FoodDeletedMessage : ValueChangedMessage<int>
    {
        public FoodDeletedMessage(int foodId) : base(foodId) { }
    }
}
