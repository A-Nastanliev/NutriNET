namespace NutriNET.Data.Models
{
    public class ModeratorRequest
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; }

        [Required]
        [Length(10, 500)]
        public string RequestDescription { get; set; }

        public RequestStatus Status { get; set; }
        public DateTime DateSent { get; set; }
        public DateTime? ActionedOn { get; set; }

        public int? ActionedById { get; set; }
        [ForeignKey(nameof(ActionedById))]
        public User ActionedBy { get; set; }
    }
}
