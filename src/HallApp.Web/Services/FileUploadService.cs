using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HallApp.Web.Services
{
    /// <summary>
    /// Where an uploaded file belongs. Files are filed by what owns them so that
    /// everything belonging to one hall or one vendor lives together and can be
    /// listed, moved or deleted as a unit.
    /// </summary>
    public static class UploadCategories
    {
        public const string Halls = "halls";
        public const string Vendors = "vendors";
        public const string ServiceItems = "service-items";
        public const string Avatars = "avatars";
        public const string VendorDocuments = "vendor-documents";
    }

    public interface IFileUploadService
    {
        /// <summary>
        /// Saves one image under {category}/{ownerId}/ and returns its public URL.
        /// </summary>
        Task<string> SaveImageAsync(IFormFile file, string category, int ownerId);

        /// <summary>
        /// Saves several images under {category}/{ownerId}/. If any one fails the
        /// already-written files are removed, so a partial upload never survives.
        /// </summary>
        Task<List<string>> SaveImagesAsync(List<IFormFile> files, string category, int ownerId);

        /// <summary>
        /// Saves a supporting document under {category}/{ownerId}/. Separate from
        /// SaveImageAsync because papers are usually PDFs and are allowed to be
        /// larger than a photograph.
        /// </summary>
        Task<string> SaveDocumentAsync(IFormFile file, string category, int ownerId);

        /// <summary>Deletes a file by the public URL previously returned.</summary>
        Task<bool> DeleteImageAsync(string filePath);

        /// <summary>
        /// Deletes everything belonging to one owner, e.g. every image of a deleted
        /// vendor. Returns the number of files removed.
        /// </summary>
        Task<int> DeleteOwnerFilesAsync(string category, int ownerId);

        /// <summary>Public URLs of every file currently stored for one owner.</summary>
        Task<List<string>> ListOwnerFilesAsync(string category, int ownerId);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly string _uploadsPath;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // Legal papers arrive as PDFs or scans, and scans of a stamped certificate
        // run larger than a listing photograph.
        private readonly long _maxDocumentSize = 10 * 1024 * 1024; // 10MB
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

        /// <summary>
        /// Categories a caller is allowed to write to. Anything else is rejected
        /// rather than quietly creating a new top-level directory.
        /// </summary>
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            UploadCategories.Halls,
            UploadCategories.Vendors,
            UploadCategories.ServiceItems,
            UploadCategories.Avatars,
            UploadCategories.VendorDocuments,
        };

        /// <summary>
        /// Resolves where uploaded files live. Defaults to {contentRoot}/wwwroot/uploads,
        /// but Uploads:Path (UPLOADS__PATH) points it at the mounted volume in the
        /// container - required because the root filesystem is read-only and because
        /// anything written outside a volume is lost on the next deploy.
        /// </summary>
        public static string ResolveUploadsPath(IConfiguration configuration, string contentRootPath)
        {
            var configured = configuration["Uploads:Path"];
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(contentRootPath, "wwwroot", "uploads")
                : configured;
        }

        /// <summary>
        /// Creates a directory when possible. A read-only filesystem must not stop the
        /// app from starting - uploads then fail per-request instead of at boot.
        /// </summary>
        public static bool TryEnsureDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        public FileUploadService(string uploadsPath)
        {
            _uploadsPath = uploadsPath;
            TryEnsureDirectory(_uploadsPath);
        }

        public Task<string> SaveImageAsync(IFormFile file, string category, int ownerId) =>
            SaveFileAsync(file, category, ownerId, _allowedExtensions, _maxFileSize);

        /// <summary>
        /// The one place a file is written. Extension and size limits are passed in
        /// so images and documents can differ without duplicating the path building,
        /// the directory guard or the GUID naming.
        /// </summary>
        private async Task<string> SaveFileAsync(
            IFormFile file, string category, int ownerId, string[] allowedExtensions, long maxBytes)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            if (file.Length > maxBytes)
                throw new ArgumentException($"File size exceeds maximum allowed size of {maxBytes / 1024 / 1024}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"File type {extension} not allowed");

            var relativeDirectory = BuildRelativeDirectory(category, ownerId);
            var folderPath = Path.Combine(_uploadsPath, relativeDirectory);

            if (!TryEnsureDirectory(folderPath))
                throw new IOException($"Upload directory is not writable: {folderPath}");

            // The stored name is a GUID, never the client-supplied one: the original
            // can collide, can carry a path, and can carry a second extension.
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{relativeDirectory.Replace(Path.DirectorySeparatorChar, '/')}/{fileName}";
        }

        public Task<string> SaveDocumentAsync(IFormFile file, string category, int ownerId) =>
            SaveFileAsync(file, category, ownerId, _allowedDocumentExtensions, _maxDocumentSize);

        public async Task<List<string>> SaveImagesAsync(List<IFormFile> files, string category, int ownerId)
        {
            var uploadedPaths = new List<string>();

            try
            {
                foreach (var file in files)
                {
                    uploadedPaths.Add(await SaveImageAsync(file, category, ownerId));
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
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                var fullPath = ResolveStoredPath(filePath);
                if (fullPath == null || !File.Exists(fullPath))
                    return false;

                await Task.Run(() => File.Delete(fullPath));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> DeleteOwnerFilesAsync(string category, int ownerId)
        {
            try
            {
                var folderPath = Path.Combine(_uploadsPath, BuildRelativeDirectory(category, ownerId));
                if (!Directory.Exists(folderPath))
                    return 0;

                var count = Directory.GetFiles(folderPath).Length;
                await Task.Run(() => Directory.Delete(folderPath, recursive: true));
                return count;
            }
            catch
            {
                return 0;
            }
        }

        public Task<List<string>> ListOwnerFilesAsync(string category, int ownerId)
        {
            var relativeDirectory = BuildRelativeDirectory(category, ownerId);
            var folderPath = Path.Combine(_uploadsPath, relativeDirectory);

            if (!Directory.Exists(folderPath))
                return Task.FromResult(new List<string>());

            var urls = Directory.GetFiles(folderPath)
                .Select(f => $"/uploads/{relativeDirectory.Replace(Path.DirectorySeparatorChar, '/')}/{Path.GetFileName(f)}")
                .ToList();

            return Task.FromResult(urls);
        }

        // ===================================================================
        // Paths
        // ===================================================================

        /// <summary>
        /// {category}/{ownerId}. The category is checked against a fixed set and the
        /// owner id is an integer, so neither can contain a separator or "..".
        /// </summary>
        private static string BuildRelativeDirectory(string category, int ownerId)
        {
            if (!AllowedCategories.Contains(category))
                throw new ArgumentException($"Unknown upload category '{category}'", nameof(category));

            if (ownerId <= 0)
                throw new ArgumentException("Owner id must be positive", nameof(ownerId));

            return Path.Combine(category.ToLowerInvariant(), ownerId.ToString());
        }

        /// <summary>
        /// Turns a stored public URL back into a path on disk, refusing anything that
        /// escapes the uploads root. Older records may still hold a flat
        /// "/uploads/folder/file.jpg", so those keep resolving too.
        /// </summary>
        private string? ResolveStoredPath(string publicUrl)
        {
            var relative = publicUrl
                .Replace("/uploads/", string.Empty)
                .TrimStart('/', '\\');

            if (string.IsNullOrWhiteSpace(relative))
                return null;

            var combined = Path.GetFullPath(Path.Combine(_uploadsPath, relative));
            var root = Path.GetFullPath(_uploadsPath);

            // Reject traversal out of the uploads root.
            return combined.StartsWith(root, StringComparison.Ordinal) ? combined : null;
        }
    }
}
