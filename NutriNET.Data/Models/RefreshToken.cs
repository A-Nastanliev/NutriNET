namespace NutriNET.Data.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Token { get; set; }
        [Required]
        public DateTime ExpiresAt { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        public bool IsRevoked { get; set; }
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
