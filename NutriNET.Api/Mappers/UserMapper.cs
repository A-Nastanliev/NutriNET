using NutriNET.Api.Dto.User;
using NutriNET.Data.Models;

namespace NutriNET.Api.Mappers
{
    public static class UserMapper
    {
        public static UserDto ToDto(this User user, string baseUrl)
        {
            return new UserDto(user.ToPublicDto(baseUrl), user.EmailAddress, user.CreatedAt,
                user.Followers?.Select(f => f.FollowerId).ToList() ?? new List<int>(), user.Following?.Select(f => f.FollowingId).ToList() ?? new List<int>());
        }

        public static PublicUserDto ToPublicDto(this User user, string baseUrl)
        {
            return new PublicUserDto(user.Id, user.Username, user.ProfilePicture == null ? null : $"{baseUrl}/{user.ProfilePicture}", user.Role);
        }

        public static ModeratorRequestDto ToDto(this ModeratorRequest mr, string baseUrl)
        {
            var senderDto = mr.Sender?.ToPublicDto(baseUrl);
            var actionUserDto = mr.ActionedBy?.ToPublicDto(baseUrl);

            if (actionUserDto != null)
            {
                return new ModeratorRequestDto(mr.Id, mr.RequestDescription, mr.Status, mr.DateSent, senderDto, actionUserDto, mr.ActionedOn);
            }

            return new ModeratorRequestDto(mr.Id, mr.RequestDescription, mr.Status, mr.DateSent, senderDto);
        }

        public static CommentRestrictionDto ToDto(this CommentRestriction cr, string baseUrl)
        {
            return new CommentRestrictionDto(cr.Id, cr.Reason, cr.StartDate, cr.EndDate, cr.User.ToPublicDto(baseUrl));
        }
    }
}
