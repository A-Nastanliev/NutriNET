namespace NutriNET.Tests
{
    [TestFixture]
    public class MealServiceTests
    {
        private AppDbContext _context;
        private MealService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            _context = new AppDbContext(options);
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

            var result = await _service.UpdateMealAsync(meal, MealType.Dinner);

            Assert.That(result, Is.True);

            var dbMeal = await _context.Meals.FindAsync(meal.Id);
            Assert.That(dbMeal.Type, Is.EqualTo(MealType.Dinner));
        }

        [Test]
        public async Task UpdateMealAsync_ShouldReturnFalse_WhenNotOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            var anotherMeal = new Meal
            {
                Id = meal.Id,
                UserId = 2
            };

            var result = await _service.UpdateMealAsync(anotherMeal, MealType.Dinner);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteMealAsync_ShouldDelete_WhenOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            var result = await _service.DeleteMealAsync(meal);

            Assert.That(result, Is.True);
            Assert.That(_context.Meals.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteMealAsync_ShouldReturnFalse_WhenNotOwner()
        {
            var meal = CreateMeal(1);
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();

            var anotherMeal = new Meal
            {
                Id = meal.Id,
                UserId = 2
            };

            var result = await _service.DeleteMealAsync(anotherMeal);

            Assert.That(result, Is.False);
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

            var result = await _service.CreateMealFoodAsync(1, mealFood);

            Assert.That(result, Is.True);
            Assert.That(_context.MealFoods.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task CreateMealFoodAsync_ShouldFail_WhenNotOwner()
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

            var result = await _service.CreateMealFoodAsync(2, mealFood);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task UpdateMealFoodAsync_ShouldUpdateWeight()
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

            var result = await _service.UpdateMealFoodAsync(1, mealFood.Id, 200);

            Assert.That(result, Is.True);

            var db = await _context.MealFoods.FindAsync(mealFood.Id);
            Assert.That(db.Weight, Is.EqualTo(200));
        }

        [Test]
        public async Task UpdateMealFoodAsync_ShouldReturnFalse_WhenNotFound()
        {
            var result = await _service.UpdateMealFoodAsync(1, 999, 200);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteMealFoodAsync_ShouldDelete()
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

            var result = await _service.DeleteMealFoodAsync(1, mealFood.Id);

            Assert.That(result, Is.True);
            Assert.That(_context.MealFoods.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteMealFoodAsync_ShouldReturnFalse_WhenNotFound()
        {
            var result = await _service.DeleteMealFoodAsync(1, 999);

            Assert.That(result, Is.False);
        }
    }   
}
