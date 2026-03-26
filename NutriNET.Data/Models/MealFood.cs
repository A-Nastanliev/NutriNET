namespace NutriNET.Data.Models
{
    public class MealFood : INutritionalValue
    {
        [Key]
        public int Id { get; set; }

        [Range(0.01, 10_000)]
        public double Weight { get; set; } 

        public int? RecipeId { get; set; }
        [ForeignKey(nameof(RecipeId))]
        public Recipe Recipe { get; set; }

        public int? FoodId { get; set; }
        [ForeignKey(nameof(FoodId))]
        public Food Food { get; set; }

        public int MealId { get; set; }
        [ForeignKey(nameof(MealId))]
        public Meal Meal { get; set; }


        private INutritionalValue currentFood;

        [NotMapped]
        public INutritionalValue CurrentFood
        {
            get
            {
                if (currentFood is not null)
                    return currentFood;

                currentFood = (INutritionalValue?)Food ?? Recipe;

                return currentFood;
            }
            set => currentFood = value;
        }

        public double GetCalories()
        {
            return CurrentFood.GetCalories() * Weight / 100;
        }

        public double GetCarbohydrates()
        {
            return CurrentFood.GetCarbohydrates() * Weight / 100;
        }

        public double GetFats()
        {
            return CurrentFood.GetFats() * Weight / 100;
        }

        public double GetProteins()
        {
            return CurrentFood.GetProteins() * Weight / 100;
        }
    }
}
