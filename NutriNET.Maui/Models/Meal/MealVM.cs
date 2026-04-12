using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Models.Food;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.Meal
{
    public partial class MealVM : ObservableObject, IJsonParseable, INutritionalValue, ILocalize
    {
        [ObservableProperty]
        int id;

        [ObservableProperty]
        MealType type;

        [ObservableProperty]
        DateTime dateTime;

        [ObservableProperty]
        ObservableCollection<MealFoodVM> mealFoods = new();

        public double Calories => Math.Round(GetCalories(), 2, MidpointRounding.AwayFromZero);
        public double Carbohydrates => Math.Round(GetCarbohydrates(), 2, MidpointRounding.AwayFromZero);
        public double Fats => Math.Round(GetFats(), 2, MidpointRounding.AwayFromZero);
        public double Proteins => Math.Round(GetProteins(), 2, MidpointRounding.AwayFromZero);

        public MealVM()
        {
            MealFoods = new ObservableCollection<MealFoodVM>();
        }

        public MealVM(MealType type)
        {
            MealFoods = new ObservableCollection<MealFoodVM>();
            Type = type;
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(DateTime));
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
            Type = (MealType)json.GetProperty("type").GetInt32();
            DateTime = json.GetProperty("dateTime").GetDateTime();
            MealFoods.Clear();
            if (json.TryGetProperty("mealFoodDtos", out var mealFoodsJson) &&
                mealFoodsJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in mealFoodsJson.EnumerateArray())
                {
                    var mealFood = new MealFoodVM();
                    mealFood.FromJson(item);
                    MealFoods.Add(mealFood);
                }
            }
            RecalculateMacros();
        }

        public double GetCalories()
        {
            double total = 0;
            foreach(INutritionalValue m in MealFoods)
            {
                total += m.GetCalories();
            }
            return total;
        }

        public double GetCarbohydrates()
        {
            double total = 0;
            foreach (INutritionalValue m in MealFoods)
            {
                total += m.GetCarbohydrates();
            }
            return total;
        }

        public double GetFats()
        {
            double total = 0;
            foreach (INutritionalValue m in MealFoods)
            {
                total += m.GetFats();
            }
            return total;
        }

        public double GetProteins()
        {
            double total = 0;
            foreach (INutritionalValue m in MealFoods)
            {
                total += m.GetProteins();
            }
            return total;
        }
    }
}
