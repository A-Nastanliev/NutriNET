namespace NutriNET.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private AppDbContext _context;
        private UserService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            _context = new AppDbContext(options);
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
        public async Task UpdatePasswordAsync_ShouldReturnTrue_WhenCorrectPassword()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var dbUser = await _context.Users.FirstAsync();

            var result = await _service.UpdatePasswordAsync(dbUser.Id, "new", "1234");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task UpdatePasswordAsync_ShouldReturnFalse_WhenWrongPassword()
        {
            var user = CreateUser();
            await _service.SignUpAsync(user);

            var dbUser = await _context.Users.FirstAsync();

            var result = await _service.UpdatePasswordAsync(dbUser.Id, "new", "wrong");

            Assert.That(result, Is.False);
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

            var result = await _service.DeleteAsync(user.Id, 1);

            Assert.That(result, Is.True);
            Assert.That(_context.Users.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteAsync_ShouldReturnFalse_WhenMissing()
        {
            var result = await _service.DeleteAsync(999, 1);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task FollowAsync_ShouldCreateFollower()
        {
            var u1 = CreateUser("u1@test.com", "u1");
            var u2 = CreateUser("u2@test.com", "u2");

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var result = await _service.FollowAsync(u1.Id, u2.Id);

            Assert.That(result, Is.True);
            Assert.That(_context.Followers.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task UnfollowAsync_ShouldRemoveFollower()
        {
            _context.Followers.Add(new Follower { FollowerId = 1, FollowingId = 2 });
            await _context.SaveChangesAsync();

            var result = await _service.UnfollowAsync(1, 2);

            Assert.That(result, Is.True);
            Assert.That(_context.Followers.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task UpdateProfilePictureAsync_ShouldUpdate()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.ProfilePicture = "pic.png";

            var result = await _service.UpdateProfilePictureAsync(user);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CreateModeratorRequestAsync_ShouldCreate()
        {
            var request = new ModeratorRequest
            {
                SenderId = 1,
                RequestDescription = "test"
            };

            var result = await _service.CreateModeratorRequestAsync(request);

            Assert.That(result, Is.True);
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

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }
    }
}
