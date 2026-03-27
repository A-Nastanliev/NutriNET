using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.Food
{
    public class FoodDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ExtraInfo { get; set; }

        public string Barcode { get; set; }

        public string Image { get; set; }

        [Range(0, 900)]
        public double Calories { get; set; }

        [Range(0, 100)]
        public double Proteins { get; set; }

        [Range(0, 100)]
        public double Carbohydrates { get; set; }

        [Range(0, 100)]
        public double Fats { get; set; }

        public FoodType FoodType { get; set; }

        public FoodDto() { }

        public FoodDto(int id, string name, string extraInfo, string barcode, string image, double calories, double proteins, double carbohydrates, double fats)
        {
            Id = id;
            Name = name;
            ExtraInfo = extraInfo;
            Barcode = barcode;
            Image = image;
            Calories = calories;
            Proteins = proteins;
            Carbohydrates = carbohydrates;
            Fats = fats;
            FoodType = FoodType.Food;
        }

        public FoodDto(int id, string name, string extraInfo, string image, double calories, double proteins, double carbohydrates, double fats, FoodType foodType)
        {
            Id = id;
            Name = name;
            ExtraInfo = extraInfo;
            Image = image;
            Calories = calories;
            Proteins = proteins;
            Carbohydrates = carbohydrates;
            Fats = fats;
            FoodType = foodType;
        }
    }
}
