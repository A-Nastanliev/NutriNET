using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NutriNET.Maui.Managers
{
    public static class ImageManager
    {
        private static readonly string TempFolder = Path.Combine(FileSystem.AppDataDirectory, "TempImages");

        static ImageManager()
        {
            if (!Directory.Exists(TempFolder))
                Directory.CreateDirectory(TempFolder);
        }

        public static void CleanupTempImage(string imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    File.Delete(imagePath);
                }
                catch
                {

                }
            }
        }

        public static async Task<string> SaveTempImageAsync(Stream sourceStream, string extension = ".jpg")
        {
            if (sourceStream == null)
                throw new ArgumentNullException(nameof(sourceStream));

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(TempFolder, fileName);

            using var fileStream = File.Create(filePath);
            await sourceStream.CopyToAsync(fileStream);

            return filePath;
        }


        public static void CleanupAllTempImages()
        {
            try
            {
                if (!Directory.Exists(TempFolder))
                    return;

                var files = Directory.GetFiles(TempFolder);
                Debug.WriteLine($"[ImageManager] Deleting {files.Length} temp image(s).");

                foreach (var file in Directory.GetFiles(TempFolder))
                {
                    Debug.WriteLine($"[ImageManager] Deleting file: {file}");
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    { 
                    }
                }
            }
            catch
            {
            }
        }
    }
}
