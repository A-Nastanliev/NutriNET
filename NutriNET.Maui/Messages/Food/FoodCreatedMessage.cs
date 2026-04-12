using CommunityToolkit.Mvvm.Messaging.Messages;
using NutriNET.Maui.Models.Food;

namespace NutriNET.Maui.Messages.Food
{
    public class FoodCreatedMessage : ValueChangedMessage<FoodVM>
    {
        public FoodCreatedMessage(FoodVM food) : base(food) { }
    }
}
