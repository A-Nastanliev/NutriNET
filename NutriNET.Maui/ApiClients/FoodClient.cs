using NutriNET.Maui.Models;
using NutriNET.Maui.Models.Food;
using NutriNET.Maui.Models.User;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.ApiClients
{
    public class FoodClient
    {
        readonly HttpClient _http;
        static readonly CultureInfo ApiCulture = CultureInfo.InvariantCulture;
        readonly UserVM _user;

        public FoodClient(HttpClient http, UserVM user) 
        {
            _http = http;
            _user = user;
        }

        public async Task<RequestResult> CreateFoodAsync(FoodVM food, string imagePath = null)
        {
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(food.Name ?? ""), "Name");
            content.Add(new StringContent(food.ExtraInfo ?? ""), "ExtraInfo");
            content.Add(new StringContent(food.Barcode ?? ""), "Barcode");

            content.Add(new StringContent(food.Calories.ToString(ApiCulture)), "Calories");
            content.Add(new StringContent(food.Proteins.ToString(ApiCulture)), "Proteins");
            content.Add(new StringContent(food.Carbohydrates.ToString(ApiCulture)), "Carbohydrates");
            content.Add(new StringContent(food.Fats.ToString(ApiCulture)), "Fats");

            if (!string.IsNullOrEmpty(imagePath))
            {
                var stream = File.OpenRead(imagePath);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                content.Add(streamContent, "Image", Path.GetFileName(imagePath));
            }

            var response = await _http.PostAsync("/api/foods", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            int id = doc.RootElement.GetProperty("id").GetInt32();
            food.Id = id;
            return new RequestResult(true, null);

        }

        public async Task<(RequestResult, List<FoodVM>? Foods, DateTime? LastCreatedAt)> 
            GetNextFoodsAsync(int count, DateTime? cursorDate, int? cursorId, string search)
        {
            var url = "/api/foods" + PaginationQuery.Build(count, cursorDate, cursorId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var separator = url.Contains("?") ? "&" : "?";
                url += $"{separator}search={Uri.EscapeDataString(search)}";
            }

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return new (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var foods = new List<FoodVM>();
            var root = doc.RootElement;

            foreach (var element in root.GetProperty("foods").EnumerateArray())
            {
                var food = new FoodVM();
                food.FromJson(element);
                foods.Add(food);
            }

            DateTime? lastCreatedAt = null;

            if (root.TryGetProperty("cursorDate", out var cursorProp) && cursorProp.ValueKind != JsonValueKind.Null)
            {
                lastCreatedAt = cursorProp.GetDateTime();
            }

            return (new RequestResult(true, null), foods, lastCreatedAt);
        }


        public async Task<(RequestResult, FoodVM? Food)> GetFoodByBarcodeAsync(string barcode)
        {
            var response = await _http.GetAsync($"/api/foods/barcode/{Uri.EscapeDataString(barcode)}");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (new RequestResult(true, null), null);

            if (!response.IsSuccessStatusCode)
            {
                return new(new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var food = new FoodVM();
            food.FromJson(doc.RootElement);

            return (new RequestResult(true, null), food);
        }


        public async Task<RequestResult> UpdateFoodAsync(FoodVM food, string imagePath = null)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(food.Name ?? ""), "Name");
            content.Add(new StringContent(food.ExtraInfo ?? ""), "ExtraInfo");
            content.Add(new StringContent(food.Barcode ?? ""), "Barcode");
            content.Add(new StringContent(food.Calories.ToString(ApiCulture)), "Calories");
            content.Add(new StringContent(food.Proteins.ToString(ApiCulture)), "Proteins");
            content.Add(new StringContent(food.Carbohydrates.ToString(ApiCulture)), "Carbohydrates");
            content.Add(new StringContent(food.Fats.ToString(ApiCulture)), "Fats");
            bool hasImage = !string.IsNullOrEmpty(imagePath);
            if (hasImage)
            {
                var stream = File.OpenRead(imagePath);
                var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                content.Add(streamContent, "Image", Path.GetFileName(imagePath));
            }

            var response = await _http.PutAsync($"/api/foods/{food.Id}", content);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            if (hasImage && response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                string imageUrl = doc.RootElement.GetProperty("image").GetString();

                food.Image = imageUrl;
                food.ImageSource = ImageSource.FromUri(new Uri(imageUrl));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> DeleteFoodAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/foods/{id}"); 

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> CreateFoodRequestAsync(FoodRequestVM request)
        {
            var dto = new
            {
                name = request.Name,
                brand = request.Brand,
                barcode = request.Barcode,
                status = (int)request.Status
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/api/foods/requests", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            int id = doc.RootElement.GetProperty("id").GetInt32();
            request.Id = id;

            return new RequestResult(true, null);
        }

        public async Task<RequestResult> GetMyPendingFoodRequestsAsync()
        {
            var response = await _http.GetAsync("/api/foods/me/requests");

            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            _user.PendingFoodRequests.Clear();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var request = new FoodRequestVM(_user.PublicUser);
                request.FromJson(element);
                _user.PendingFoodRequests.Add(request);
            }

            return new RequestResult(true, null);
        }

        public async Task<(RequestResult, List<FoodRequestVM>? Requests)>
            GetNextFoodRequestsAsync(int count, DateTime? cursorDate, int? cursorId, RequestStatus status)
        {
            var query = PaginationQuery.Build(count, cursorDate, cursorId);
            var url = $"/api/foods/requests{query}&status={status}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return (new RequestResult(false, await ApiErrorParser.ParseAsync(response)), null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var list = new List<FoodRequestVM>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var vm = new FoodRequestVM();
                vm.FromJson(element);
                list.Add(vm);
            }

            return (new RequestResult(true, null), list);
        }

        public async Task<RequestResult> UpdateFoodRequestStatusAsync(int id, RequestStatus status)
        {
            var dto = new
            {
                status = (int)status
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

            var response = await _http.PutAsync($"/api/foods/requests/{id}", jsonContent);

            if(response.StatusCode == HttpStatusCode.NotFound ||  response.StatusCode == HttpStatusCode.Conflict)
            {
                return new RequestResult(true, await ApiErrorParser.ParseAsync(response));
            }
            if (!response.IsSuccessStatusCode)
            {
                return new RequestResult(false, await ApiErrorParser.ParseAsync(response));
            }

            return new RequestResult(true, null);
        }

    }
}
