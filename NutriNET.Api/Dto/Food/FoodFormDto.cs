using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.Food
{
    public class FoodFormDto
    {
        public string Name { get; set; }

        public string ExtraInfo { get; set; }

        public string Barcode { get; set; }

        [Range(0, 900)]
        public double Calories { get; set; }

        [Range(0, 100)]
        public double Proteins { get; set; }

        [Range(0, 100)]
        public double Carbohydrates { get; set; }

        [Range(0, 100)]
        public double Fats { get; set; }

        public IFormFile? Image { get; set; }
    }
}
