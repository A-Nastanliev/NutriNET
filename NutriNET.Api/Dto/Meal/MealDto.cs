using NutriNET.Data;
using NutriNET.Data.Enums ;

namespace NutriNET.Api.Dto.Meal
{
    public class MealDto : INutritionalValue
    {
        public int Id { get; set; }

        public MealType Type { get; set; }

        public DateTime DateTime { get; set; }
        public List<MealFoodDto> MealFoodDtos { get; set; } = new();

        public MealDto() { }

        public MealDto(int id, MealType type, DateTime dateTime, List<MealFoodDto> mealFoodDtos)
        {
            Id = id;
            Type = type;
            DateTime = dateTime;
            MealFoodDtos = mealFoodDtos;
        }

        public double GetCalories()
        {
            double calories = 0;
            foreach (MealFoodDto mfd in MealFoodDtos)
            {
                calories += mfd.GetCalories();
            }
            return calories;
        }

        public double GetProteins()
        {
            double proteins = 0;
            foreach (MealFoodDto mfd in MealFoodDtos)
            {
                proteins += mfd.GetProteins();
            }
            return proteins;
        }

        public double GetCarbohydrates()
        {
            double carbs = 0;
            foreach (MealFoodDto mfd in MealFoodDtos)
            {
                carbs += mfd.GetCarbohydrates();
            }
            return carbs;
        }

        public double GetFats()
        {
            double fats = 0;
            foreach (MealFoodDto mfd in MealFoodDtos)
            {
                fats += mfd.GetFats();
            }
            return fats;
        }
    }
}
