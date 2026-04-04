using NutriNET.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NutriNET.Api.Dto.Food;
using NutriNET.Api.Dto.User;

namespace NutriNET.Api.Dto.Recipe
{
    public class RecipeDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public PrivacyLevel PrivacyLevel { get; set; }

        public DateTime Date { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public PublicUserDto Creator { get; set; }

        public List<RecipeIngredientDto> Ingredients { get; set; }


        public RecipeDto() { }

        public RecipeDto(int id, string name,PrivacyLevel privacy, DateTime date, string description, string image, 
            PublicUserDto creator, List<RecipeIngredientDto> ingredients)
        {
            Id = id;
            Name = name;
            PrivacyLevel = privacy;
            Date = date;
            Description = description;
            Image = image;
            Creator = creator;
            Ingredients = ingredients;
        }

    }
}
