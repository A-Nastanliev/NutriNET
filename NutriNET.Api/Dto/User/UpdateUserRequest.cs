using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.User
{
    public class UpdateUserRequest
    {
        [Required]
        public string Username { get; set; }
    }
}
