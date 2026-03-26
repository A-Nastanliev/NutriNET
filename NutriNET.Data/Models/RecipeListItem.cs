namespace NutriNET.Data.Models
{
    [PrimaryKey(nameof(RecipeListId), nameof(RecipeId))]
    public class RecipeListItem
    {
        public int RecipeListId { get; set; }
        [ForeignKey(nameof(RecipeListId))]
        public RecipeList RecipeList { get; set; }

        public int? RecipeId {get; set; }
        [ForeignKey(nameof(RecipeId))]
        public Recipe Recipe { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
