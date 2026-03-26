namespace NutriNET.Data.Models
{
    [Index(nameof(EmailAddress), IsUnique = true)]
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public UserRole Role { get; set; }

        [Required]
        [Length(4, 40)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        public string PasswordHash { get; set; }

       
        public string? ProfilePicture { get; set; }

        [Required]
        public List<CommentRestriction> CommentRestrictions { get; set; }

        [NotMapped]
        public CommentRestriction CurrentRestriction
        {
            get
            {
                return CommentRestrictions.FirstOrDefault(cr => cr.EndDate > DateTime.UtcNow || cr.EndDate is null);
            }
        }

        [Required]
        public List<Meal> Meals { get; set; } = new();

        [Required]
        public List<Recipe> CreatedRecipes { get; set; } = new();

        [Required]
        public List<RecipeList> RecipeLists { get; set; } = new();

        [Required]
        public List<RecipeRating> RecipeRatings { get; set; } = new();

        [Required]
        public List<RecipeComment> RecipeComments { get; set; } = new();

        [Required]
        public List<Follower> Followers { get; set; } = new();

        [Required]
        public List<Follower> Following { get; set; } = new();

        private List<User> friends;

        [NotMapped]
        public List<User> Friends
        {
            get
            {
                if (friends == null)
                {
                    var followerUsers = Followers.Select(f => f.FollowerUser);
                    var followingUsers = Following.Select(f => f.FollowingUser);

                    friends = followerUsers
                        .Where(fu => followingUsers.Any(fu2 => fu2.Id == fu.Id))
                        .ToList();
                }
                return friends;
            }
            private set => friends = value;
        }

        private List<int> friendsIds;

        [NotMapped]
        public List<int> FriendsIds
        {
            get
            {
                if (friendsIds == null)
                {
                    var followerIds = Followers.Select(f => f.FollowerId);
                    var followingIds = Following.Select(f => f.FollowingId);

                    friendsIds = followerIds.Intersect(followingIds).ToList();
                }
                return friendsIds;
            }
            private set => friendsIds = value;
        }

        [Required]
        public List<ModeratorRequest> SentModeratorRequests { get; set; } = new();
        [Required]
        public List<ModeratorRequest> ActionedModeratorRequests { get; set; } = new();

        [Required]
        public List<FoodRequest> SentFoodRequests { get; set; } = new ();

        [Required]
        public List<FoodRequest> ActionedFoodRequests { get; set; } = new();

        [NotMapped]
        public List<FoodRequest> PendingFoodRequests => SentFoodRequests?.Where(fr=>fr.Status == RequestStatus.Pending).ToList();

        [NotMapped]
        public ModeratorRequest PendingModeratorRequest => SentModeratorRequests?.FirstOrDefault(mr => mr.Status == RequestStatus.Pending);

        [NotMapped]
        public DateTime? _FollowerDate { get; set; }

        [NotMapped]
        public int? _FollowerId { get; set; }

        [NotMapped]
        public DateTime? _FollowingDate { get; set; }

        [NotMapped]
        public int? _FollowingId { get; set; }
    }
}
