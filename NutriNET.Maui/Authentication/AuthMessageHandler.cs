using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using NutriNET.Maui.ApiClients;

namespace NutriNET.Maui.Authentication
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly ITokenStore _tokenStore;
        private readonly RefreshClient _refreshClient;
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        public AuthMessageHandler(ITokenStore tokenStore, RefreshClient refreshClient)
        {
            _tokenStore = tokenStore;
            _refreshClient = refreshClient;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath?.ToLower();
            if (path != null && (
                path.Contains("/api/users/email_login") ||
                path.Contains("/api/users/signup") ||
                path.Contains("/api/users/refresh")))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            if (request.Headers.Contains("X-Retry"))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var token = await _tokenStore.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync();
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                return response;

            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                var currentToken = await _tokenStore.GetAccessTokenAsync();
                if (!string.IsNullOrWhiteSpace(currentToken) && currentToken != token)
                {
                    var retry = await CloneRequest(request);
                    retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                    retry.Headers.Add("X-Retry", "true");

                    return await base.SendAsync(retry, cancellationToken);
                }

                var refreshed = await TryRefreshAsync(cancellationToken);
                if (!refreshed)
                {
                    return response;
                }

                var newToken = await _tokenStore.GetAccessTokenAsync();
                if (string.IsNullOrWhiteSpace(newToken))
                {
                    return response;
                }

                var retryRequest = await CloneRequest(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                retryRequest.Headers.Add("X-Retry", "true");

                return await base.SendAsync(retryRequest, cancellationToken);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<bool> TryRefreshAsync(CancellationToken ct)
        {
            var refreshToken = await _tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            try
            {
                var result = await _refreshClient.RefreshAsync(refreshToken, ct);
                if (result == null) return false;

                await _tokenStore.SetAccessTokenAsync(result.Value.accessToken);
                await _tokenStore.SetRefreshTokenAsync(result.Value.refreshToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthMessageHandler] Refresh failed: {ex}");
                return false;
            }
        }

        private static async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (req.Content != null)
            {
                var bytes = await req.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(bytes);

                foreach (var header in req.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }

}
