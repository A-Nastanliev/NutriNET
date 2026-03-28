using NutriNET.Data.Models;
using NutriNET.Api.Dto.Food;

namespace NutriNET.Api.Mappers
{
    public static class RecipeMapper
    {
        public static FoodDto ToFoodDto(this Recipe recipe, string baseUrl)
        {
            return new FoodDto(recipe.Id, recipe.Name, recipe.Creator.Username, 
                recipe.Image == null ? null : $"{baseUrl}/{recipe.Image}", 
                recipe.GetCalories(), recipe.GetProteins(), recipe.GetCarbohydrates(), recipe.GetFats(), FoodType.RecipeFood);
        }
    }
}
