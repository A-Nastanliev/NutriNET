using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.User;
using System.Text.Json;

namespace NutriNET.Maui.Models.Meal
{
    public partial class MealFoodVM : ObservableObject, IJsonParseable, INutritionalValue
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private double weight;

        [ObservableProperty]
        private FoodVM food;

        public double Calories => Math.Round(GetCalories(), 2, MidpointRounding.AwayFromZero);
        public double Carbohydrates => Math.Round(GetCarbohydrates(), 2, MidpointRounding.AwayFromZero);
        public double Fats => Math.Round(GetFats(), 2, MidpointRounding.AwayFromZero);
        public double Proteins => Math.Round(GetProteins(), 2, MidpointRounding.AwayFromZero);

        public MealFoodVM() 
        {
            Food= new FoodVM();
        }

        public MealFoodVM(double weight, FoodVM food)
        {
            Weight = weight;
            Food = food;
        }

        public void RecalculateMacros()
        {
            OnPropertyChanged(nameof(Calories));
            OnPropertyChanged(nameof(Carbohydrates));
            OnPropertyChanged(nameof(Fats));
            OnPropertyChanged(nameof(Proteins));
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Weight = json.GetProperty("weight").GetDouble();
            if (json.TryGetProperty("food", out var foodJson) && foodJson.ValueKind == JsonValueKind.Object
                && foodJson.EnumerateObject().Any())
            {
                Food ??= new FoodVM();
                Food.FromJson(foodJson);
            }
            RecalculateMacros();
        }

        public double GetCalories()
        {
            return Food.Calories * Weight / 100;
        }

        public double GetCarbohydrates()
        {
            return Food.Carbohydrates * Weight / 100;
        }

        public double GetFats()
        {
            return Food.Fats * Weight / 100;
        }

        public double GetProteins()
        {
            return Food.Proteins * Weight / 100;
        }
    }
}
