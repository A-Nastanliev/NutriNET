using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.Recipes;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.ApiClients
{
    public class RecipeClient
    {
        readonly HttpClient _http;

        static readonly CultureInfo ApiCulture = CultureInfo.InvariantCulture;

        UserVM _user;

        public RecipeClient(HttpClient http, UserVM user)
        {
            _http = http;
            _user= user;
        }

        public async Task<RequestResult> CreateRecipeAsync(RecipeVM recipe, string? imagePath = null)
        {
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(recipe.Name ?? ""), "Name");
            content.Add(new StringContent(((int)recipe.PrivacyLevel).ToString()), "PrivacyLevel");
            content.Add(new StringContent(recipe.Description ?? ""), "Description");

            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var ingredient = recipe.Ingredients[i];
                content.Add(new StringContent(ingredient.Food.Id.ToString()), $"Ingredients[{i}].FoodId");
                content.Add(new StringContent(ingredient.Weight.ToString(ApiCulture)), $"Ingredients[{i}].Weight");
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                var stream = File.OpenRead(imagePath);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "Image", Path.GetFileName(imagePath));
            }

            var response = await _http.PostAsync("/api/recipes", content);

            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            recipe.Id = doc.RootElement.GetProperty("recipeId").GetInt32();

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, RecipeVM?, int, double, int?)> GetRecipeDetailsAsync(int id)
        {
            var response = await _http.GetAsync($"/api/recipes/{id}");
            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, 0, 0, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recipe = new RecipeVM();
            recipe.FromJson(root.GetProperty("recipe"));

            var ratingCount = root.GetProperty("ratingCount").GetInt32();
            var ratingAverage = root.GetProperty("ratingAverage").GetDouble();
            int? userRating = root.TryGetProperty("userRating", out var ur) && ur.ValueKind != JsonValueKind.Null
                ? ur.GetInt32() : null;

            return (new RequestResult(true, null), recipe, ratingCount, ratingAverage, userRating);
        }

        public async Task<(RequestResult, List<FoodVM>?, DateTime? nextCursorDate, int? nextCursorId)>
            GetNextPublicRecipesAsync(int count, DateTime? cursorDate, int? cursorId, string search)
        {
            var url = "/api/recipes/public" + PaginationQuery.Build(count, cursorDate, cursorId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var separator = url.Contains("?") ? "&" : "?";
                url += $"{separator}search={Uri.EscapeDataString(search)}";
            }
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recipes = new List<FoodVM>();
            foreach (var element in root.GetProperty("recipes").EnumerateArray())
            {
                var food = new FoodVM();
                food.FromJson(element);
                recipes.Add(food);
            }

            DateTime? nextDate = root.TryGetProperty("nextCursorDate", out var dateProp) && dateProp.ValueKind != JsonValueKind.Null
                ? dateProp.GetDateTime()
                : null;

            int? nextId = root.TryGetProperty("nextCursorId", out var idProp) && idProp.ValueKind != JsonValueKind.Null
                ? idProp.GetInt32()
                : null;

            return (new RequestResult(true, null), recipes, nextDate, nextId);
        }

        public async Task<(RequestResult, List<FoodVM>?, DateTime? nextCursorDate, int? nextCursorId)>
            GetNextFollowingRecipesAsync(int count, DateTime? cursorDate, int? cursorId, string search)
        {
            var url = "/api/recipes/following" + PaginationQuery.Build(count, cursorDate, cursorId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var separator = url.Contains("?") ? "&" : "?";
                url += $"{separator}search={Uri.EscapeDataString(search)}";
            }
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recipes = new List<FoodVM>();
            foreach (var element in root.GetProperty("recipes").EnumerateArray())
            {
                var food = new FoodVM();
                food.FromJson(element);
                recipes.Add(food);
            }

            DateTime? nextDate = root.TryGetProperty("nextCursorDate", out var dateProp) && dateProp.ValueKind != JsonValueKind.Null
                ? dateProp.GetDateTime()
                : null;

            int? nextId = root.TryGetProperty("nextCursorId", out var idProp) && idProp.ValueKind != JsonValueKind.Null
                ? idProp.GetInt32()
                : null;

            return (new RequestResult(true, null), recipes, nextDate, nextId);
        }

        public async Task<(RequestResult, List<FoodVM>?, DateTime? nextCursorDate, int? nextCursorId)>
            GetUserRecipesAsync(int creatorId, int count, DateTime? cursorDate, int? cursorId)
        {
            var url = $"/api/recipes/user/{creatorId}" +
                      PaginationQuery.Build(count, cursorDate, cursorId);

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recipes = new List<FoodVM>();
            foreach (var element in root.GetProperty("recipes").EnumerateArray())
            {
                var food = new FoodVM();
                food.FromJson(element);
                recipes.Add(food);
            }

            DateTime? nextDate = root.TryGetProperty("nextCursorDate", out var dateProp) && dateProp.ValueKind != JsonValueKind.Null
                ? dateProp.GetDateTime()
                : null;

            int? nextId = root.TryGetProperty("nextCursorId", out var idProp) && idProp.ValueKind != JsonValueKind.Null
                ? idProp.GetInt32()
                : null;

            return (new RequestResult(true, null), recipes, nextDate, nextId);
        }

        public async Task<(RequestResult, List<FoodVM>?, DateTime? nextCursorDate, int? nextCursorId)>
            GetMyRecipesAsync(int count, DateTime? nextDate, int? nextId)
        {
            return await GetUserRecipesAsync(_user.PublicUser.Id,count, nextDate, nextId);
        }

        public async Task<RequestResult> UpdateRecipeAsync(RecipeVM recipe, string? imagePath = null)
        {
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(recipe.Name ?? ""), "Name");
            content.Add(new StringContent(((int)recipe.PrivacyLevel).ToString()), "PrivacyLevel");
            content.Add(new StringContent(recipe.Description ?? ""), "Description");

            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var ingredient = recipe.Ingredients[i];
                content.Add(new StringContent(ingredient.Food.Id.ToString()), $"Ingredients[{i}].FoodId");
                content.Add(new StringContent(ingredient.Weight.ToString(ApiCulture)), $"Ingredients[{i}].Weight");
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                var stream = File.OpenRead(imagePath);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "Image", Path.GetFileName(imagePath));
            }

            var response = await _http.PutAsync($"/api/recipes/{recipe.Id}", content);
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteRecipeAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/recipes/{id}");
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> CreateRecipeRatingAsync(int recipeId, int rating)
        {
            var response = await _http.PostAsJsonAsync($"/api/recipes/{recipeId}/rating", rating);
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> UpdateRecipeRatingAsync(int recipeId, int rating)
        {
            var response = await _http.PutAsJsonAsync($"/api/recipes/{recipeId}/rating", rating);
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteRecipeRatingAsync(int recipeId)
        {
            var response = await _http.DeleteAsync($"/api/recipes/{recipeId}/rating");
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, int?)> CreateRecipeCommentAsync(int recipeId, string comment)
        {
            var response = await _http.PostAsJsonAsync($"/api/recipes/{recipeId}/comments", comment);
            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null);

            var id = await response.Content.ReadFromJsonAsync<int>();
            return (new RequestResult(true, null), id);
        }

        public async Task<(RequestResult, List<RecipeCommentVM>?, DateTime? nextCursorDate, int? nextCursorId)>
            GetNextRecipeCommentsAsync(int recipeId, int count, DateTime? cursorDate, int? cursorId)
        {
            var url = $"/api/recipes/{recipeId}/comments" + PaginationQuery.Build(count, cursorDate, cursorId);
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var comments = new List<RecipeCommentVM>();
            foreach (var element in root.EnumerateArray())
            {
                var vm = new RecipeCommentVM();
                vm.FromJson(element);
                comments.Add(vm);
            }

            return (new RequestResult(true, null), comments, comments.LastOrDefault()?.Date, comments.LastOrDefault()?.Id);
        }

        public async Task<RequestResult> UpdateRecipeCommentAsync(int commentId, string comment)
        {
            var response = await _http.PutAsJsonAsync($"/api/recipes/comments/{commentId}", comment);
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteOwnRecipeCommentAsync(int commentId)
        {
            var response = await _http.DeleteAsync($"/api/recipes/comments/{commentId}/self");
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteRecipeCommentAsync(int commentId)
        {
            var response = await _http.DeleteAsync($"/api/recipes/comments/{commentId}");
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> CreateRecipeListAsync(RecipeListVM recipeList)
        {
            var response = await _http.PostAsJsonAsync("/api/recipes/lists", new { Name = recipeList.Name });
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            recipeList.Id = doc.RootElement.GetProperty("id").GetInt32();

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, List<RecipeListVM>?)> GetAllRecipeListsAsync()
        {
            var response = await _http.GetAsync("/api/recipes/lists");
            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var lists = new List<RecipeListVM>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var vm = new RecipeListVM();
                vm.FromJson(element);
                lists.Add(vm);
            }

            return (new RequestResult(true, null), lists);
        }

        public async Task<RequestResult> UpdateRecipeListAsync(RecipeListVM list)
        {
            var response = await _http.PutAsJsonAsync("/api/recipes/lists", new { list.Id, list.Name });
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteRecipeListAsync(int listId)
        {
            var response = await _http.DeleteAsync($"/api/recipes/lists/{listId}");
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> CreateRecipeListItemAsync(int listId, int recipeId)
        {
            var response = await _http.PostAsJsonAsync("/api/recipes/lists/items", new { ListId = listId, RecipeId = recipeId });
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, List<FoodVM>?, DateTime? nextCursorDate, int? nextCursorId)>
            GetNextRecipesInListAsync(int listId, int count, DateTime? cursorDate, int? cursorId)
        {
            var url = $"/api/recipes/lists/{listId}/recipes" + PaginationQuery.Build(count, cursorDate, cursorId);
            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recipes = new List<FoodVM>();
            foreach (var element in root.GetProperty("recipes").EnumerateArray())
            {
                var vm = new FoodVM();
                vm.FromJson(element);
                recipes.Add(vm);
            }

            int? nextId = root.TryGetProperty("nextCursorId", out var idProp) && idProp.ValueKind != JsonValueKind.Null
                ? idProp.GetInt32() : null;

            return (new RequestResult(true, null), recipes, null, nextId);
        }

        public async Task<RequestResult> DeleteRecipeListItemAsync(int listId, int recipeId)
        {
            var response = await _http.DeleteAsync($"/api/recipes/lists/{listId}/items/{recipeId}");
            if (!response.IsSuccessStatusCode)
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));

            return new RequestResult(true, null);
        }
    }
}