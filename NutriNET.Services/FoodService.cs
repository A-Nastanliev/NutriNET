namespace NutriNET.Services
{
    public class FoodService
    {
        private readonly AppDbContext _context;

        public FoodService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateFoodAsync( Food food)
        {
            var validationError = ValidateNutrition(food);
            if (validationError != null)
                return (false, validationError);

            food.DateAdded = DateTime.UtcNow;
            _context.Foods.Add(food);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        private static string? ValidateNutrition(Food food)
        {
            if (food.Proteins < 0 || food.Proteins > 100)
                return "FoodValidationProtein";

            if (food.Fats < 0 || food.Fats > 100)
                return "FoodValidationFats";

            if (food.Carbohydrates < 0 || food.Carbohydrates > 100)
                return "FoodValidationCarbohydrates";

            if (food.Calories < 0 || food.Calories > 900)
                return "FoodValidationCalories";

            return null;
        }

        public async Task<List<Food>> GetNextFoodsAsync(int count, DateTime? lastDateAdded, int? lastFoodId, string search ) 
        {
            var query = _context.Foods.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search
                    .ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    var w = word;
                    query = query.Where(f =>
                        f.Name.Contains(w) ||
                        f.Brand.Contains(w));
                }
            }

            if (lastDateAdded != null && lastFoodId != null)
            {
                query = query.Where(f =>
                    f.DateAdded < lastDateAdded ||
                    (f.DateAdded == lastDateAdded && f.Id < lastFoodId));
            }

            return await query
                .OrderByDescending(f => f.DateAdded)
                .ThenByDescending(f => f.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Food> GetFoodAsync(int id)
        {
            return await _context.Foods.FindAsync(id);
        }

        public async Task<Food> GetFoodByBarcodeAsync(string barcode)
        {
            return await _context.Foods.FirstOrDefaultAsync(f => f.Barcode == barcode);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateFoodAsync(Food foodToUpdate, bool updateImage = false)
        {
            var food = await _context.Foods.FindAsync(foodToUpdate.Id);
            if(food == null)
                return (false, "FoodNotFound");

            var validationError = ValidateNutrition(foodToUpdate);
            if (validationError != null)
                return (false, validationError);

            food.Name = foodToUpdate.Name;
            food.Brand = foodToUpdate.Brand;
            food.Calories = foodToUpdate.Calories;
            food.Proteins = foodToUpdate.Proteins;
            food.Carbohydrates = foodToUpdate.Carbohydrates;
            food.Fats = foodToUpdate.Fats;
            food.Barcode = foodToUpdate.Barcode;
            if (updateImage) 
            {
                food.Image = foodToUpdate.Image;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteFoodAsync(int foodId)
        {
            var food = await _context.Foods.FindAsync(foodId);
            if (food == null)
                return false;

            _context.Foods.Remove(food);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task CreateFoodRequestAsync(FoodRequest foodRequest)
        {
            var exists = await _context.Foods.AnyAsync(f => f.Barcode == foodRequest.Barcode);
            if (exists)
                throw new InvalidOperationException("FoodBarcodeExists");

            var pendingCount = await _context.FoodRequests
              .Where(r => r.SenderId == foodRequest.SenderId && r.Status == RequestStatus.Pending)
              .CountAsync();

            if (pendingCount >= 3)
                throw new InvalidOperationException("MaxFoodRequests");

            foodRequest.DateSent = DateTime.UtcNow;
            foodRequest.Status = RequestStatus.Pending;
            _context.FoodRequests.Add(foodRequest);
            await _context.SaveChangesAsync();
        }

        public async Task<List<FoodRequest>> GetMyPendingFoodRequestsAsync(int userId)
        {
            return await _context.FoodRequests.Where(fr=>fr.Status == RequestStatus.Pending && fr.SenderId == userId).ToListAsync();
        }

        public async Task UpdateFoodRequestAsync(FoodRequest foodRequest)
        { 
            var fr = await _context.FoodRequests.FindAsync(foodRequest.Id);

            if (fr == null)
                throw new KeyNotFoundException();

            if (foodRequest.Status == RequestStatus.Accepted)
            {
                bool exists = await _context.Foods.AnyAsync(f => f.Barcode == fr.Barcode);
                if (exists)
                {
                    fr.Status = RequestStatus.Declined;
                    fr.ActionedById = foodRequest.ActionedById;
                    fr.ActionedOn = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    throw new InvalidOperationException();
                }
            }

            fr.ActionedOn = DateTime.UtcNow;
            fr.ActionedById = foodRequest.ActionedById;
            fr.Status = foodRequest.Status;

            await _context.SaveChangesAsync();
        }

        public async Task<List<FoodRequest>> GetNextFoodRequestsAsync(int count, DateTime? lastDateSent, int? lastRequestId, RequestStatus status)
        {
            var query = _context.FoodRequests.Where(fr => fr.Status == status);

            if (lastDateSent != null && lastRequestId != null)
            {
                query = query.Where(fr =>
                    fr.DateSent < lastDateSent ||
                    (fr.DateSent == lastDateSent && fr.Id < lastRequestId));
            }

            if (status != RequestStatus.Pending)
            {
                query = query.Include(fr => fr.ActionedBy);
            }

            return await query
                .OrderByDescending(fr => fr.DateSent)
                .ThenByDescending(fr => fr.Id)
                .Include(fr => fr.Sender)
                .Take(count)
                .ToListAsync();
        }
    }
}
