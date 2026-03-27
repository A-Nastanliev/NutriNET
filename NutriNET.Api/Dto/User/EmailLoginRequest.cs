using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.User
{
    public class EmailLoginRequest
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
