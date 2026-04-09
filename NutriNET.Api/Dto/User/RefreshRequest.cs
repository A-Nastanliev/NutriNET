using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.User
{
    public class RefreshRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
