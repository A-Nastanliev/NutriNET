namespace NutriNET.Api.Dto.Meal
{
    public class MealDayDto
    {
        public DateTime Date { get; set; }
        public List<MealDto> Meals { get; set; } = new();
    }
}
