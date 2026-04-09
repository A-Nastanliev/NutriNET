using System.Data;
using System.Security.Cryptography;

namespace NutriNET.Services
{
    public class UserService
    {
        private readonly NutriDbContext _context;

        public UserService(NutriDbContext context)
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

        public async Task UpdatePasswordAsync(int userToUpdateId, string newPassword, string currentPassword)
        {    
            var user = await _context.Users.FindAsync(userToUpdateId);
            if (user == null)
                throw new KeyNotFoundException("UserNotFound");

            if (!VerifyPassword(currentPassword, user.PasswordHash))
                throw new InvalidOperationException("IncorrectPassword");

            user.PasswordHash = HashPassword(newPassword);

            await _context.SaveChangesAsync();
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

        public async Task UpdateProfilePictureAsync(User userToUpdate)
        {
            var user = await _context.Users.FindAsync(userToUpdate.Id);
            if (user == null)
                throw new KeyNotFoundException("UserNotFound");

            user.ProfilePicture = userToUpdate.ProfilePicture;
            await _context.SaveChangesAsync();
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

        public async Task DeleteAsync( int userToDeleteId, int actionUserId)
        {
            var userToDelete = await _context.Users.FindAsync(userToDeleteId);

            if (userToDelete == null)
                throw new KeyNotFoundException();

            if (userToDelete.Role == UserRole.Administrator)
                throw new InvalidOperationException($"CannotDeleteOtherAdmin");


            if (userToDeleteId != actionUserId)
            {
                var actionUser = await _context.Users.FindAsync(actionUserId);
                if (actionUser == null)
                {
                    throw new KeyNotFoundException();
                }
                if (actionUser.Role != UserRole.Administrator)
                {
                    throw new UnauthorizedAccessException("ForbiddenAction");
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var recipes = await _context.Recipes
                  .Where(r => r.CreatorId == userToDeleteId)
                  .ToListAsync();

                var recipeIds = recipes.Select(r => r.Id).ToList();

                var usedRecipeIds = await (
                     from mf in _context.MealFoods
                     join m in _context.Meals on mf.MealId equals m.Id
                     where mf.RecipeId != null &&
                           recipeIds.Contains(mf.RecipeId.Value) &&
                           m.UserId != userToDeleteId
                     select mf.RecipeId.Value)
                    .Distinct()
                    .ToListAsync();

                foreach (var recipe in recipes)
                {
                    if (usedRecipeIds.Contains(recipe.Id))
                    {
                        recipe.PrivacyLevel = PrivacyLevel.Archieved;

                        await _context.RecipeListItems
                            .Where(rl => rl.RecipeId == recipe.Id)
                            .ExecuteDeleteAsync();

                        await _context.RecipeComments
                            .Where(rc => rc.RecipeId == recipe.Id)
                            .ExecuteDeleteAsync();

                        await _context.RecipeRatings
                            .Where(rr => rr.RecipeId == recipe.Id)
                            .ExecuteDeleteAsync();
                    }
                    else
                    {
                        _context.Recipes.Remove(recipe);
                    }
                }
                _context.Users.Remove(userToDelete);
                await _context.SaveChangesAsync() ;
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task FollowAsync(int followerId, int followingId) 
        {
            var followingUser = await _context.Users.FindAsync(followingId);
            if (followingUser == null)
                throw new KeyNotFoundException("UserNotFound");

            _context.Followers.Add(new Follower { FollowerId = followerId, FollowingId = followingId, FollowDate = DateTime.UtcNow });   
            await _context.SaveChangesAsync();
         }

        public async Task UnfollowAsync(int followerId, int followingId)
        {
            var follower = await _context.Followers.FindAsync(followerId, followingId);
            if (follower == null)
                throw new KeyNotFoundException("NotFollowing");

            _context.Followers.Remove(follower);
            await _context.SaveChangesAsync();
        }

        public async Task<(int FollowersCount, int FollowingCount)> GetFollowStatsAsync(int userId)
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!exists)
                throw new KeyNotFoundException("UserNotFound");

            var followersCount = await _context.Followers.CountAsync(f => f.FollowingId == userId);
            var followingCount = await _context.Followers.CountAsync(f => f.FollowerId == userId);

            return (followersCount, followingCount);
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

        public async Task EndCommentRestrictionAsync(int id)
        {
            var restriction = await _context.CommentRestrictions.FindAsync(id);
            if(restriction == null)
                throw new KeyNotFoundException("CommentRestrictionNotFound");

            restriction.EndDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
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

        public async Task CreateModeratorRequestAsync(ModeratorRequest moderatorRequest)
        {
            moderatorRequest.DateSent = DateTime.UtcNow;
            moderatorRequest.Status = RequestStatus.Pending;
            moderatorRequest.ActionedById = null;
            moderatorRequest.ActionedBy = null;
            _context.ModeratorRequests.Add(moderatorRequest);
            await _context.SaveChangesAsync();
        }

        public async Task<ModeratorRequest> GetPendingModeratorRequestAsync(int userId)
        {
            return await _context.ModeratorRequests.FirstOrDefaultAsync(mr=>mr.SenderId == userId && mr.Status == RequestStatus.Pending);
        }

        public async Task UpdateModeratorRequestAsync(int requestId, RequestStatus newStatus, int adminId)
        {
            var admin = await _context.Users.FindAsync(adminId);
            if (admin == null || admin?.Role != UserRole.Administrator) 
            {
                throw new UnauthorizedAccessException("ForbiddenAction");
            }

            var moderatorRequest = await _context.ModeratorRequests.FindAsync(requestId);
            if(moderatorRequest == null)
                throw new KeyNotFoundException("ModeratorRequestNotFound");

            moderatorRequest.Status = newStatus;
            moderatorRequest.ActionedOn = DateTime.UtcNow;
            moderatorRequest.ActionedById = adminId;
            if(moderatorRequest.Status == RequestStatus.Accepted)
            {
                var user = await _context.Users.FindAsync(moderatorRequest.SenderId);
                if (user == null)
                    throw new KeyNotFoundException("UserNotFound");

                user.Role = UserRole.Moderator;
                var restriction = await _context.CommentRestrictions.
                  FirstOrDefaultAsync(cr => cr.UserId == user.Id && (cr.EndDate == null || cr.EndDate > DateTime.UtcNow));
                if (restriction != null)
                {
                    restriction.EndDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
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

        public async Task<string> CreatePasswordResetCodeAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
            if (user == null)
            {
                await Task.Delay(Random.Shared.Next(100, 300));
                return null;
            }

            var existing = _context.PasswordResetTokens.Where(t => t.UserId == user.Id);
            _context.PasswordResetTokens.RemoveRange(existing);

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Code = HashPassword(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });

            await _context.SaveChangesAsync();
            return code;
        }

        public async Task ResetPasswordAsync(string email, string code, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
            if (user == null)
                throw new KeyNotFoundException("UserNotFound");

            var token = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.ExpiresAt > DateTime.UtcNow);

            if (token == null)
                throw new InvalidOperationException("InvalidOrExpiredCode");

            if (!VerifyPassword(code, token.Code))
                throw new InvalidOperationException("InvalidOrExpiredCode");

            _context.PasswordResetTokens.Remove(token);
            user.PasswordHash = HashPassword(newPassword);

            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
        {
            var token = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(int userId, int expireDays = 30)
        {
            var token = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(expireDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<RefreshToken?> GetActiveRefreshTokenAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task RevokeRefreshTokenAsync(RefreshToken token)
        {
            token.IsRevoked = true;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllUserRefreshTokensAsync(int userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();
        }
    }
}
