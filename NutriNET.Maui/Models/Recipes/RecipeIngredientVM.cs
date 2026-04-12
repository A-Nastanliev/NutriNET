using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Models.Food;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.Recipes
{
    public partial class RecipeIngredientVM : ObservableObject, IJsonParseable, INutritionalValue, ICopyable<RecipeIngredientVM>
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

        public RecipeIngredientVM()
        {
            Food = new FoodVM();
        }

        public RecipeIngredientVM(int id, double weight, FoodVM food)
        {
            Id = id;
            Weight = weight;
            Food = food;
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Weight = json.GetProperty("weight").GetDouble();

            if (json.TryGetProperty("food", out var foodJson) &&
                foodJson.ValueKind == JsonValueKind.Object &&
                foodJson.EnumerateObject().Any())
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

        public void RecalculateMacros()
        {
            OnPropertyChanged(nameof(Calories));
            OnPropertyChanged(nameof(Carbohydrates));
            OnPropertyChanged(nameof(Fats));
            OnPropertyChanged(nameof(Proteins));
        }

        public void CopyFrom(RecipeIngredientVM original)
        {
            Food.CopyFrom(original.Food);
            Id = original.Id;
            Weight = original.Weight;
        }
    }
}
