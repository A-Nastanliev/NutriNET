namespace NutriNET.Data.Models
{
    public class Recipe : INutritionalValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Name { get; set; }

        public PrivacyLevel PrivacyLevel { get; set; }
        
        public string? Image { get; set;  }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(2_000)]
        public string Description { get; set; }

        public int? CreatorId { get; set; }
        [ForeignKey(nameof(CreatorId))]
        public User Creator { get; set; }

        [Required]
        public List<RecipeListItem> RecipeListItems { get; set; } = new();

        [Required]
        public List<MealFood> MealFoods { get; set; } = new();

        [Required]
        public List<RecipeIngredient> Ingredients { get; set; } = new();

        [Required]
        public List<RecipeComment> Comments { get; set; } = new();

        [Required]
        public List<RecipeRating> RecipeRatings { get; set; } = new();

        public Recipe() { }

        public Recipe(string name, PrivacyLevel privacyLevel, string image, string description, int userId
            , List<RecipeIngredient> ingredients)
        {
            Name = name;
            PrivacyLevel = privacyLevel;
            Image = image;
            Description = description;
            CreatorId = userId;
            Ingredients = ingredients;
        }

        public double AverageRating()
        {
            double avgRating = 0;

            if (RecipeRatings.Count <= 0)
                return avgRating;
         
            foreach (var item in RecipeRatings)
            {
                avgRating += item.Rating;
            }
            return avgRating / RecipeRatings.Count;
        }

        public double GetCalories()
        {
            double totalCalories = 0;
            double totalWeight = 0;

            foreach (var item in Ingredients)
            {
                totalCalories += item.GetCalories();
                totalWeight += item.Weight;
            }

            if (totalWeight == 0)
                return 0;

            return totalCalories * 100 / totalWeight;
        }

        public double GetProteins()
        {
            double totalProteins = 0;
            double totalWeight = 0;

            foreach (var item in Ingredients)
            {
                totalProteins += item.GetProteins();
                totalWeight += item.Weight;
            }

            if (totalWeight == 0)
                return 0;

            return totalProteins * 100 / totalWeight;
        }

        public double GetCarbohydrates()
        {
            double totalCarbs = 0;
            double totalWeight = 0;

            foreach (var item in Ingredients)
            {
                totalCarbs += item.GetCarbohydrates();
                totalWeight += item.Weight;
            }

            if (totalWeight == 0)
                return 0;

            return totalCarbs * 100 / totalWeight;
        }

        public double GetFats()
        {
            double totalFats = 0;
            double totalWeight = 0;

            foreach (var item in Ingredients)
            {
                totalFats += item.GetFats();
                totalWeight += item.Weight;
            }

            if (totalWeight == 0)
                return 0;

            return totalFats * 100 / totalWeight;
        }

    }
}
