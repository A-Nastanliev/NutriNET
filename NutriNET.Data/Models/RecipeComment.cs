namespace NutriNET.Data.Models
{
    public class RecipeComment
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public int RecipeId { get; set; }
        [ForeignKey(nameof(RecipeId))]
        public Recipe Recipe { get; set; }

        [Length(4, 500)]
        [Required]
        public string Comment { get; set; }

        public DateTime Date {  get; set; }

        public RecipeComment() { }

        public RecipeComment(int userId, int recipeId, string comment) 
        {
            UserId = userId;
            RecipeId = recipeId;
            Comment = comment;
        }

    }
}
