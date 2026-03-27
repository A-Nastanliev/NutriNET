using NutriNET.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.User
{
    public class PublicUserDto
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string ProfilePicture { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public PublicUserDto(int id, string username, string profilePicture, UserRole role)
        {
            Id = id;
            Username = username;
            ProfilePicture = profilePicture;
            Role = role;
        }

        public PublicUserDto() { }
    }
}
