using NutriNET.Data.Enums;
using NutriNET.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriNET.Services;
using System.Security.Claims;
using NutriNET.Api.Dto;
using NutriNET.Api.Dto.Food;
using NutriNET.Api.Mappers;
using NutriNET.Api.Services;

namespace NutriNET.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/foods")]
    public class FoodController : ControllerBase
    {
        private readonly FoodService _service;

        private int? UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        private IConfiguration _configuration;

        private IImageStorageService _imageStorageService;

        public FoodController(FoodService service, IConfiguration configuration, IImageStorageService imageStorageService)
        {
            _service = service;
            _configuration = configuration;
            _imageStorageService = imageStorageService;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOrModerator")]
        public async Task<IActionResult> CreateFood([FromForm] FoodFormDto dto)
        {

            string imagePath = null;

            try
            {
                imagePath = await _imageStorageService.SaveImageAsync(dto.Image, "food-images");
            }
            catch (InvalidOperationException e)
            {
                return BadRequest();
            }

            try
            {
                Food food = new Food(dto.Name, dto.ExtraInfo, dto.Barcode, imagePath, dto.Calories, dto.Proteins, dto.Carbohydrates, dto.Fats);
                var (success, error) = await _service.CreateFoodAsync(food);

                if (!success)
                {
                    _imageStorageService.DeleteImage(food.Image);
                    return BadRequest(error);
                }

                return StatusCode(201, new { id = food.Id });
            }
            catch (DbUpdateException ex)
            {
                _imageStorageService.DeleteImage(imagePath);
                return Conflict("FoodBarcodeExists");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNextFoods([FromQuery] CursorDto load, [FromQuery] string? search)
        {
            List<Food> foods = await _service.GetNextFoodsAsync(load.Count, load.CursorDate, load.CursorId, search);
            var baseUrl = _configuration["App:BaseUrl"];
            var list = foods.Select(f => f.ToDto(baseUrl)).ToList();
            var cursorDate = foods.LastOrDefault()?.DateAdded;
            return Ok(new
            {
                foods = list,
                cursorDate
            });
        }

        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> GetFoodByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return BadRequest();

            barcode = barcode?.Trim();
            var food = await _service.GetFoodByBarcodeAsync(barcode);
            if (food == null)
                return NotFound("BarcodeNotFound");

            var baseUrl = _configuration["App:BaseUrl"];
            return Ok(food.ToDto(baseUrl));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOrModerator")]
        public async Task<IActionResult> UpdateFood(int id, [FromForm] FoodFormDto foodDto)
        {
            var existingFood = await _service.GetFoodAsync(id);
            if (existingFood == null)
                return NotFound();

            Food foodEntity = new Food(id, foodDto.Name, foodDto.ExtraInfo, foodDto.Barcode, foodDto.Calories, foodDto.Proteins, foodDto.Carbohydrates, foodDto.Fats);

            string? oldImagePath = existingFood.Image;
            string? newImagePath = null;
            bool imageUpdated = false;

            if (foodDto.Image != null && foodDto.Image.Length > 0)
            {
                try
                {
                    newImagePath = await _imageStorageService.SaveImageAsync(foodDto.Image, "food-images");

                    foodEntity.Image = newImagePath;
                    imageUpdated = true;
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { error = ex.Message });
                }
            }

            try
            {
                var (success, error) = await _service.UpdateFoodAsync(foodEntity, imageUpdated);
                if (!success)
                {
                    if (imageUpdated && !string.IsNullOrWhiteSpace(newImagePath))
                    {
                        _imageStorageService.DeleteImage(newImagePath);
                    }
                    return BadRequest(error);
                }

                if (imageUpdated && !string.IsNullOrWhiteSpace(oldImagePath))
                {
                    _imageStorageService.DeleteImage(oldImagePath);
                    var baseUrl = _configuration["App:BaseUrl"];
                    return Ok(new { image = $"{baseUrl}/{newImagePath}" });
                }

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _imageStorageService.DeleteImage(newImagePath);
                return Conflict("FoodBarcodeExists");
            }

        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteFood(int id)
        {
            var food = await _service.GetFoodAsync(id);
            if (food == null)
                return NotFound("FoodNotFound");

            string path = food.Image;

            try
            {
                await _service.DeleteFoodAsync(id);

                _imageStorageService.DeleteImage(path);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("requests")]
        public async Task<IActionResult> CreateFoodRequest([FromBody] FoodRequestDto foodRequest)
        {
            try
            {
                FoodRequest foodEntity = new FoodRequest(foodRequest.Name, foodRequest.Barcode, foodRequest.Brand, UserId.Value);
                await _service.CreateFoodRequestAsync(foodEntity);

                return StatusCode(201, new { id = foodEntity.Id });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("me/requests")]
        public async Task<IActionResult> GetMyPendingFoodRequests()
        {
            var foodRequests = await _service.GetMyPendingFoodRequestsAsync(UserId.Value);
            var baseUrl = _configuration["App:BaseUrl"];
            return Ok(foodRequests.Select(f => f.ToDto(baseUrl)));
        }

        [HttpPut("requests/{id}")]
        [Authorize(Policy = "AdminOrModerator")]
        public async Task<IActionResult> UpdateFoodRequest(int id, [FromBody] FoodRequestDto foodRequest)
        {
            try
            {
                await _service.UpdateFoodRequestAsync(new FoodRequest(id, foodRequest.Status, UserId.Value));
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("FoodRequestNotFound");
            }
            catch (InvalidOperationException)
            {
                return Conflict("FoodRequestDeniedBarcodeExists");
            }
        }

        [HttpGet("requests")]
        [Authorize(Policy = "AdminOrModerator")]
        public async Task<IActionResult> GetNextFoodRequests([FromQuery] CursorDto load, [FromQuery] RequestStatus status)
        {
            var foodRequests = await _service.GetNextFoodRequestsAsync(load.Count, load.CursorDate, load.CursorId, status);
            var baseUrl = _configuration["App:BaseUrl"];
            return Ok(foodRequests.Select(f => f.ToDto(baseUrl)));
        }
    }
}
