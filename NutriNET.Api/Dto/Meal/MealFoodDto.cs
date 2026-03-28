using NutriNET.Data;
using NutriNET.Api.Dto.Food;

namespace NutriNET.Api.Dto.Meal
{
    public class MealFoodDto : INutritionalValue
    {
        public int Id { get; set; }
        public double Weight { get; set; }
        public FoodDto Food { get; set; }

        public MealFoodDto() { }

        public MealFoodDto(int id, double weight, FoodDto food)
        {
            Id = id;
            Weight = weight;
            Food = food;
        }

        public double GetCalories()
        {
            return Weight * Food.Calories / 100;
        }

        public double GetProteins()
        {
            return Weight * Food.Proteins / 100;
        }

        public double GetCarbohydrates()
        {
            return Weight * Food.Carbohydrates / 100;
        }

        public double GetFats()
        {
            return Weight * Food.Fats / 100;
        }
    }
}
