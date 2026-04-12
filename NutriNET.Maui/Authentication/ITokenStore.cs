namespace NutriNET.Maui.Authentication
{
    public interface ITokenStore
    {
        Task<string?> GetAccessTokenAsync();
        Task SetAccessTokenAsync(string? token);
        Task<string?> GetRefreshTokenAsync();
        Task SetRefreshTokenAsync(string? token);
        void Clear();
    }
}
