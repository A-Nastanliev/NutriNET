using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.ApiClients
{
    public class RefreshClient
    {
        private readonly HttpClient _httpClient;

        public RefreshClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string accessToken, string refreshToken)?> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var payload = JsonSerializer.Serialize(new { RefreshToken = refreshToken });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/users/refresh", content, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return (
                root.GetProperty("accessToken").GetString()!,
                root.GetProperty("refreshToken").GetString()!
            );
        }
    }
}
