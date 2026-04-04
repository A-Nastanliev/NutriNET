namespace NutriNET.Tests
{
    [TestFixture]
    public class MealServiceTests
    {
        private NutriDbContext _context;
        private MealService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<NutriDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;

            _context = new NutriDbContext(options);
            _service = new MealService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        private Meal CreateMeal(int userId = 1, DateTime? dateTime = null)
        {
            return new Meal
            {
                UserId = userId,
                DateTime = dateTime ?? DateTime.UtcNow,
                Type = MealType.Breakfast,
                MealFoods = new()
            };
        }

        private Food CreateFood()
        {
            return new Food
            {
                Name = "Chicken",
                Brand = "Brand",
                Calories = 200,
                Proteins = 20,
                Carbohydrates = 10,
                Fats = 5
            };
        }

        [Test]
        public async Task CreateMealAsync_ShouldCreateMeal()
        {
            var meal = CreateMeal();

            await _service.CreateMealAsync(meal);

            Assert.That(_context.Meals.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetTodaysMealsAsync_ShouldReturnOnlyTodayMeals()
        {
            var today = DateTime.UtcNow;
            var yesterday = today.AddDays(-1);

            _context.Meals.Add(CreateMeal(1, today));
            _context.Meals.Add(CreateMeal(1, yesterday));
            await _context.SaveChangesAsync();

            var result = await _service.GetTodaysMealsAsync(1, TimeSpan.Zero);

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateMealAsync_ShouldUpdate_WhenOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            await _service.UpdateMealAsync(meal, MealType.Dinner);

            var dbMeal = await _context.Meals.FindAsync(meal.Id);
            Assert.That(dbMeal.Type, Is.EqualTo(MealType.Dinner));
        }

        [Test]
        public async Task UpdateMealAsync_ShouldThrow_WhenNotOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            var anotherMeal = new Meal
            {
                Id = meal.Id,
                UserId = 2
            };

            Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateMealAsync(anotherMeal, MealType.Dinner));
        }

        [Test]
        public async Task DeleteMealAsync_ShouldDelete_WhenOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            await _service.DeleteMealAsync(meal);

            Assert.That(_context.Meals.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteMealAsync_ShouldThrow_WhenNotOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            var anotherMeal = new Meal
            {
                Id = meal.Id,
                UserId = 2
            };

            Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteMealAsync(anotherMeal));
        }

        [Test]
        public async Task CreateMealFoodAsync_ShouldAdd_WhenUserOwnsMeal()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);

            var food = CreateFood();
            _context.Foods.Add(food);

            await _context.SaveChangesAsync();

            var mealFood = new MealFood
            {
                MealId = meal.Id,
                FoodId = food.Id,
                Weight = 100
            };

            await _service.CreateMealFoodAsync(1, mealFood);

            Assert.That(_context.MealFoods.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task CreateMealFoodAsync_ShouldThrow_WhenNotOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);

            var food = CreateFood();
            _context.Foods.Add(food);

            await _context.SaveChangesAsync();

            var mealFood = new MealFood
            {
                MealId = meal.Id,
                FoodId = food.Id,
                Weight = 100
            };

            Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateMealFoodAsync(2, mealFood));
        }

        [Test]
        public async Task UpdateMealFoodAsync_ShouldUpdateWeight_WhenExists()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);

            var food = CreateFood();
            _context.Foods.Add(food);

            await _context.SaveChangesAsync();

            var mealFood = new MealFood
            {
                MealId = meal.Id,
                FoodId = food.Id,
                Weight = 100
            };

            _context.MealFoods.Add(mealFood);
            await _context.SaveChangesAsync();

            await _service.UpdateMealFoodAsync(1, mealFood.Id, 200);

            var db = await _context.MealFoods.FindAsync(mealFood.Id);
            Assert.That(db.Weight, Is.EqualTo(200));
        }

        [Test]
        public async Task UpdateMealFoodAsync_ShouldThrow_WhenNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateMealFoodAsync(1, 999, 200));
        }

        [Test]
        public async Task DeleteMealFoodAsync_ShouldDelete_WhenExists()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);

            var food = CreateFood();
            _context.Foods.Add(food);

            await _context.SaveChangesAsync();

            var mealFood = new MealFood
            {
                MealId = meal.Id,
                FoodId = food.Id,
                Weight = 100
            };

            _context.MealFoods.Add(mealFood);
            await _context.SaveChangesAsync();

            await _service.DeleteMealFoodAsync(1, mealFood.Id);

            Assert.That(_context.MealFoods.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteMealFoodAsync_ShouldThrow_WhenNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteMealFoodAsync(1, 999));
        }
    }
}
