using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using De.Hsfl.LoomChat.File.Persistence;
using De.Hsfl.LoomChat.File.Options;
using De.Hsfl.LoomChat.File.Models;
using De.Hsfl.LoomChat.Common.Dtos; 

namespace De.Hsfl.LoomChat.File.Services
{
    /// <summary>
    /// Provides methods to manage document storage and retrieval
    /// </summary>
    public class FileService
    {
        private readonly FileDbContext _context;
        private readonly FileStorageOptions _storageOptions;

        public FileService(FileDbContext context, FileStorageOptions storageOptions)
        {
            _context = context;
            _storageOptions = storageOptions;
        }

        /// <summary>
        /// Creates a document in DB and returns a DocumentResponse
        /// </summary>
        public async Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request)
        {
            var doc = new Document
            {
                Name = request.Name,
                ChannelId = request.ChannelId,
                OwnerUserId = request.OwnerUserId,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            return new DocumentResponse(
                doc.Id,
                doc.Name,
                doc.OwnerUserId,
                doc.CreatedAt,
                doc.Description,
                doc.ChannelId
            );
        }

        /// <summary>
        /// Uploads a new version for an existing document
        /// </summary>
        public async Task<DocumentVersionResponse?> UploadDocumentVersionAsync(int documentId, IFormFile file)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (doc == null) return null;

            // Determine the new version number
            int newVersionNumber = doc.DocumentVersions.Any()
                ? doc.DocumentVersions.Max(v => v.VersionNumber) + 1
                : 1;

            // Generate filename
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{documentId}_v{newVersionNumber}{extension}";
            var fullPath = Path.Combine(_storageOptions.StoragePath, fileName);

            // Save file to disk
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var newVersion = new DocumentVersion
            {
                DocumentId = doc.Id,
                VersionNumber = newVersionNumber,
                StoragePath = fullPath,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentVersions.Add(newVersion);
            await _context.SaveChangesAsync();

            return new DocumentVersionResponse(
                newVersion.Id,
                newVersion.DocumentId,
                newVersion.VersionNumber,
                newVersion.StoragePath,
                newVersion.CreatedAt
            );
        }

        /// <summary>
        /// Retrieves and streams a specific version of a document
        /// </summary>
        public async Task<FileDownloadResult?> DownloadDocumentVersionAsync(int documentId, int versionNumber)
        {
            var version = await _context.DocumentVersions
                .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNumber == versionNumber);

            if (version == null) return null;
            if (!System.IO.File.Exists(version.StoragePath)) return null;

            var fileStream = new FileStream(version.StoragePath, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(version.StoragePath);

            return new FileDownloadResult
            {
                FileStream = fileStream,
                FileName = fileName,
                ContentType = MediaTypeNames.Application.Octet
            };
        }

        /// <summary>
        /// Lists all versions of a document as DocumentVersionResponse
        /// </summary>
        public async Task<List<DocumentVersionResponse>> GetDocumentVersionsAsync(int documentId)
        {
            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderBy(v => v.VersionNumber)
                .ToListAsync();

            return versions.Select(v => new DocumentVersionResponse(
                v.Id,
                v.DocumentId,
                v.VersionNumber,
                v.StoragePath,
                v.CreatedAt
            )).ToList();
        }
    }

    /// <summary>
    /// Represents a file download
    /// </summary>
    public class FileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
    }
}
