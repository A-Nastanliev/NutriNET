using CommunityToolkit.Mvvm.Messaging.Messages;
using NutriNET.Maui.Models.Food;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.Messages.Recipes
{
    public class RecipeListItemAddedMessage : ValueChangedMessage<FoodVM>
    {
        public int ListId { get; set; }
        public RecipeListItemAddedMessage(FoodVM recipeFood, int listId) : base(recipeFood) 
        {
            ListId = listId;
        }
    }
}
