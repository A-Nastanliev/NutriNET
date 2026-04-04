using NutriNET.Api.Dto.Food;
using NutriNET.Api.Dto.Recipe;
using NutriNET.Data.Models;

namespace NutriNET.Api.Mappers
{
    public static class RecipeMapper
    {
        public static RecipeDto ToDto(this Recipe recipe, string baseUrl)
        {
            return new RecipeDto(recipe.Id, recipe.Name, recipe.PrivacyLevel, recipe.Date, recipe.Description, recipe.Image == null ? null : $"{baseUrl}/{recipe.Image}",
                recipe.Creator.ToPublicDto(baseUrl), recipe.Ingredients.Select(i => i.ToDto(baseUrl)).ToList());
        }

        public static FoodDto ToFoodDto(this Recipe recipe, string baseUrl)
        {
            return new FoodDto(recipe.Id, recipe.Name, recipe.Creator.Username, 
                recipe.Image == null ? null : $"{baseUrl}/{recipe.Image}", 
                recipe.GetCalories(), recipe.GetProteins(), recipe.GetCarbohydrates(), recipe.GetFats(), FoodType.RecipeFood);
        }

        public static RecipeCommentDto ToDto(this RecipeComment comment, string baseUrl)
        {
            return new RecipeCommentDto(comment.Id, comment.Comment, comment.User?.ToPublicDto(baseUrl), comment.Date);
        }

        public static RecipeIngredientDto ToDto(this RecipeIngredient ingredient, string baseUrl)
        {
            return new RecipeIngredientDto(ingredient.Id, ingredient.Weight, ingredient.Food.ToDto(baseUrl));
        }

        public static RecipeListDto ToDto(this RecipeList list)
        {
            return new RecipeListDto { Id = list.Id, Name = list.Name };
        }
    }
}
