using NutriNET.Data.Enums;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace NutriNET.Api.Dto.User
{
    public class CommentRestrictionDto
    {
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
        public PublicUserDto PublicUser { get; set; }

        public CommentRestrictionDto() { }

        public CommentRestrictionDto(int id, string reason, DateTime startDate, DateTime? endDate, PublicUserDto user)
        {
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            Reason = reason;
            PublicUser = user;
        }
    }
}
