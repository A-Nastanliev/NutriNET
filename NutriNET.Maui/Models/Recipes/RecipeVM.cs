using CommunityToolkit.Mvvm.ComponentModel;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Meal;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.Models.Recipes
{
    public partial class RecipeVM : ObservableObject, IJsonParseable, ICopyable<RecipeVM>, INutritionalValue, ILocalize
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private PrivacyLevel privacyLevel;

        [ObservableProperty]
        private DateTime date;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string image;

        [ObservableProperty]
        private ImageSource imageSource;

        [ObservableProperty]
        private PublicUserVM creator;

        [ObservableProperty]
        private ObservableCollection<RecipeIngredientVM> ingredients = new();

        public double TotalCalories => Math.Round(GetCalories(), 2, MidpointRounding.AwayFromZero);
        public double TotalCarbohydrates => Math.Round(GetCarbohydrates(), 2, MidpointRounding.AwayFromZero);
        public double TotalFats => Math.Round(GetFats(), 2, MidpointRounding.AwayFromZero);
        public double TotalProteins => Math.Round(GetProteins(), 2, MidpointRounding.AwayFromZero);
        public double TotalWeight => Math.Round(GetWeight(), 2, MidpointRounding.AwayFromZero);

        public double NormalizedCalories => Math.Round(GetCalories() * WeightRatio, 2, MidpointRounding.AwayFromZero);
        public double NormalizedCarbohydrates => Math.Round(GetCarbohydrates() * WeightRatio, 2, MidpointRounding.AwayFromZero);
        public double NormalizedFats => Math.Round(GetFats() * WeightRatio, 2, MidpointRounding.AwayFromZero);
        public double NormalizedProteins => Math.Round(GetProteins() * WeightRatio, 2, MidpointRounding.AwayFromZero);

        private double WeightRatio => TotalWeight > 0 ? 100.0 / TotalWeight : 0;

        public RecipeVM()
        {
            Creator = new PublicUserVM();
            Ingredients = new ObservableCollection<RecipeIngredientVM>();
        }

        public void CopyFrom(RecipeVM original)
        {
            Ingredients.Clear();
            foreach(var ing in original.Ingredients)
            {
                var vm = new RecipeIngredientVM();
                vm.CopyFrom(ing);
                Ingredients.Add(vm);
            }

            Creator = original.Creator;
            Image = original.Image;
            ImageSource = original.ImageSource;
            Description = original.Description;
            PrivacyLevel = original.PrivacyLevel;
            Name = original.Name;
            Id = original.Id;
        }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            PrivacyLevel = (PrivacyLevel)json.GetProperty("privacyLevel").GetInt32();
            Date = json.GetProperty("date").GetDateTime();
            Name = json.GetProperty("name").GetString() ?? string.Empty;
            Description = json.GetProperty("description").GetString() ?? string.Empty;

            if (json.TryGetProperty("creator", out var creatorJson) &&
                creatorJson.ValueKind == JsonValueKind.Object &&
                creatorJson.EnumerateObject().Any())
            {
                Creator ??= new PublicUserVM();
                Creator.FromJson(creatorJson);
            }

            Ingredients.Clear();

            if (json.TryGetProperty("ingredients", out var ingredientsJson) &&
                ingredientsJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var ingredientJson in ingredientsJson.EnumerateArray())
                {
                    var ingredient = new RecipeIngredientVM();
                    ingredient.FromJson(ingredientJson);
                    Ingredients.Add(ingredient);
                }
            }

            Image = json.GetProperty("image").GetString();
            if (!string.IsNullOrWhiteSpace(Image))
            {
                try
                {
                    ImageSource = ImageSource.FromUri(new Uri(Image));
                }
                catch
                {
                    ImageSource = null;
                }
            }
            else
            {
                ImageSource = null;
            }
        }

        public double GetCalories()
        {
            double total = 0;
            foreach (INutritionalValue m in Ingredients)
            {
                total += m.GetCalories();
            }
            return total;
        }

        public double GetCarbohydrates()
        {
            double total = 0;
            foreach (INutritionalValue m in Ingredients)
            {
                total += m.GetCarbohydrates();
            }
            return total;
        }

        public double GetFats()
        {
            double total = 0;
            foreach (INutritionalValue m in Ingredients)
            {
                total += m.GetFats();
            }
            return total;
        }

        public double GetProteins()
        {
            double total = 0;
            foreach (INutritionalValue m in Ingredients)
            {
                total += m.GetProteins();
            }
            return total;
        }

        public double GetWeight()
        {
            double total = 0;
            foreach (RecipeIngredientVM m in Ingredients)
            {
                total += m.Weight;
            }
            return total;
        }

        public void RecalculateMacros()
        {
            OnPropertyChanged(nameof(TotalCalories));
            OnPropertyChanged(nameof(TotalCarbohydrates));
            OnPropertyChanged(nameof(TotalFats));
            OnPropertyChanged(nameof(TotalProteins));
            OnPropertyChanged(nameof(TotalWeight));
            OnPropertyChanged(nameof(NormalizedCalories));
            OnPropertyChanged(nameof(NormalizedCarbohydrates));
            OnPropertyChanged(nameof(NormalizedFats));
            OnPropertyChanged(nameof(NormalizedProteins));
        }

        public void OnLocalize()
        {
            OnPropertyChanged(nameof(PrivacyLevel));
            OnPropertyChanged(nameof(Date));
        }
    }
}
