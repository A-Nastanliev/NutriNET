namespace NutriNET.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private NutriDbContext _context;
        private UserService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<NutriDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;

            _context = new NutriDbContext(options);
            _service = new UserService(_context);
        }

        private User CreateUser(string email = "test@test.com", string username = "test", string password = "1234", UserRole role = UserRole.User)
        {
            return new User
            {
                Username = username,
                EmailAddress = email,
                PasswordHash = password,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                CommentRestrictions = new(),
            };
        }

        [Test]
        public async Task SignUpAsync_ShouldCreateUser()
        {
            var user = CreateUser();

            await _service.SignUpAsync(user);

            var dbUser = await _context.Users.FirstOrDefaultAsync();

            Assert.That(dbUser, Is.Not.Null);
            Assert.That(dbUser.PasswordHash, Is.Not.EqualTo("1234"));
            Assert.That(dbUser.Role, Is.EqualTo(UserRole.User));
        }

        [Test]
        public async Task EmailPasswordLoginAsync_ShouldReturnUser_WhenValid()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var result = await _service.EmailPasswordLoginAsync(user.EmailAddress, "1234");

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task EmailPasswordLoginAsync_ShouldReturnNull_WhenWrongPassword()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var result = await _service.EmailPasswordLoginAsync(user.EmailAddress, "wrong");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetRole_ShouldReturnRole()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var role = await _service.GetRole(user.Id);

            Assert.That(role, Is.EqualTo(UserRole.User));
        }

        [Test]
        public void GetRole_ShouldThrow_WhenUserMissing()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetRole(999));
        }

        [Test]
        public void UpdateRoleAsync_ShouldThrow_WhenNotAdmin()
        {
            var user = CreateUser("u@test.com", "u");
            var target = CreateUser("t@test.com", "t");

            _context.Users.AddRange(user, target);
            _context.SaveChanges();

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateRoleAsync(target.Id, UserRole.Moderator, user.Id));
        }

        [Test]
        public async Task UpdatePasswordAsync_ShouldUpdatePassword_WhenCorrectPassword()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var dbUser = await _context.Users.FirstAsync();
            var oldHash = dbUser.PasswordHash;

            await _service.UpdatePasswordAsync(dbUser.Id, "new", "1234");

            var updatedUser = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == dbUser.Id);

            Assert.That(updatedUser.PasswordHash, Is.Not.EqualTo(oldHash));
        }

        [Test]
        public async Task UpdatePasswordAsync_ShouldThrow_WhenWrongPassword()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var dbUser = await _context.Users.FirstAsync();

            Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdatePasswordAsync(dbUser.Id, "new", "wrong"));
        }

        [Test]
        public async Task UpdateAsync_ShouldUpdateUsername()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.Username = "updated";

            await _service.UpdateAsync(user);

            var dbUser = await _context.Users.FindAsync(user.Id);

            Assert.That(dbUser.Username, Is.EqualTo("updated"));
        }

        [Test]
        public void UpdateAsync_ShouldThrow_WhenMissing()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(new User { Id = 999 }));
        }

        [Test]
        public async Task UpdateEmailAsync_ShouldUpdate_WhenPasswordCorrect()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var dbUser = await _context.Users.FirstAsync();

            await _service.UpdateEmailAsync(dbUser.Id, "new@test.com", "1234");

            Assert.That(dbUser.EmailAddress, Is.EqualTo("new@test.com"));
        }

        [Test]
        public async Task UpdateEmailAsync_ShouldThrow_WhenWrongPassword()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _service.UpdateEmailAsync(user.Id, "new@test.com", "wrong"));
        }


        [Test]
        public async Task DeleteAsync_ShouldDeleteUser()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await _service.DeleteAsync(user.Id, 1);

            Assert.That(_context.Users.Count(), Is.EqualTo(0));
        }

        [Test]
        public void DeleteAsync_ShouldThrow_WhenMissing()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _service.DeleteAsync(999, 1));
        }

        [Test]
        public async Task DeleteAsync_ShouldThrow_WhenAdmin()
        {
            var admin = CreateUser();
            admin.Role = UserRole.Administrator;

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.DeleteAsync(admin.Id, 1));
        }

        [Test]
        public async Task FollowAsync_ShouldCreateFollower_WhenUsersExist()
        {
            var u1 = CreateUser("u1@test.com", "u1");
            var u2 = CreateUser("u2@test.com", "u2");

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            await _service.FollowAsync(u1.Id, u2.Id);

            Assert.That(_context.Followers.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task UnfollowAsync_ShouldRemoveFollower_WhenExists()
        {
            _context.Followers.Add(new Follower { FollowerId = 1, FollowingId = 2 });
            await _context.SaveChangesAsync();

            await _service.UnfollowAsync(1, 2);

            Assert.That(_context.Followers.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetFollowStatsAsync_ShouldReturnZeros_WhenNoFollowers()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var (followersCount, followingCount) = await _service.GetFollowStatsAsync(user.Id);

            Assert.That(followersCount, Is.EqualTo(0));
            Assert.That(followingCount, Is.EqualTo(0));
        }

        [Test]
        public async Task GetFollowStatsAsync_ShouldReturnCorrectCounts()
        {
            var u1 = CreateUser("u1@test.com", "u1");
            var u2 = CreateUser("u2@test.com", "u2");
            var u3 = CreateUser("u3@test.com", "u3");

            _context.Users.AddRange(u1, u2, u3);
            await _context.SaveChangesAsync();

            _context.Followers.AddRange(
                new Follower { FollowerId = u2.Id, FollowingId = u1.Id, FollowDate = DateTime.UtcNow },
                new Follower { FollowerId = u3.Id, FollowingId = u1.Id, FollowDate = DateTime.UtcNow },
                new Follower { FollowerId = u1.Id, FollowingId = u2.Id, FollowDate = DateTime.UtcNow }
            );
            await _context.SaveChangesAsync();

            var (followersCount, followingCount) = await _service.GetFollowStatsAsync(u1.Id);

            Assert.That(followersCount, Is.EqualTo(2));
            Assert.That(followingCount, Is.EqualTo(1));
        }

        [Test]
        public void GetFollowStatsAsync_ShouldThrow_WhenUserNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetFollowStatsAsync(999));
        }

        [Test]
        public async Task UpdateProfilePictureAsync_ShouldUpdateProfilePicture()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.ProfilePicture = "pic.png";

            await _service.UpdateProfilePictureAsync(user);

            var dbUser = await _context.Users.FindAsync(user.Id);

            Assert.That(dbUser.ProfilePicture, Is.EqualTo("pic.png"));
        }

        [Test]
        public async Task CreateModeratorRequestAsync_ShouldCreateRequest()
        {
            var request = new ModeratorRequest
            {
                SenderId = 1,
                RequestDescription = "test"
            };

            await _service.CreateModeratorRequestAsync(request);

            Assert.That(_context.ModeratorRequests.Count(), Is.EqualTo(1));
        }

        [Test]
        public void UpdateModeratorRequestAsync_ShouldThrow_WhenNotAdmin()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            _context.SaveChanges();

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateModeratorRequestAsync(1, RequestStatus.Accepted, user.Id));
        }

        [Test]
        public async Task CreatePasswordResetCodeAsync_ShouldReturnCode_AndSaveToDb()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var code = await _service.CreatePasswordResetCodeAsync(user.EmailAddress);

            Assert.That(code, Is.Not.Null);
            Assert.That(code, Has.Length.EqualTo(6));
            Assert.That(await _context.PasswordResetTokens.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task CreatePasswordResetCodeAsync_UnknownEmail_ReturnsNull()
        {
            var code = await _service.CreatePasswordResetCodeAsync("ghost@test.com");

            Assert.That(code, Is.Null);
            Assert.That(await _context.PasswordResetTokens.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task CreatePasswordResetCodeAsync_ShouldReplaceOldToken()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            await _service.CreatePasswordResetCodeAsync(user.EmailAddress);
            await _service.CreatePasswordResetCodeAsync(user.EmailAddress);

            Assert.That(await _context.PasswordResetTokens.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task ResetPasswordAsync_ValidCode_UpdatesPasswordAndDeletesToken()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);
            var code = await _service.CreatePasswordResetCodeAsync(user.EmailAddress);

            await _service.ResetPasswordAsync(user.EmailAddress, code, "NewPassword123!");

            Assert.That(await _context.PasswordResetTokens.CountAsync(), Is.EqualTo(0));
            var result = await _service.EmailPasswordLoginAsync(user.EmailAddress, "NewPassword123!");
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task ResetPasswordAsync_WrongCode_Throws()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);
            await _service.CreatePasswordResetCodeAsync(user.EmailAddress);

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ResetPasswordAsync(user.EmailAddress, "000000", "NewPassword123!"));
        }

        [Test]
        public async Task ResetPasswordAsync_ExpiredToken_Throws()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Code = "doesntmatter",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });
            await _context.SaveChangesAsync();

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ResetPasswordAsync(user.EmailAddress, "123456", "NewPassword123!"));
        }

        [Test]
        public async Task ResetPasswordAsync_UnknownEmail_Throws()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ResetPasswordAsync("ghost@test.com", "123456", "NewPassword123!"));
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }
    }
}
