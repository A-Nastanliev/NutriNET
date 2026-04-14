using NutriNET.Data.Enums;
using NutriNET.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriNET.Services;
using System.Security.Claims;
using NutriNET.Api.Dto;
using NutriNET.Api.Dto.Food;
using NutriNET.Api.Dto.Meal;
using NutriNET.Api.Mappers;

namespace NutriNET.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/meals")]
    public class MealController : ControllerBase
    {
        private readonly MealService _service;

        private readonly IConfiguration _configuration;

        private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public MealController(MealService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMeal([FromBody] MealType mealType)
        {
            var entity = new Meal(UserId.Value, mealType);

            await _service.CreateMealAsync(entity);

            return StatusCode(201, new { id = entity.Id });
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodaysMeals([FromQuery] TimeSpan timeOffset)
        {
            var meals = await _service.GetTodaysMealsAsync(UserId.Value, timeOffset);
            var baseUrl = _configuration["App:BaseUrl"];

            var mealDtos = meals.Select(m => m.ToDto(baseUrl)).OrderByDescending(m => m.DateTime).ToList();

            return Ok(new { meals = mealDtos });
        }

        [HttpGet]
        public async Task<IActionResult> GetNextMeals([FromQuery] CursorDto cursor, [FromQuery] TimeSpan timeOffset)
        {
            var mealDays = await _service.GetMealsByDayAsync(UserId.Value, cursor.Count, timeOffset, cursor.CursorDate);
            var mealDayDtos = new List<(DateTime date, List<MealDto> dtos)>();
            var baseUrl = _configuration["App:BaseUrl"];
            var result = mealDays.Select(md => new MealDayDto
            {
                Date = md.Date,
                Meals = md.Meals.Select(m => m.ToDto(baseUrl)).ToList()
            });

            return Ok(new { mealDays = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeal(int id, [FromBody] MealDto meal)
        {
            var entity = new Meal
            {
                Id = id,
                UserId = UserId.Value,
            };

            try
            {
                await _service.UpdateMealAsync(entity, meal.Type);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeal(int id)
        {
            try
            {
                await _service.DeleteMealAsync(new Meal { Id = id, UserId = UserId.Value });
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpPost("{mealId}/mealfoods")]
        public async Task<IActionResult> CreateMealFood(int mealId, [FromBody] MealFoodDto mealFood)
        {
            var entity = new MealFood
            {
                MealId = mealId,
                Weight = mealFood.Weight,
            };

            if (mealFood.Food.FoodType == FoodType.Food)
            {
                entity.FoodId = mealFood.Food.Id;
            }
            else
            {
                entity.RecipeId = mealFood.Food.Id;
            }

            try
            {
                await _service.CreateMealFoodAsync(UserId.Value, entity);
                return StatusCode(201, new { id = entity.Id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("mealfoods/{mealFoodId}")]
        public async Task<IActionResult> UpdateMealFood(int mealFoodId, [FromBody] MealFoodDto mealFood)
        {
            try
            {
                await _service.UpdateMealFoodAsync(UserId.Value, mealFoodId, mealFood.Weight);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("mealfoods/{mealFoodId}")]
        public async Task<IActionResult> DeleteMealFood(int mealFoodId)
        {
            try
            {
                await _service.DeleteMealFoodAsync(UserId.Value, mealFoodId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
