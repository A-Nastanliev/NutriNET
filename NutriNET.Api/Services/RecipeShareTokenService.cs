using System.Security.Cryptography;
using System.Text;

namespace NutriNET.Api.Services
{
    public static class RecipeShareTokenService
    {
        public static string Create(int recipeId, string secret)
        {
            var hmac = ComputeHmac(recipeId.ToString(), secret);
            var combined = $"{recipeId}:{Base64UrlEncode(hmac)}";
            return Base64UrlEncode(Encoding.UTF8.GetBytes(combined));
        }

        public static bool TryValidate(string token, string secret, out int recipeId)
        {
            recipeId = 0;
            try
            {
                var decoded = Encoding.UTF8.GetString(Base64UrlDecode(token));
                var colon = decoded.IndexOf(':');
                if (colon < 0) return false;
                if (!int.TryParse(decoded[..colon], out recipeId)) return false;

                var expected = Create(recipeId, secret);
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(token));
            }
            catch { return false; }
        }

        private static byte[] ComputeHmac(string data, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        private static string Base64UrlEncode(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string s)
        {
            var b = s.Replace('-', '+').Replace('_', '/');
            b = b.PadRight(b.Length + (4 - b.Length % 4) % 4, '=');
            return Convert.FromBase64String(b);
        }
    }
}
