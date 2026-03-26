namespace NutriNET.Data.Models
{
    public class RecipeList
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Length(1, 20)]
        public string Name { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Required]
        public List<RecipeListItem> RecipeListItems { get; set; } = new();

        public RecipeList() { }

        public RecipeList(int userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        public RecipeList(int userId, string name, int id)
        {
            UserId = userId;
            Name = name;
            Id = id;
        }
    }
}
