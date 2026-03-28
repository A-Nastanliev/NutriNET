namespace NutriNET.Services
{
    public class MealService
    {
        private readonly AppDbContext _context;

        public MealService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateMealAsync( Meal meal)
        {
            meal.DateTime = DateTime.UtcNow;
            _context.Meals.Add(meal);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Meal>> GetTodaysMealsAsync(int userId, TimeSpan timeOffset)
        {
            var localToday = DateTime.UtcNow.Add(timeOffset).Date;
            var utcStart = localToday - timeOffset;
            var utcEnd = utcStart.AddDays(1);

            var meals = await _context.Meals
                .AsNoTracking()
                .Where(m => m.UserId == userId &&
                            m.DateTime >= utcStart &&
                            m.DateTime < utcEnd)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Recipe)
                        .ThenInclude(rf => rf.Ingredients)
                            .ThenInclude(i => i.Food)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Recipe)
                        .ThenInclude(r => r.Creator)
                .OrderByDescending(m => m.DateTime)
                .ToListAsync();

            return meals;
        }

        public async Task<List<(DateTime Date, List<Meal> Meals)>> GetMealsByDayAsync
            (int userId, int dayCount, TimeSpan timeOffset, DateTime? cursorDate = null)
        {
            var todayLocal = DateTime.UtcNow.Add(timeOffset).Date;

            var todayUtcStart = todayLocal - timeOffset;

            var baseQuery = _context.Meals
                .AsNoTracking()
                .Where(m => m.UserId == userId)
                .Where(m => m.DateTime < todayUtcStart);

            if (cursorDate.HasValue)
            {
                var cursorUtc = cursorDate.Value.Date - timeOffset;
                baseQuery = baseQuery.Where(m => m.DateTime < cursorUtc);
            }

            var mealsRaw = await baseQuery
                .Select(m => new { m.Id, m.DateTime })
                .ToListAsync();

            if (!mealsRaw.Any())
                return new List<(DateTime, List<Meal>)>();

            var targetDays = mealsRaw
                .Select(m => new
                {
                    m.Id,
                    LocalDate = m.DateTime.Add(timeOffset).Date
                })
                .GroupBy(x => x.LocalDate)
                .OrderByDescending(g => g.Key)
                .Take(dayCount)
                .Select(g => g.Key)
                .ToList();

            if (!targetDays.Any())
                return new List<(DateTime, List<Meal>)>();

            var minDay = targetDays.Min();
            var maxDay = targetDays.Max().AddDays(1);

            var utcStart = minDay - timeOffset;
            var utcEnd = maxDay - timeOffset;

            var meals = await _context.Meals
                .AsNoTracking()
                .Where(m => m.UserId == userId &&
                            m.DateTime >= utcStart &&
                            m.DateTime < utcEnd)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Food)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Recipe)
                        .ThenInclude(r => r.Ingredients)
                            .ThenInclude(i => i.Food)
                .Include(m => m.MealFoods)
                    .ThenInclude(mf => mf.Recipe)
                        .ThenInclude(r => r.Creator)
                .ToListAsync();

            var result = meals
                .Select(m => new
                {
                    Meal = m,
                    LocalDate = m.DateTime.Add(timeOffset).Date
                })
                .Where(x => targetDays.Contains(x.LocalDate))
                .GroupBy(x => x.LocalDate)
                .OrderByDescending(g => g.Key)
                .Select(g => (
                    Date: g.Key,
                    Meals: g.OrderByDescending(x => x.Meal.DateTime)
                            .Select(x => x.Meal)
                            .ToList()
                ))
                .ToList();

            return result;
        }

        public async Task UpdateMealAsync( Meal meal, MealType type)
        {
            var m = await _context.Meals.FindAsync(meal.Id);
            if (m == null || m?.UserId != meal.UserId)
                throw new InvalidOperationException("MealNotFound");

            m.Type = type;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMealAsync( Meal meal)
        {
            var m = await _context.Meals.FindAsync(meal.Id);
            if (m == null || m?.UserId != meal.UserId)
                throw new InvalidOperationException("MealNotFound");

            _context.Meals.Remove(m);
            await _context.SaveChangesAsync();
        }

        public async Task CreateMealFoodAsync(int userId, MealFood mealFood)
        {
            var ownsMeal = await _context.Meals
                .AnyAsync(m => m.Id == mealFood.MealId && m.UserId == userId);

            if (!ownsMeal)
                throw new InvalidOperationException("MealNotFound");

            await _context.MealFoods.AddAsync(mealFood);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMealFoodAsync(int userId, int mealFoodId, double newWeight)
        {
            var mealFood = await _context.MealFoods
               .Where(mf => mf.Id == mealFoodId && mf.Meal.UserId == userId)
               .FirstOrDefaultAsync();

            if (mealFood == null)
                throw new KeyNotFoundException("MealFoodNotFound");

            mealFood.Weight = newWeight;
            await _context.SaveChangesAsync();
        }


        public async Task DeleteMealFoodAsync(int userId, int mealFoodId)
        {
            var mealFood = await _context.MealFoods
               .Where(mf => mf.Id == mealFoodId && mf.Meal.UserId == userId)
               .FirstOrDefaultAsync();

            if (mealFood == null)
                throw new KeyNotFoundException("MealFoodNotFound");

            _context.MealFoods.Remove(mealFood);
            await _context.SaveChangesAsync();
        }

    }
}
