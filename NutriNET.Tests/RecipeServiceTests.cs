namespace NutriNET.Tests
{
    [TestFixture]
    public class RecipeServiceTests
    {
        private AppDbContext _context;
        private RecipeService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            _context = new AppDbContext(options);
            _service = new RecipeService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        private User CreateUser(string username = "user")
            => new User
            {
                Username = username,
                EmailAddress = $"{username}@test.com",
                PasswordHash = "1234",
                CreatedAt = DateTime.UtcNow
            };

        private Food CreateFood(string name = "Chicken")
            => new Food
            {
                Name = name,
                Calories = 100,
                Proteins = 10,
                Carbohydrates = 5,
                Fats = 2
            };

        private Recipe CreateRecipe(int userId, PrivacyLevel privacy = PrivacyLevel.Public)
            => new Recipe
            {
                Name = "Recipe",
                Description = "Desc",
                PrivacyLevel = privacy,
                CreatorId = userId,
                Date = DateTime.UtcNow,
                Ingredients = new List<RecipeIngredient>()
            };

        [Test]
        public async Task CreateRecipeAsync_ShouldCreate()
        {
            var user = CreateUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var recipe = CreateRecipe(user.Id);

            await _service.CreateRecipeAsync(recipe);

            Assert.That(_context.Recipes.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetRecipeAsync_ShouldReturnNull_WhenNotFound()
        {
            var result = await _service.GetRecipeAsync(999);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetRecipeAsync_ShouldReturnNull_WhenArchived()
        {
            var recipe = CreateRecipe(1, PrivacyLevel.Archieved);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            var result = await _service.GetRecipeAsync(recipe.Id);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetRecipeDetailsAsync_ShouldIncludeRelations()
        {
            var user = CreateUser();
            var food = CreateFood();

            _context.Users.Add(user);
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            var recipe = CreateRecipe(user.Id);
            recipe.Ingredients.Add(new RecipeIngredient { FoodId = food.Id });

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            var result = await _service.GetRecipeDetailsAsync(recipe.Id);

            Assert.That(result.Creator, Is.Not.Null);
            Assert.That(result.Ingredients.First().Food, Is.Not.Null);
        }

        [Test]
        public async Task GetNextRecipesAsync_ShouldReturnOnlyPublic()
        {
            _context.Recipes.AddRange(
                CreateRecipe(1, PrivacyLevel.Public),
                CreateRecipe(1, PrivacyLevel.FriendsOnly)
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetNextRecipesAsync(10, null, null, null);

            Assert.That(result.All(r => r.PrivacyLevel == PrivacyLevel.Public));
        }

        [Test]
        public async Task GetNextRecipesAsync_ShouldPaginate()
        {
            var r1 = CreateRecipe(1);
            r1.Date = DateTime.UtcNow.AddMinutes(-10);

            var r2 = CreateRecipe(1);

            _context.Recipes.AddRange(r1, r2);
            await _context.SaveChangesAsync();

            var result = await _service.GetNextRecipesAsync(10, r2.Date, r2.Id, null);

            Assert.That(result.All(r => r.Id != r2.Id));
        }

        [Test]
        public async Task GetNextRecipesAsync_ShouldFilterByIngredient()
        {
            var food = CreateFood("chicken");
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            var recipe = CreateRecipe(1);

            recipe.Ingredients.Add(new RecipeIngredient
            {
                FoodId = food.Id
            });

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            var result = await _service.GetNextRecipesAsync(10, null, null, "chicken");

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetNextFollowingRecipesAsync_ShouldReturnMutualFriendsOnly()
        {
            var u1 = CreateUser("u1");
            var u2 = CreateUser("u2");

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            _context.Followers.AddRange(
                new Follower { FollowerId = u1.Id, FollowingId = u2.Id },
                new Follower { FollowerId = u2.Id, FollowingId = u1.Id }
            );

            _context.Recipes.Add(CreateRecipe(u2.Id, PrivacyLevel.FriendsOnly));
            await _context.SaveChangesAsync();

            var result = await _service.GetNextFollowingRecipesAsync(u1.Id, 10, null, null, null);

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetUserRecipesAsync_ShouldReturnOnlyPublic_WhenNotFollowing()
        {
            var creator = CreateUser("creator");
            var viewer = CreateUser("viewer");

            _context.Users.AddRange(creator, viewer);

            _context.Recipes.AddRange(
                CreateRecipe(creator.Id, PrivacyLevel.Public),
                CreateRecipe(creator.Id, PrivacyLevel.FriendsOnly)
            );

            await _context.SaveChangesAsync();

            var result = await _service.GetUserRecipesAsync(creator.Id, viewer.Id, 10, null, null);

            Assert.That(result.Count, Is.EqualTo(1));
        }


        [Test]
        public void UpdateRecipeAsync_ShouldThrow_WhenNotFound()
        {
            var updated = CreateRecipe(1);
            updated.Id = 999;

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateRecipeAsync(updated, false));
        }

        [Test]
        public void UpdateRecipeAsync_ShouldThrow_WhenNotOwner()
        {
            var recipe = CreateRecipe(1);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var updated = CreateRecipe(2);
            updated.Id = recipe.Id;

            Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateRecipeAsync(updated, false));
        }


        [Test]
        public async Task DeleteRecipeAsync_ShouldReturnFalse_WhenNotOwner()
        {
            var recipe = CreateRecipe(1);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            var fake = CreateRecipe(2);
            fake.Id = recipe.Id;

            var result = await _service.DeleteRecipeAsync(fake);

            Assert.That(result, Is.False);
        }


        [Test]
        public async Task GetRecipeRatingSummaryAsync_ShouldReturnZero_WhenEmpty()
        {
            var (count, avg) = await _service.GetRecipeRatingSummaryAsync(1);

            Assert.That(count, Is.EqualTo(0));
            Assert.That(avg, Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteRecipeRatingAsync_ShouldFail_WhenMissing()
        {
            var result = await _service.DeleteRecipeRatingAsync(new RecipeRating(1, 1, 5));

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetNextRecipeCommentsAsync_ShouldPaginate()
        {
            var c1 = new RecipeComment(1, 1, "old") { Date = DateTime.UtcNow.AddMinutes(-10) };
            var c2 = new RecipeComment(1, 1, "new") { Date = DateTime.UtcNow };

            _context.RecipeComments.AddRange(c1, c2);
            await _context.SaveChangesAsync();

            var result = await _service.GetNextRecipeCommentsAsync(1, 10, c2.Date, c2.Id);

            Assert.That(result.All(c => c.Id != c2.Id));
        }

        [Test]
        public async Task RecipeList_CRUD_ShouldWork()
        {
            var list = new RecipeList { Name = "Test", UserId = 1 };

            await _service.CreateRecipeListAsync(list);

            var all = await _service.GetAllRecipeListsAsync(1);
            Assert.That(all.Count, Is.EqualTo(1));

            list.Name = "Updated";
            var updated = await _service.UpdateRecipeListAsync(list, 1);
            Assert.That(updated, Is.True);

            await _service.DeleteRecipeListAsync(list.Id, 1);

            Assert.That(_context.RecipeLists.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task RecipeListItems_ShouldAddAndRemove()
        {
            var recipe = CreateRecipe(1);
            _context.Recipes.Add(recipe);

            var list = new RecipeList { Name = "List", UserId = 1 };
            _context.RecipeLists.Add(list);

            await _context.SaveChangesAsync();

            await _service.CreateRecipeListItemAsync(list.Id, recipe.Id);

            Assert.That(_context.RecipeListItems.Count(), Is.EqualTo(1));

            await _service.DeleteRecipeListItemAsync(list.Id, recipe.Id, 1);

            Assert.That(_context.RecipeListItems.Count(), Is.EqualTo(0));
        }
    }
}