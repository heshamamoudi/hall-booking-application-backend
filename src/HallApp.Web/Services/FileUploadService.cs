using Microsoft.AspNetCore.Http;

namespace HallApp.Web.Services
{
    public interface IFileUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, string folder);
        Task<List<string>> SaveImagesAsync(List<IFormFile> files, string folder);
        Task<bool> DeleteImageAsync(string filePath);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly string _uploadsPath;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public FileUploadService(string contentRootPath)
        {
            _uploadsPath = Path.Combine(contentRootPath, "wwwroot", "uploads");
            Directory.CreateDirectory(_uploadsPath);
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");

                if (file.Length > _maxFileSize)
                    throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024}MB");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                    throw new ArgumentException($"File type {extension} not allowed");

                var folderPath = Path.Combine(_uploadsPath, folder);
                Directory.CreateDirectory(folderPath);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save image: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> SaveImagesAsync(List<IFormFile> files, string folder)
        {
            var uploadedPaths = new List<string>();

            try
            {
                foreach (var file in files)
                {
                    var path = await SaveImageAsync(file, folder);
                    uploadedPaths.Add(path);
                }

                return uploadedPaths;
            }
            catch
            {
                foreach (var path in uploadedPaths)
                {
                    await DeleteImageAsync(path);
                }
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var normalizedPath = filePath.Replace("/uploads/", "");
                var fullPath = Path.Combine(_uploadsPath, normalizedPath);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
