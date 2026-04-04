using NutriNET.Data.Enums;

namespace NutriNET.Api.Dto.Recipe
{
    public class RecipeFormDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public PrivacyLevel PrivacyLevel { get; set; }

        public string Description { get; set; }

        public IFormFile Image {  get; set; }

        public List<RecipeIngredientFormDto> Ingredients { get; set; } = new();
    }
}
