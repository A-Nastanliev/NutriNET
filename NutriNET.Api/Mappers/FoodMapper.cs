using NutriNET.Api.Dto.Food;
using NutriNET.Data.Models;

namespace NutriNET.Api.Mappers
{
    public static class FoodMapper
    {
        public static FoodDto ToDto(this Food food, string baseUrl)
        {
            return new FoodDto(food.Id, food.Name, food.Brand, food.Barcode, 
                food.Image == null ? null : $"{baseUrl}/{food.Image}",
                food.Calories, food.Proteins, food.Carbohydrates, food.Fats);
        }

        public static FoodRequestDto ToDto(this FoodRequest foodRequest, string baseUrl, bool mapSender = true) 
        {
            return new FoodRequestDto(foodRequest.Id ,foodRequest.Name, foodRequest.Brand, foodRequest.Barcode, foodRequest.DateSent, foodRequest.ActionedOn,
                foodRequest.Status, foodRequest.ActionedBy?.ToPublicDto(baseUrl) ,mapSender ? foodRequest.Sender?.ToPublicDto(baseUrl) : null);
        }

    }
}
