using NutriNET.Api.Dto.Food;

namespace NutriNET.Api.Dto.Recipe
{
    public class RecipeIngredientDto
    {
        public int Id { get; set; }
        public double Weight { get; set; }

        public FoodDto Food { get; set; }

        public RecipeIngredientDto() { }

        public RecipeIngredientDto(int id, double weight, FoodDto food) 
        {
            Id = id;
            Weight = weight;
            Food = food;
        }
    }
}
