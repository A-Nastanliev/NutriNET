namespace NutriNET.Data.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public DateTime ExpiresAt { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
