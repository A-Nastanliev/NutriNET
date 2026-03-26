namespace NutriNET.Data.Models
{
    [PrimaryKey(nameof(UserId), nameof(RecipeId))]
    public class RecipeRating
    {
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public int RecipeId { get; set; }
        [ForeignKey(nameof(RecipeId))]
        public Recipe Recipe { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public RecipeRating() { }

        public RecipeRating(int userId, int recipeId)
        {
            UserId = userId;
            RecipeId = recipeId;
        }

        public RecipeRating(int userId, int recipeId, int rating) : this(userId, recipeId)
        {
            Rating = rating;
        }
    }
}
