using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NutriNET.Maui.ApiClients
{
    public static class ApiErrorParser
    {
        private static Func<Task>? _onLogout;
        private static Func<Task>? _onForbidden;

        public static void Initialize(Func<Task> onLogout, Func<Task> onForbidden)
        {
            _onLogout = onLogout;
            _onForbidden = onForbidden;
        }

        public static async Task<string> ParseAsync(HttpResponseMessage response, bool handleUnauthorized = true)
        {
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                return "InternalServerError";
            }
            
            var content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && handleUnauthorized)
            {
                if (_onLogout != null)
                {
                    await _onLogout();
                }

                return "SessionExpired";
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                if (_onForbidden != null)
                    await _onForbidden();

                return "ForbiddenAction";
            }

            if (string.IsNullOrWhiteSpace(content))
                return "GenericErrorMessage";

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var errorProp))
                {
                    var errorStr = errorProp.GetString();
                    return string.IsNullOrWhiteSpace(errorStr) ? "GenericErrorMessage" : errorStr;
                }

                if (root.ValueKind == JsonValueKind.String)
                {
                    var errorStr = root.GetString();
                    return string.IsNullOrWhiteSpace(errorStr) ? "GenericErrorMessage" : errorStr;
                }

                return "GenericErrorMessage";
            }
            catch
            {
                return "GenericErrorMessage";
            }
        }
    }
}
