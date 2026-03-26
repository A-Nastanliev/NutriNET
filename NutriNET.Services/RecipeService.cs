namespace NutriNET.Services
{
    public class RecipeService
    {
        private readonly AppDbContext _context;

        public RecipeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateRecipeAsync(Recipe recipe)
        {
            recipe.Date = DateTime.UtcNow;
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
        }

        public async Task<Recipe> GetRecipeAsync(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe.PrivacyLevel == PrivacyLevel.Archieved)
                return null;

            return recipe;
        }

        public async Task<Recipe> GetRecipeDetailsAsync(int id)
        {
            return await _context.Recipes
              .Include(r => r.Creator)
              .Include(r => r.Ingredients)
                  .ThenInclude(i => i.Food)
              .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Recipe>> GetNextRecipesAsync(int count, DateTime? lastDate, int? lastRecipeId, string search)
        {
            var query = _context.Recipes.Where(r => r.PrivacyLevel == PrivacyLevel.Public);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    var w = word;
                    query = query.Where(r =>
                        r.Name.Contains(w) ||                     
                        r.Creator.Username.Contains(w) ||  
                        r.Ingredients.Any(i => i.Food.Name.Contains(w))
                    );
                }
            }

            if (lastDate != null && lastRecipeId != null)
            {
                query = query.Where(r =>
                    r.Date < lastDate ||
                    (r.Date == lastDate && r.Id < lastRecipeId));
            }

            return await query
                .Include(r => r.Creator)
                .Include(r => r.Ingredients)
                    .ThenInclude(i => i.Food)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Recipe>> GetNextFollowingRecipesAsync( int userId, int count, DateTime? lastDate, int? lastRecipeId, string search)
        {
            var followingIds = await _context.Followers
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            var query = _context.Recipes
                .Where(r => r.CreatorId != null)
                .Where(r =>
                    (r.PrivacyLevel == PrivacyLevel.Public &&
                     followingIds.Contains(r.CreatorId.Value)) ||

                    (r.PrivacyLevel == PrivacyLevel.FriendsOnly &&
                     followingIds.Contains(r.CreatorId.Value) &&
                     _context.Followers.Any(f => f.FollowerId == r.CreatorId && f.FollowingId == userId))
                );

            if (!string.IsNullOrWhiteSpace(search))
            {
                var words = search.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    var w = word;

                    query = query.Where(r =>
                        r.Name.Contains(w) ||
                        r.Creator.Username.Contains(w) ||
                        r.Ingredients.Any(i => i.Food.Name.Contains(w))
                    );
                }
            }

            if (lastDate != null && lastRecipeId != null)
            {
                query = query.Where(r =>
                    r.Date < lastDate ||
                    (r.Date == lastDate && r.Id < lastRecipeId));
            }

            return await query
                .Include(r => r.Creator)
                .Include(r => r.Ingredients)
                    .ThenInclude(i => i.Food)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Recipe>> GetUserRecipesAsync(int creatorId,int viewerId, int count, DateTime? lastDate, int? lastRecipeId)
        {
            var query = _context.Recipes
                .Where(r => r.CreatorId == creatorId);

            if (creatorId == viewerId)
            {
                query = query.Where(r => r.PrivacyLevel != PrivacyLevel.Archieved);
            }
            else
            {
                var isFollowing = await _context.Followers
                    .AnyAsync(f => f.FollowerId == viewerId && f.FollowingId == creatorId);

                var isFollowedBack = await _context.Followers
                    .AnyAsync(f => f.FollowerId == creatorId && f.FollowingId == viewerId);

                var isMutual = isFollowing && isFollowedBack;

                if (isMutual)
                {
                    query = query.Where(r =>
                        r.PrivacyLevel == PrivacyLevel.Public ||
                        r.PrivacyLevel == PrivacyLevel.FriendsOnly
                    );
                }
                else
                {
                    query = query.Where(r =>
                        r.PrivacyLevel == PrivacyLevel.Public
                    );
                }
            }

            if (lastDate != null && lastRecipeId != null)
            {
                query = query.Where(r =>
                    r.Date < lastDate ||
                    (r.Date == lastDate && r.Id < lastRecipeId));
            }

            return await query
                .Include(r => r.Creator)
                .Include(r => r.Ingredients)
                    .ThenInclude(i => i.Food)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Id)
                .Take(count)
                .ToListAsync();
        }


        public async Task UpdateRecipeAsync(Recipe updated, bool updateImage)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == updated.Id);

            if (recipe == null)
                throw new KeyNotFoundException("RecipeNotFound");

            if (recipe.CreatorId != updated.CreatorId)
                throw new UnauthorizedAccessException();

            recipe.Name = updated.Name;
            recipe.Description = updated.Description;
            recipe.PrivacyLevel = updated.PrivacyLevel;

            if (updateImage)
                recipe.Image = updated.Image;

            _context.RecipeIngredients.RemoveRange(recipe.Ingredients);

            foreach (var i in updated.Ingredients)
                i.RecipeId = recipe.Id;

            await _context.RecipeIngredients.AddRangeAsync(updated.Ingredients);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteRecipeAsync(Recipe recipe)
        {
            var r = await _context.Recipes.FindAsync(recipe.Id);
            if (r == null || r.CreatorId != recipe.CreatorId)
                return false;

            if (await _context.MealFoods.AnyAsync(mf=>mf.RecipeId == recipe.Id)) 
            {
                r.PrivacyLevel = PrivacyLevel.Archieved;
            }
            else
            {
                _context.Recipes.Remove(r);
            }
            return await _context.SaveChangesAsync() > 0;
        }


        public async Task CreateRecipeRatingAsync(RecipeRating rating) 
        {
            _context.RecipeRatings.Add(rating);
           await _context.SaveChangesAsync();
        }

        public async Task<(int count, double average)> GetRecipeRatingSummaryAsync(int recipeId)
        {
            var result = await _context.RecipeRatings
                .Where(r => r.RecipeId == recipeId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    count = g.Count(),
                    average = g.Average(r => (double)r.Rating)
                })
                .FirstOrDefaultAsync();

            return result == null ? (0, 0d) : (result.count, result.average);
        }

        public async Task<bool> UpdateRecipeRatingAsync(RecipeRating rating)
        {
            var recipeRating = await _context.RecipeRatings.FindAsync(rating.UserId, rating.RecipeId);
            if (recipeRating == null) 
                return false;

            recipeRating.Rating = rating.Rating;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteRecipeRatingAsync(RecipeRating rating) 
        {
            _context.RecipeRatings.Remove(await _context.RecipeRatings.FindAsync((rating.UserId, rating.RecipeId)));
            return await _context.SaveChangesAsync() >0;
        }

        public async Task CreateRecipeCommentAsync(RecipeComment comment)
        {
            comment.Date = DateTime.UtcNow;
            _context.RecipeComments.Add(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RecipeComment>> GetNextRecipeCommentsAsync(int recipeId, int count, DateTime? lastDate, int? lastCommentId)
        {
            var query = _context.RecipeComments.Where(c => c.RecipeId == recipeId);

            if (lastDate != null && lastCommentId != null)
            {
                query = query.Where(c =>
                    c.Date < lastDate ||
                    (c.Date == lastDate && c.Id < lastCommentId));
            }

            return await query
                .OrderByDescending(c => c.Date)
                .ThenByDescending(c => c.Id)
                .Include(c => c.User)
                .Take(count)
                .ToListAsync();
        }


        public async Task<bool> UpdateRecipeCommentAsync(RecipeComment comment)
        {
            var c = await _context.RecipeComments.FindAsync(comment.Id);
            if (c == null || c.UserId != comment.UserId) return false;

            c.Comment = comment.Comment;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteRecipeCommentAsync(RecipeComment comment, int? userId = null)
        {
            var c = await _context.RecipeComments.FindAsync(comment.Id);
            if (c == null || (c.UserId != userId && userId!=null)) return false;

            _context.RecipeComments.Remove(c);
           return await _context.SaveChangesAsync() > 0;
        }

        public async Task CreateRecipeListAsync(RecipeList list)
        {
            _context.RecipeLists.Add(list);
            await _context.SaveChangesAsync();
        }

        public async Task<List<RecipeList>> GetAllRecipeListsAsync(int userId)
        {
            return await _context.RecipeLists
                .Where(rl => rl.UserId == userId)
                .OrderByDescending(rl => rl.Id)
                .ToListAsync();
        }

        public async Task<bool> UpdateRecipeListAsync(RecipeList list, int userId)
        {
            var rl = await _context.RecipeLists.FindAsync(list.Id);
            if (rl == null || rl.UserId != userId) return false;

            rl.Name = list.Name;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task DeleteRecipeListAsync(int listId, int userId)
        {
            var rl = await _context.RecipeLists.FindAsync(listId);
            if (rl == null || rl.UserId != userId) return ;

            _context.RecipeLists.Remove(rl);
           await _context.SaveChangesAsync() ;
        }

        public async Task CreateRecipeListItemAsync(int listId, int recipeId)
        {
            var item = new RecipeListItem
            {
                RecipeListId = listId,
                RecipeId = recipeId,
                CreatedAt = DateTime.UtcNow
            };
            _context.RecipeListItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Recipe>> GetNextRecipesInListAsync(int listId,  int count,  DateTime? lastDate,  int? lastRecipeId)
        {
            var query = from rli in _context.RecipeListItems
                        join r in _context.Recipes on rli.RecipeId equals r.Id
                        where rli.RecipeListId == listId
                        select r;

            if (lastDate.HasValue && lastRecipeId.HasValue)
            {
                query = query.Where(r =>
                    r.Date < lastDate.Value ||
                    (r.Date == lastDate.Value && r.Id < lastRecipeId.Value));
            }

            return await query
                .Include(r => r.Creator)
                .Include(r => r.Ingredients)
                    .ThenInclude(i => i.Food)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task DeleteRecipeListItemAsync(int listId, int recipeId, int userId)
        {
            var item = await _context.RecipeListItems
                .Include(rli => rli.RecipeList)
                .FirstOrDefaultAsync(rli => rli.RecipeListId == listId && rli.RecipeId == recipeId);

            if (item == null || item.RecipeList.UserId != userId)
                return ;

            _context.RecipeListItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
