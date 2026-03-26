namespace NutriNET.Data.Models
{
    public class CommentRestriction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
