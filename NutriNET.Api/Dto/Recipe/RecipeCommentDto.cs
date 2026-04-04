using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NutriNET.Api.Dto.User;

namespace NutriNET.Api.Dto.Recipe
{
    public class RecipeCommentDto
    {
        public int Id { get; set; }

        public PublicUserDto User { get; set; }

        [Length(4, 500)]
        [Required]
        public string Comment { get; set; }

        public DateTime Date { get; set; }

        public RecipeCommentDto() { }

        public RecipeCommentDto(int id, string comment, PublicUserDto user, DateTime date)
        {
            Id = id;
            Comment = comment;
            User = user;
            Date = date;
        }
    }
}
