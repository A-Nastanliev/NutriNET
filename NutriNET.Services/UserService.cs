using System.Data;
using System.Security.Cryptography;

namespace NutriNET.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context; 
        }

        private async Task<User> IncludeUserRelations(int userId)
        {
            return await _context.Users
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User> EmailPasswordLoginAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;

            return await IncludeUserRelations(user.Id);
        }

        public async Task SignUpAsync(User user)
        {
            user.PasswordHash = HashPassword(user.PasswordHash);
            user.Role = UserRole.User;
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<UserRole> GetRole(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException();

            return user.Role;
        }

        public async Task UpdateRoleAsync(int userToUpdateId, UserRole userRole, int adminId)
        {
            if (userRole == UserRole.Administrator)
                throw new InvalidOperationException();

            var admin = await _context.Users.FindAsync(adminId);
            if (admin == null)
                throw new KeyNotFoundException();

            if (admin.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException();

            var user = await _context.Users.FindAsync(userToUpdateId);
            if (user == null)
                throw new InvalidOperationException();

            user.Role = userRole;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdatePasswordAsync(int userToUpdateId, string newPassword, string currentPassword)
        {    
            var user = await _context.Users.FindAsync(userToUpdateId);
            if (user == null)
                throw new InvalidOperationException();

            if (!VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = HashPassword(newPassword);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task UpdateAsync(User userToUpdate)
        {
            var user = await _context.Users.FindAsync(userToUpdate.Id);
            if (user == null)
                throw new KeyNotFoundException();

            user.Username = userToUpdate.Username;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateEmailAsync( int userId ,string newEmail, string currentPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException();

            if (!VerifyPassword(currentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException();

            user.EmailAddress = newEmail;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateProfilePictureAsync(User userToUpdate)
        {
            var user = await _context.Users.FindAsync(userToUpdate.Id);
            if (user == null)
                return false;

            user.ProfilePicture = userToUpdate.ProfilePicture;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await IncludeUserRelations(id);
        } 

        public async Task<(List<User>, DateTime? CursorDate)> GetNextUsersAsync(int count, DateTime? lastCreatedAt, UserRole role, int? lastUserId)
        {
            var query = _context.Users.Where(u => u.Role == role);

            if (lastCreatedAt != null && lastUserId != null)
            {
                query = query.Where(u =>
                    u.CreatedAt < lastCreatedAt ||
                    (u.CreatedAt == lastCreatedAt && u.Id < lastUserId));
            }

           var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Take(count)
                .ToListAsync();

            return (users, users.LastOrDefault()?.CreatedAt);
        }

        public async Task<bool> DeleteAsync( int userToDeleteId, int actionUser)
        {
            var userToDelete = await _context.Users.FindAsync(userToDeleteId);

            if (userToDelete == null)
                return false;

            if (userToDelete.Role == UserRole.Administrator)
                throw new InvalidOperationException($"CannotDeleteOtherAdmin");

            try
            {
                _context.Users.Remove(userToDelete);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public async Task<bool> FollowAsync(int followerId, int followingId) 
        {
            _context.Followers.Add(new Follower { FollowerId = followerId, FollowingId = followingId, FollowDate = DateTime.UtcNow });   
            return await _context.SaveChangesAsync() > 0;
         }

        public async Task<bool> UnfollowAsync(int followerId, int followingId)
        {
            var follower = await _context.Followers.FindAsync(followerId, followingId);
            if (follower == null)
                return false;

            _context.Followers.Remove(follower);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<User>> GetNextFollowersAsync(int  userId, int count, DateTime? lastFollowDate, int? lastFollowerId)
        {
            var query = _context.Followers.Where(f => f.FollowingId == userId);

            if (lastFollowDate != null && lastFollowerId != null)
                query = query.Where(f => f.FollowDate < lastFollowDate || (f.FollowDate == lastFollowDate && f.FollowerId < lastFollowerId));

            var list = await query
                .OrderByDescending(f => f.FollowDate)
                .ThenByDescending(f => f.FollowerId)
                .Take(count)
                    .Include(f=>f.FollowerUser)
                .Select(f => new { f.FollowerUser, f.FollowDate, f.FollowerId })
                .ToListAsync();

            foreach (var item in list)
            {
                item.FollowerUser._FollowerDate = item.FollowDate;
                item.FollowerUser._FollowerId = item.FollowerId;
            }

            return list.Select(x => x.FollowerUser).ToList();
        }

        public async Task<List<User>> GetNextFollowingAsync(int userId, int count, DateTime? lastFollowDate, int? lastFollowingId)
        {
            var query = _context.Followers
                .Where(f => f.FollowerId == userId);

            if (lastFollowDate != null && lastFollowingId != null)
            {
                query = query.Where(f =>
                    f.FollowDate < lastFollowDate ||
                    (f.FollowDate == lastFollowDate && f.FollowingId < lastFollowingId));
            }

            var list =  await query
                .OrderByDescending(f => f.FollowDate)
                .ThenByDescending(f => f.FollowingId)
                .Take(count)
                    .Include(f=>f.FollowingUser)
                .Select(f => new { f.FollowingUser, f.FollowDate, f.FollowingId })
                .ToListAsync();

            foreach(var item in list)
            {
                item.FollowingUser._FollowingDate = item.FollowDate;
                item.FollowingUser._FollowingId = item.FollowingId;
            }

            return list.Select(x => x.FollowingUser).ToList();
        }

        public async Task CreateCommentRestrictionAsync(CommentRestriction restriction, int moderatorId)
        {
            var currentRestriction = await _context.CommentRestrictions
                .FirstOrDefaultAsync(cr=>cr.UserId == restriction.UserId && (cr.EndDate == null || cr.EndDate > DateTime.UtcNow));

            var moderator = await _context.Users.FindAsync(moderatorId);

            if (moderator.Role == UserRole.User)
                throw new UnauthorizedAccessException("ForbiddenAction");

            if (moderator.Role == UserRole.Moderator && restriction.EndDate is null)
                throw  new InvalidOperationException( "OnlyTemporaryRestrictions");

            if (currentRestriction != null)
            {
                throw new InvalidOperationException("AlreadyHasRestriction");
            }

            restriction.StartDate = DateTime.UtcNow;
            _context.CommentRestrictions.Add(restriction);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EndCommentRestrictionAsync(int id)
        {
            var restriction = await _context.CommentRestrictions.FindAsync(id);
            if(restriction == null)
                return false;

            restriction.EndDate = DateTime.UtcNow;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<CommentRestriction> GetActiveCommentRestrictionAsync(int userId)
        {
            return await _context.CommentRestrictions.FirstOrDefaultAsync(cr=>cr.UserId == userId && (cr.EndDate > DateTime.UtcNow || cr.EndDate == null) );
        }

        public async Task<List<CommentRestriction>> GetLatestRestrictionsAsync(int count, DateTime? lastStartDate, int? lastRestrictionId, RestrictionStatus status)
        {
            var now = DateTime.UtcNow;
            IQueryable<CommentRestriction> query = _context.CommentRestrictions;

            query = status switch
            {
                RestrictionStatus.ActiveTemporary =>
                    query.Where(cr => cr.EndDate > now),

                RestrictionStatus.ActiveIndefinite =>
                    query.Where(cr => cr.EndDate == null),

                RestrictionStatus.Inactive =>
                    query.Where(cr => cr.EndDate <= now),

                _ => query
            };

            if (lastStartDate != null && lastRestrictionId != null)
            {
                query = query.Where(cr =>
                    cr.StartDate < lastStartDate ||
                    (cr.StartDate == lastStartDate && cr.Id < lastRestrictionId));
            }

            return await query
                .Include(cr => cr.User)
                .OrderByDescending(cr => cr.StartDate)
                .ThenByDescending(cr => cr.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> CreateModeratorRequestAsync(ModeratorRequest moderatorRequest)
        {
            moderatorRequest.DateSent = DateTime.UtcNow;
            moderatorRequest.Status = RequestStatus.Pending;
            moderatorRequest.ActionedById = null;
            moderatorRequest.ActionedBy = null;
            _context.ModeratorRequests.Add(moderatorRequest);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<ModeratorRequest> GetPendingModeratorRequestAsync(int userId)
        {
            return await _context.ModeratorRequests.FirstOrDefaultAsync(mr=>mr.SenderId == userId && mr.Status == RequestStatus.Pending);
        }

        public async Task<bool> UpdateModeratorRequestAsync(int requestId, RequestStatus newStatus, int adminId)
        {
            var admin = await _context.Users.FindAsync(adminId);
            if (admin == null || admin?.Role != UserRole.Administrator) 
            {
                throw new UnauthorizedAccessException("ForbiddenAction");
            }

            var moderatorRequest = await _context.ModeratorRequests.FindAsync(requestId);
            if(moderatorRequest == null)
                return false;

            moderatorRequest.Status = newStatus;
            moderatorRequest.ActionedOn = DateTime.UtcNow;
            moderatorRequest.ActionedById = adminId;
            if(moderatorRequest.Status == RequestStatus.Accepted)
            {
                var user = await _context.Users.FindAsync(moderatorRequest.SenderId);
                if (user == null)
                    return false;

                user.Role = UserRole.Moderator;
            }

           return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<ModeratorRequest>> GetNextModeratorRequestsAsync(int count, DateTime? lastDateSent, int? lastRequestId, RequestStatus status)
        {
            var query = _context.ModeratorRequests
                .Where(mr => mr.Status == status);

            if (lastDateSent != null && lastRequestId != null)
            {
                query = query.Where(mr =>
                    mr.DateSent < lastDateSent ||
                    (mr.DateSent == lastDateSent && mr.Id < lastRequestId));
            }

            return await query
                .OrderByDescending(mr => mr.DateSent)
                .ThenByDescending(mr => mr.Id)
                .Include(mr => mr.Sender)
                .Include(mr => mr.ActionedBy)
                .Take(count)
                .ToListAsync();
        }

        private string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32);

            return $"{Convert.ToHexString(salt)}:{Convert.ToHexString(hash)}";
        }

        private bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split(':');
            byte[] salt = Convert.FromHexString(parts[0]);
            byte[] storedHash = Convert.FromHexString(parts[1]);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32);

            return CryptographicOperations.FixedTimeEquals(hash, storedHash);
        }


    }
}
