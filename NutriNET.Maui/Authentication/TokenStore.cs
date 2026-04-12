using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NutriNET.Maui.Authentication
{
    public class TokenStore : ITokenStore
    {
        private const string AccessTokenKey = "access_token";
        private const string RefreshTokenKey = "refresh_token";

        public async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(AccessTokenKey);
            }
            catch
            {
                return null;
            }
        }

        public async Task SetAccessTokenAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Clear();
                return;
            }

            try
            {
                await SecureStorage.SetAsync(AccessTokenKey, token);
            }
            catch(Exception ex) 
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            try { return await SecureStorage.GetAsync(RefreshTokenKey); }
            catch { return null; }
        }

        public async Task SetRefreshTokenAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) 
            { 
                SecureStorage.Remove(RefreshTokenKey); 
                return; 
            }
            try 
            { 
                await SecureStorage.SetAsync(RefreshTokenKey, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Clear()
        {
            SecureStorage.Remove(AccessTokenKey);
            SecureStorage.Remove(RefreshTokenKey);
        }
    }
}
