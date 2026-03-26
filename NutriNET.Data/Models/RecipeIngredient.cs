namespace NutriNET.Data.Models
{
    public class RecipeIngredient : INutritionalValue
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 10_000)]
        public double Weight { get; set; }

        public int RecipeId { get; set; }
        [ForeignKey(nameof(RecipeId))]
        public Recipe Recipe { get; set; }

        public int FoodId { get; set; }
        [ForeignKey(nameof(FoodId))]
        public Food Food { get; set; }

        public double GetCalories()
        {
            return Food.Calories * Weight / 100;
        }

        public double GetProteins()
        {
            return Food.Proteins * Weight / 100;
        }

        public double GetCarbohydrates()
        {
            return Food.Carbohydrates * Weight / 100;
        }

        public double GetFats()
        {
            return Food.Fats * Weight / 100;
        }
    }
}
