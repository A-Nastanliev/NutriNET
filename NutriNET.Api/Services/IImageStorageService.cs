namespace NutriNET.Api.Services
{
    public interface IImageStorageService
    {
        Task<string?> SaveImageAsync(IFormFile file, string folder, int maxWidth = 1024, int maxHeight = 1024);
        void DeleteImage(string? relativePath);
    }
}
