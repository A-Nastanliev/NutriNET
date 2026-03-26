namespace NutriNET.Data.Models
{
    [PrimaryKey(nameof(FollowerId), nameof(FollowingId))]
    public class Follower
    {
        public int FollowerId { get; set; }
        [ForeignKey(nameof(FollowerId))]
        public User FollowerUser { get; set; }

        public int FollowingId { get; set; }
        [ForeignKey(nameof(FollowingId))]
        public User FollowingUser { get; set; }

        public DateTime FollowDate { get; set; }
    }
}
