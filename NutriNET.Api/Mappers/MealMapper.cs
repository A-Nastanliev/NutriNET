using NutriNET.Data.Models;
using NutriNET.Api.Dto.Meal;
using NutriNET.Api.Dto.Food;

namespace NutriNET.Api.Mappers
{
    public static class MealMapper
    {
        public static MealDto ToDto(this Meal meal, string baseUrl)
        {
            List<MealFoodDto> mealFoodDtos = new List<MealFoodDto>();
            foreach(var mf in meal.MealFoods)
            {
                mealFoodDtos.Add(mf.ToDto(baseUrl));
            }
            return new MealDto(meal.Id, meal.Type, meal.DateTime, mealFoodDtos );
        }

        public static MealFoodDto ToDto(this MealFood mealFood, string baseUrl)
        {
            FoodDto foodDto = new FoodDto();
            if (mealFood.Recipe != null)
            {
                foodDto = mealFood.Recipe.ToFoodDto(baseUrl);
            }
            else 
            {
                foodDto = mealFood.Food.ToDto(baseUrl);
            }

            return new MealFoodDto(mealFood.Id, mealFood.Weight, foodDto);
        }
    }
}
