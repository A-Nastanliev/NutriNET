namespace NutriNET.Api.Dto.Recipe
{
    public class CreateRecipeIngredientDto
    {
        public int FoodId { get; set; }
        public double Weight { get; set; }

        public CreateRecipeIngredientDto() { }
    }
}
