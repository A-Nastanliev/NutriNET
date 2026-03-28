namespace NutriNET.Tests
{
    [TestFixture]
    public class FoodServiceTests
    {
        private AppDbContext _context;
        private FoodService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;

            _context = new AppDbContext(options);
            _service = new FoodService(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        private Food CreateFood(string name = "Chicken", string brand = "Brand", string barcode = "12345678",
            double calories = 200, double protein = 20, double carbs = 10, double fats = 5)
        {
            return new Food
            {
                Name = name,
                Brand = brand,
                Barcode = barcode,
                Calories = calories,
                Proteins = protein,
                Carbohydrates = carbs,
                Fats = fats
            };
        }

        private FoodRequest CreateFoodRequest(int senderId = 1,string barcode = "12345678",string name = "Test Food",    string brand = "Test Brand")
        {
            return new FoodRequest
            {
                SenderId = senderId,
                Barcode = barcode,
                Name = name,
                Brand = brand,
                Status = RequestStatus.Pending
            };
        }

        [Test]
        public async Task CreateFoodAsync_ShouldCreateFood()
        {
            var food = CreateFood();

            var result = await _service.CreateFoodAsync(food);

            Assert.That(result.Success, Is.True);
            Assert.That(_context.Foods.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task CreateFoodAsync_ShouldFail_WhenInvalidNutrition()
        {
            var food = CreateFood(protein: 150);

            var result = await _service.CreateFoodAsync(food);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("FoodValidationProtein"));
        }

        [Test]
        public async Task GetFoodAsync_ShouldReturnFood()
        {
            var food = CreateFood();
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            var result = await _service.GetFoodAsync(food.Id);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetFoodByBarcodeAsync_ShouldReturnFood()
        {
            var food = CreateFood(barcode: "999");
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            var result = await _service.GetFoodByBarcodeAsync("999");

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task UpdateFoodAsync_ShouldUpdateFood()
        {
            var food = CreateFood();
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            var updated = CreateFood("Updated");
            updated.Id = food.Id;

            var result = await _service.UpdateFoodAsync(updated);

            Assert.That(result.Success, Is.True);

            var dbFood = await _context.Foods.FindAsync(food.Id);
            Assert.That(dbFood.Name, Is.EqualTo("Updated"));
        }

        [Test]
        public async Task UpdateFoodAsync_ShouldFail_WhenNotFound()
        {
            var food = CreateFood();
            food.Id = 999;

            var result = await _service.UpdateFoodAsync(food);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("FoodNotFound"));
        }

        [Test]
        public async Task UpdateFoodAsync_ShouldFail_WhenInvalidNutrition()
        {
            var food = CreateFood();
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            var updated = CreateFood(protein: -10);
            updated.Id = food.Id;

            var result = await _service.UpdateFoodAsync(updated);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("FoodValidationProtein"));
        }

        [Test]
        public async Task DeleteFoodAsync_ShouldDeleteFood_WhenExists()
        {
            var food = CreateFood();
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            await _service.DeleteFoodAsync(food.Id);

            Assert.That(_context.Foods.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteFoodAsync_ShouldThrow_WhenFoodNotFound()
        {
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteFoodAsync(999));
        }

        [Test]
        public async Task GetNextFoodsAsync_ShouldFilterBySearch()
        {
            _context.Foods.Add(CreateFood("chicken breast", "brandA"));
            _context.Foods.Add(CreateFood("beef steak", "brandB"));
            await _context.SaveChangesAsync();

            var result = await _service.GetNextFoodsAsync(10, null, null, "chicken");

            Assert.That(result.Count, Is.EqualTo(1));
        }

        [Test]
        public void CreateFoodRequestAsync_ShouldThrow_WhenBarcodeExists()
        {
            var food = CreateFood(barcode: "111");
            _context.Foods.Add(food);
            _context.SaveChanges();

            var request = new FoodRequest
            {
                Barcode = "111",
                SenderId = 1
            };

            Assert.ThrowsAsync<InvalidOperationException>(() =>_service.CreateFoodRequestAsync(request));
        }

        [Test]
        public async Task CreateFoodRequestAsync_ShouldCreateRequest()
        {
            var request = CreateFoodRequest();

            await _service.CreateFoodRequestAsync(request);

            Assert.That(_context.FoodRequests.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateFoodRequestAsync_ShouldThrow_WhenTooManyPending()
        {
            for (int i = 0; i < 3; i++)
            {
                _context.FoodRequests.Add(CreateFoodRequest());
            }

            _context.SaveChanges();

            var request = CreateFoodRequest();

            Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateFoodRequestAsync(request));
        }

        [Test]
        public void UpdateFoodRequestAsync_ShouldThrow_WhenNotFound()
        {
            var request = new FoodRequest { Id = 999 };

            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateFoodRequestAsync(request));
        }

        [Test]
        public async Task UpdateFoodRequestAsync_ShouldUpdateStatus()
        {
            var request = CreateFoodRequest();

            _context.FoodRequests.Add(request);
            await _context.SaveChangesAsync();

            var update = new FoodRequest
            {
                Id = request.Id,
                Status = RequestStatus.Declined,
                ActionedById = 2
            };

            await _service.UpdateFoodRequestAsync(update);

            var db = await _context.FoodRequests.FindAsync(request.Id);

            Assert.That(db.Status, Is.EqualTo(RequestStatus.Declined));
        }
    }
}
