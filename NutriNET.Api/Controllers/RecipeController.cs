using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriNET.Api.Dto;
using NutriNET.Api.Services;
using NutriNET.Api.Mappers;
using NutriNET.Api.Dto.Recipe;
using NutriNET.Data.Models;
using NutriNET.Services;
using System.Security.Claims;

namespace NutriNET.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/recipes")]
    public class RecipeController : ControllerBase
    {
        private readonly RecipeService _service;
        private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private IConfiguration _configuration;

        private IImageStorageService _imageStorageService;

        public RecipeController(RecipeService service, IConfiguration configuration, IImageStorageService imageStorageService)
        {
            _service = service;
            _configuration = configuration;
            _imageStorageService = imageStorageService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromForm] RecipeFormDto req)
        {
            if (req.Image == null || req.Image.Length == 0)
                return BadRequest();

            string imagePath = null;
            try
            {
                imagePath = await _imageStorageService.SaveImageAsync(req.Image, "recipe-images");
                var ingredients = req.Ingredients.Select(i => new RecipeIngredient
                {
                    FoodId = i.FoodId,
                    Weight = i.Weight
                }).ToList();

                var recipe = new Recipe(req.Name, req.PrivacyLevel, imagePath, req.Description, UserId.Value, ingredients);

                await _service.CreateRecipeAsync(recipe);

                return StatusCode(201, new
                {
                    RecipeId = recipe.Id,
                });
            }
            catch (InvalidOperationException e)
            {
                return BadRequest();
            }
            catch (Exception e)
            {
                if (imagePath != null) _imageStorageService.DeleteImage(imagePath);
                return BadRequest();
            }
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetNextPublicRecipes([FromQuery] CursorDto load, [FromQuery] string? search)
        {
            var recipes = await _service.GetNextRecipesAsync(load.Count, load.CursorDate, load.CursorId, search);
            var baseUrl = _configuration["App:BaseUrl"];
            DateTime? cursorDate = recipes.LastOrDefault()?.Date;
            int? cursorKey = recipes.LastOrDefault()?.Id;
            var recipeDtos = recipes.Select(r => r.ToFoodDto(baseUrl)).ToList();
            return Ok(new
            {
                recipes = recipeDtos,
                nextCursorDate = cursorDate,
                nextCursorId = cursorKey
            });
        }

        [HttpGet("following")]
        public async Task<IActionResult> GetNextFollowingRecipes([FromQuery] CursorDto load, [FromQuery] string? search)
        {
            var recipes = await _service.GetNextFollowingRecipesAsync(UserId.Value, load.Count, load.CursorDate, load.CursorId, search);
            DateTime? cursorDate = recipes.LastOrDefault()?.Date;
            int? cursorKey = recipes.LastOrDefault()?.Id;
            var baseUrl = _configuration["App:BaseUrl"];
            var recipeDtos = recipes.Select(r => r.ToFoodDto(baseUrl)).ToList();
            return Ok(new
            {
                recipes = recipeDtos,
                nextCursorDate = cursorDate,
                nextCursorId = cursorKey
            });
        }

        [HttpGet("user/{creatorId}")]
        public async Task<IActionResult> GetUserRecipes(int creatorId, [FromQuery] CursorDto load)
        {
            var viewerId = UserId.Value;

            var recipes = await _service.GetUserRecipesAsync(creatorId, viewerId, load.Count, load.CursorDate, load.CursorId);

            var baseUrl = _configuration["App:BaseUrl"];

            DateTime? cursorDate = recipes.LastOrDefault()?.Date;
            int? cursorKey = recipes.LastOrDefault()?.Id;

            var recipeDtos = recipes.Select(r => r.ToFoodDto(baseUrl)).ToList();

            return Ok(new
            {
                recipes = recipeDtos,
                nextCursorDate = cursorDate,
                nextCursorId = cursorKey
            });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipeDetails(int id)
        {
            var recipe = await _service.GetRecipeDetailsAsync(id);
            if (recipe == null) return NotFound();

            var baseUrl = _configuration["App:BaseUrl"];
            var (count, average) = await _service.GetRecipeRatingSummaryAsync(id);
            var userRating = await _service.GetUserRatingAsync(UserId.Value, id);

            var dto = recipe.ToDto(baseUrl);

            return Ok(new
            {
                recipe = dto,
                ratingCount = count,
                ratingAverage = average,
                userRating = userRating
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromForm] RecipeFormDto req)
        {
            var existing = await _service.GetRecipeAsync(id);
            if (existing == null || existing?.CreatorId != UserId)
                return NotFound(new { error = "RecipeNotFound" });

            string? oldImagePath = existing.Image;
            string? newImagePath = null;
            bool imageUpdated = false;

            if (req.Image != null && req.Image.Length > 0)
            {
                try
                {
                    newImagePath = await _imageStorageService.SaveImageAsync(req.Image, "recipe-images");
                    imageUpdated = true;
                }
                catch (InvalidOperationException)
                {
                    return BadRequest();
                }
            }

            try
            {
                var recipe = new Recipe(req.Name, req.PrivacyLevel, newImagePath, req.Description, UserId.Value,
                    req.Ingredients.Select(i => new RecipeIngredient
                    {
                        FoodId = i.FoodId,
                        Weight = i.Weight
                    }).ToList()
                )
                { Id = id };

                await _service.UpdateRecipeAsync(recipe, imageUpdated);

                if (imageUpdated && !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    _imageStorageService.DeleteImage(oldImagePath);

                    var baseUrl = _configuration["App:BaseUrl"];
                    return Ok(new { image = $"{baseUrl}/{newImagePath}" });
                }

                return NoContent();
            }
            catch (KeyNotFoundException e)
            {
                if (imageUpdated && newImagePath != null)
                    _imageStorageService.DeleteImage(newImagePath);

                return NotFound(new { error = e.Message });
            }
            catch (UnauthorizedAccessException)
            {
                if (imageUpdated && newImagePath != null)
                    _imageStorageService.DeleteImage(newImagePath);

                return Forbid();
            }
            catch
            {
                if (imageUpdated && newImagePath != null)
                    _imageStorageService.DeleteImage(newImagePath);

                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            try
            {
                var recipe = await _service.GetRecipeAsync(id);
                if (recipe == null || recipe?.CreatorId != UserId)
                    return NotFound(new { error = "RecipeNotFound" });

                string imagePath = recipe.Image;

                await _service.DeleteRecipeAsync(new Recipe { Id = id, CreatorId = UserId });
                if (await _service.GetRecipeAsync(id) == null)
                {
                    try
                    {
                        _imageStorageService.DeleteImage(imagePath);
                    }
                    catch (Exception imgEx)
                    {
                        Console.Error.WriteLine($"Image deletion failed for {imagePath}: {imgEx.Message}");
                    }
                }
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }


        [HttpPost("{id}/rating")]
        public async Task<IActionResult> CreateRecipeRating(int id, [FromBody] int rating)
        {
            if (rating < 1 || rating > 5)
                return BadRequest();

            try
            {
                var recipeRating = new RecipeRating(UserId.Value, id, rating);
                await _service.CreateRecipeRatingAsync(recipeRating);

                return StatusCode(201);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create comment for RecipeId {id}: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPut("{id}/rating")]
        public async Task<IActionResult> UpdateRecipeRating(int id, [FromBody] int rating)
        {
            if (rating < 1 || rating > 5)
                return BadRequest();

            try
            {
                var recipeRating = new RecipeRating(UserId.Value, id, rating);
                await _service.UpdateRecipeRatingAsync(recipeRating);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to update rating for RecipeId {id}: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpDelete("{id}/rating")]
        public async Task<IActionResult> DeleteRecipeRating(int id)
        {
            try
            {
                var recipeRating = new RecipeRating(UserId.Value, id);
                await _service.DeleteRecipeRatingAsync(recipeRating);

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete recipe rating for RecipeId {id}: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> CreateRecipeComment(int id, [FromBody] string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return BadRequest();

            try
            {
                var recipeComment = new RecipeComment(UserId.Value, id, comment);
                await _service.CreateRecipeCommentAsync(recipeComment);

                return StatusCode(201, recipeComment.Id);
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetNextRecipeComments(int id, [FromQuery] CursorDto load)
        {
            var comments = await _service.GetNextRecipeCommentsAsync(id, load.Count, load.CursorDate, load.CursorId);

            var baseUrl = _configuration["App:BaseUrl"];
            return Ok(comments.Select(c => c.ToDto(baseUrl)));
        }

        [HttpPut("comments/{commentId}")]
        public async Task<IActionResult> UpdateRecipeComment(int commentId, [FromBody] string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return BadRequest();

            try
            {
                var recipeComment = new RecipeComment(UserId.Value, 0, comment) { Id = commentId };
                await _service.UpdateRecipeCommentAsync(recipeComment);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to update comment {commentId}: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpDelete("comments/{commentId}")]
        [Authorize("AdminOrModerator")]
        public async Task<IActionResult> DeleteRecipeComment(int commentId)
        {
            try
            {
                await _service.DeleteRecipeCommentAsync(new RecipeComment { Id = commentId });
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete comment {commentId}: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpDelete("comments/{commentId}/self")]
        public async Task<IActionResult> DeleteOwnRecipeComment(int commentId)
        {
            try
            {
                await _service.DeleteRecipeCommentAsync(new RecipeComment { Id = commentId }, UserId.Value);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }

        [HttpPost("lists")]
        public async Task<IActionResult> CreateRecipeList([FromBody] RecipeListDto req)
        {
            try
            {
                var list = new RecipeList
                {
                    Name = req.Name,
                    UserId = UserId.Value
                };

                await _service.CreateRecipeListAsync(list);
                return StatusCode(201, new { list.Id });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpGet("lists")]
        public async Task<IActionResult> GetAllRecipeLists()
        {
            try
            {
                var lists = await _service.GetAllRecipeListsAsync(UserId.Value);
                return Ok(lists.Select(rl => rl.ToDto()).ToList());
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpPut("lists")]
        public async Task<IActionResult> UpdateRecipeList(int listId, [FromBody] RecipeListDto req)
        {
            try
            {
                var list = new RecipeList
                {
                    Id = req.Id,
                    Name = req.Name
                };

                await _service.UpdateRecipeListAsync(list, UserId.Value);
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpDelete("lists/{listId}")]
        public async Task<IActionResult> DeleteRecipeList(int listId)
        {
            try
            {
                await _service.DeleteRecipeListAsync(listId, UserId.Value);
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpPost("lists/items")]
        public async Task<IActionResult> CreateRecipeListItem([FromBody] RecipeListItemDto req)
        {
            try
            {
                await _service.CreateRecipeListItemAsync(req.ListId, req.RecipeId);
                return StatusCode(201);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        [HttpGet("lists/{listId}/recipes")]
        public async Task<IActionResult> GetNextRecipesInList(int listId, [FromQuery] CursorDto load)
        {
            try
            {
                var recipes = await _service.GetNextRecipesInListAsync(listId, load.Count, load.CursorDate, load.CursorId);
                var baseUrl = _configuration["App:BaseUrl"];
                return Ok(new
                {
                    recipes = recipes.Select(r => r.ToFoodDto(baseUrl)),
                    nextCursorId = recipes.LastOrDefault()?.Id
                });
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }

        [HttpDelete("lists/{listId}/items/{recipeId}")]
        public async Task<IActionResult> DeleteRecipeListItem(int listId, int recipeId)
        {
            try
            {
                await _service.DeleteRecipeListItemAsync(listId, recipeId, UserId.Value);
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }
    }
}
