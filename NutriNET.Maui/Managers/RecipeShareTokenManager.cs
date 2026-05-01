using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NutriNET.Maui.Managers
{
    public static class RecipeShareTokenManager
    {
        public static bool TryParseToken(string uriString, out string token)
        {
            token = string.Empty;
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) return false;
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length < 2) return false;
            token = segments[1];
            return !string.IsNullOrWhiteSpace(token);
        }
    }
}
