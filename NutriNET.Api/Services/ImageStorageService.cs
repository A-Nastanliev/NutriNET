using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace NutriNET.Api.Services
{
    public class ImageStorageService : IImageStorageService
    {
        private static readonly string[] AllowedTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly string _wwwRoot;

        public ImageStorageService(IWebHostEnvironment env)
        {
            _wwwRoot = env.WebRootPath;
        }

        public async Task<string?> SaveImageAsync(IFormFile file, string folder, int maxWidth = 1024, int maxHeight = 1024)
        {
            if (file == null || file.Length == 0)
                return null;

            if (!AllowedTypes.Contains(file.ContentType))
                throw new InvalidOperationException("Invalid image type.");

            var directory = Path.Combine(_wwwRoot, folder);
            Directory.CreateDirectory(directory);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(directory, fileName);

            using var image = await Image.LoadAsync(file.OpenReadStream());

            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth, maxHeight)
                }));
            }

            await image.SaveAsync(fullPath);

            return $"{folder}/{fileName}";
        }

        public void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var fullPath = Path.Combine(_wwwRoot, relativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
