using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.User
{
    public class TokenRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
