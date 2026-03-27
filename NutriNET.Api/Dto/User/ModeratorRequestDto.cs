using NutriNET.Data.Enums;
using System.Data;

namespace NutriNET.Api.Dto.User
{
    public class ModeratorRequestDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public RequestStatus Status { get; set; }
        public PublicUserDto PublicUser { get; set; }
        public PublicUserDto? ActionUser { get; set; }
        public DateTime DateSent { get; set; }
        public DateTime? ActionDate {  get; set; }

        public ModeratorRequestDto() { }

        public ModeratorRequestDto(int id, string description, RequestStatus status, DateTime dateSent, PublicUserDto publicUser)
        {      
            PublicUser = publicUser;
            Description = description;
            Id = id;
            Status = status;
            DateSent = dateSent;
        }

        public ModeratorRequestDto(int id, string description, RequestStatus status, DateTime dateSent, PublicUserDto publicUser, PublicUserDto actionUser, DateTime? actionDate)
            : this(id, description, status, dateSent, publicUser)
        {
            ActionUser = actionUser;
            ActionDate = actionDate;
        }
    }
}
