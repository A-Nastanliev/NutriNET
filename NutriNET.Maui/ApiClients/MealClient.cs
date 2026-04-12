using NutriNET.Maui.Models.Meal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.ApiClients
{
    public class MealClient
    {
        readonly HttpClient _http;

        public MealClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<RequestResult> CreateMealAsync(MealVM meal)
        {
            var content = new StringContent(JsonSerializer.Serialize((int)meal.Type),Encoding.UTF8,"application/json");

            var response = await _http.PostAsync("/api/meals", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            meal.Id = doc.RootElement.GetProperty("id").GetInt32();
            meal.DateTime = DateTime.UtcNow;

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> GetTodaysMealsAsync(TimeSpan timeOffset, MealDayVM mealDay)
        {
            var url = $"/api/meals/today?timeOffset={Uri.EscapeDataString(timeOffset.ToString())}";
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            mealDay.Meals.Clear();
            foreach (var element in doc.RootElement.GetProperty("meals").EnumerateArray())
            {
                var meal = new MealVM();
                meal.FromJson(element);
                mealDay.Meals.Add(meal);
            }

            mealDay.Date = DateTime.Now.Date;

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, List<MealDayVM>?, DateTime? NextCursor)> GetNextMealDaysAsync(
            int count, DateTime? cursorDate, TimeSpan timeOffset)
        {
            var url = "/api/meals" + PaginationQuery.Build(count, cursorDate, null);
            url += $"&timeOffset={Uri.EscapeDataString(timeOffset.ToString())}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var mealDays = new List<MealDayVM>();

            if (!doc.RootElement.TryGetProperty("mealDays", out var mealDaysJson) ||
            mealDaysJson.ValueKind != JsonValueKind.Array)
            {
                return (new RequestResult(true, null), new List<MealDayVM>(), null);
            }

            foreach (var day in mealDaysJson.EnumerateArray())
            {
                if (!day.TryGetProperty("date", out var dateProp) ||
                    dateProp.ValueKind != JsonValueKind.String)
                    continue;

                if (!dateProp.TryGetDateTime(out var date))
                    continue;

                var meals = new ObservableCollection<MealVM>();

                if (day.TryGetProperty("meals", out var mealsJson) &&
                    mealsJson.ValueKind == JsonValueKind.Array)
                {
                    foreach (var mealJson in mealsJson.EnumerateArray())
                    {
                        var meal = new MealVM();
                        meal.FromJson(mealJson);
                        meals.Add(meal);
                    }
                }

                mealDays.Add(new MealDayVM(date, meals));
            }

            DateTime? nextCursor = mealDays.Count > 0
                ? mealDays[^1].Date
                : null;

            return (new RequestResult(true, null), mealDays, nextCursor);
        }

        public async Task<RequestResult> UpdateMealAsync(MealVM meal)
        {
            var dto = new
            {
                type = (int)meal.Type
            };

            var content = new StringContent(JsonSerializer.Serialize(dto),Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"/api/meals/{meal.Id}", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteMealAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/meals/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> AddMealFoodAsync(int mealId, MealFoodVM mealFood)
        {
            var dto = new
            {
                weight = mealFood.Weight,
                food = new
                {
                    id = mealFood.Food.Id,
                    foodType = mealFood.Food.FoodType
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8,"application/json");

            var response = await _http.PostAsync($"/api/meals/{mealId}/mealfoods", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            mealFood.Id = doc.RootElement.GetProperty("id").GetInt32();

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> UpdateMealFoodAsync(MealFoodVM mealFood, double newWeight)
        {
            var dto = new
            {
                weight = newWeight
            };

            var content = new StringContent(JsonSerializer.Serialize(dto),Encoding.UTF8,"application/json");

            var response = await _http.PutAsync($"/api/meals/mealfoods/{mealFood.Id}", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            mealFood.Weight = newWeight;
            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteMealFoodAsync(int mealFoodId)
        {
            var response = await _http.DeleteAsync($"/api/meals/mealfoods/{mealFoodId}");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }
    }

}
