using NutriNET.Data.Enums;
using NutriNET.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace NutriNET.Api.Dto.User
{
    public class UserDto
    {
        public PublicUserDto PublicUser { get; set; }

        [EmailAddress]
        public string EmailAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        [Required]
        public List<int> FollowerIds { get; set; } = new();

        [Required]
        public List<int> FollowingIds { get; set; } = new();

        public UserDto() { }

        public UserDto(PublicUserDto publicUser, string emailAddress, DateTime createdAt, List<int> followerIds, List<int> followingIds)
        {
            PublicUser= publicUser;
            CreatedAt = createdAt;
            EmailAddress= emailAddress;
            FollowerIds = followerIds;
            FollowingIds = followingIds;
        }
    }
}
