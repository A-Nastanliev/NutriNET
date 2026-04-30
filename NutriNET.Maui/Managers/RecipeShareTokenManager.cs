using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NutriNET.Maui.Managers
{
    public static class RecipeShareTokenManager
    {
        private const string Secret = "N3tR3c!p3$hAr3S3cr3t2025#xZ*****484dhhr";

        public static string Create(int recipeId)
        {
            var hmac = Base64UrlEncode(ComputeHmac($"{recipeId}"));
            var combined = $"{recipeId}:{hmac}";
            return Base64UrlEncode(Encoding.UTF8.GetBytes(combined));
        }

        public static bool Validate(int recipeId, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var expected = Create(recipeId);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(token));
        }

        public static bool TryParseUri(string uriString, out int recipeId, out string token)
        {
            recipeId = 0;
            token = string.Empty;

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
                return false;

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length < 2) return false;
            token = segments[1];
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                var base64 = token.Replace('-', '+').Replace('_', '/');
                var padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                var colon = decoded.IndexOf(':');
                if (colon < 0) return false;
                return int.TryParse(decoded[..colon], out recipeId);
            }
            catch { return false; }
        }

        private static byte[] ComputeHmac(string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(Secret);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            return hmac.ComputeHash(dataBytes);
        }

        private static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes)
                       .TrimEnd('=')
                       .Replace('+', '-')
                       .Replace('/', '_');
    }
}
