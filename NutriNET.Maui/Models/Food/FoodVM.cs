using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;

namespace NutriNET.Maui.Models.Food
{
    public partial class FoodVM : ObservableObject, IJsonParseable, ICopyable<FoodVM>
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string extraInfo;

        [ObservableProperty]
        private string barcode;

        [ObservableProperty]
        private string image;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CarbsRatio))]
        [NotifyPropertyChangedFor(nameof(ProteinRatio))]
        [NotifyPropertyChangedFor(nameof(FatRatio))]
        private double calories;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProteinCalories))]
        [NotifyPropertyChangedFor(nameof(ProteinRatio))]

        private double proteins;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CarbsCalories))]
        [NotifyPropertyChangedFor(nameof(CarbsRatio))]
        private double carbohydrates;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FatCalories))]
        [NotifyPropertyChangedFor(nameof(FatRatio))]
        private double fats;

        [ObservableProperty]
        private FoodType foodType;

        [ObservableProperty]
        private ImageSource imageSource;

        public double CarbsCalories => Carbohydrates * 4;
        public double ProteinCalories => Proteins * 4;
        public double FatCalories => Fats * 9;

        public double CarbsRatio => Calories == 0 ? 0 : (CarbsCalories / Calories);
        public double ProteinRatio => Calories == 0 ? 0 : (ProteinCalories / Calories);
        public double FatRatio=> Calories == 0 ? 0 : (FatCalories / Calories);

        public FoodVM() { }

        public void FromJson(JsonElement json)
        {
            Id = json.GetProperty("id").GetInt32();
            Name = json.GetProperty("name").GetString()!;
            ExtraInfo = json.GetProperty("extraInfo").GetString();
            Barcode = json.GetProperty("barcode").GetString();
            Image = json.GetProperty("image").GetString();

            Calories = json.GetProperty("calories").GetDouble();
            Proteins = json.GetProperty("proteins").GetDouble();
            Carbohydrates = json.GetProperty("carbohydrates").GetDouble();
            Fats = json.GetProperty("fats").GetDouble();

            Calories = Math.Round(Calories, 2, MidpointRounding.AwayFromZero);
            Carbohydrates = Math.Round(Carbohydrates, 2, MidpointRounding.AwayFromZero);
            Fats = Math.Round(Fats, 2, MidpointRounding.AwayFromZero);
            Proteins = Math.Round(Proteins, 2, MidpointRounding.AwayFromZero);

            FoodType = (FoodType)json.GetProperty("foodType").GetInt32();

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
        public void CopyFrom(FoodVM original)
        {
            if (original == null) return;

            Id = original.Id;
            Name = original.Name;
            ExtraInfo = original.ExtraInfo;
            Barcode = original.Barcode;
            Image = original.Image;

            Calories = original.Calories;
            Proteins = original.Proteins;
            Carbohydrates = original.Carbohydrates;
            Fats = original.Fats;

            FoodType = original.FoodType;
            ImageSource = original.ImageSource;
        }

    }
}
