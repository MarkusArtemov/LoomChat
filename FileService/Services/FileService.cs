using System.IO;
using Microsoft.EntityFrameworkCore;
using De.Hsfl.LoomChat.File.Persistence;
using De.Hsfl.LoomChat.File.Options;
using De.Hsfl.LoomChat.File.Models;
using De.Hsfl.LoomChat.File.Helpers;
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.File.Services
{
    public class FileService
    {
        private readonly FileDbContext _context;
        private readonly FileStorageOptions _storageOptions;

        public FileService(FileDbContext context, FileStorageOptions storageOptions)
        {
            _context = context;
            _storageOptions = storageOptions;
        }

        public async Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request, int userId)
        {
            var doc = new Document
            {
                Name = request.Name,
                ChannelId = request.ChannelId,
                OwnerUserId = userId,
                CreatedAt = DateTime.UtcNow,
                FileType = "application/octet-stream",
                FileExtension = ".bin"
            };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            return new DocumentResponse(
                doc.Id,
                doc.Name,
                doc.OwnerUserId,
                doc.CreatedAt,
                doc.FileType,
                doc.ChannelId
            );
        }

        public async Task<DocumentVersionResponse?> UploadDocumentVersionAsync(int documentId, IFormFile file, int currentUserId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return null;
            if (doc.OwnerUserId != currentUserId) return null;

            var extension = Path.GetExtension(file.FileName);
            if (!doc.DocumentVersions.Any())
            {
                doc.FileExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(extension) && extension != doc.FileExtension)
                    return null;
            }

            var contentType = file.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            doc.FileType = contentType;

            int newVersionNumber = doc.DocumentVersions.Any()
                ? doc.DocumentVersions.Max(v => v.VersionNumber) + 1
                : 1;

            bool isFull = (newVersionNumber % 5 == 1);

            var docNameSafe = SanitizeFileName(doc.Name);
            var serverFileName = $"{docNameSafe}_v{newVersionNumber}{doc.FileExtension}";
            var fullPath = Path.Combine(_storageOptions.StoragePath, serverFileName);

            DocumentVersion newVersion;
            if (isFull)
            {
                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                newVersion = new DocumentVersion
                {
                    DocumentId = doc.Id,
                    VersionNumber = newVersionNumber,
                    IsFull = true,
                    BaseVersionId = null,
                    StoragePath = fullPath,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                var prevVersion = doc.DocumentVersions
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefault();
                if (prevVersion == null) return null;

                var baseFilePath = await ReconstructFileAsync(doc.Id, prevVersion.VersionNumber);
                if (baseFilePath == null) return null;

                var tempNewFile = Path.Combine(_storageOptions.StoragePath,
                    $"temp_new_{Guid.NewGuid()}{doc.FileExtension}");
                using (var fs = new FileStream(tempNewFile, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }
                DeltaUtility.CreateDelta(baseFilePath, tempNewFile, fullPath);
                if (global::System.IO.File.Exists(tempNewFile))
                {
                    global::System.IO.File.Delete(tempNewFile);
                }

                newVersion = new DocumentVersion
                {
                    DocumentId = doc.Id,
                    VersionNumber = newVersionNumber,
                    IsFull = false,
                    BaseVersionId = prevVersion.Id,
                    StoragePath = fullPath,
                    CreatedAt = DateTime.UtcNow
                };
            }
            _context.DocumentVersions.Add(newVersion);
            await _context.SaveChangesAsync();

            return new DocumentVersionResponse(
                newVersion.Id,
                newVersion.DocumentId,
                newVersion.VersionNumber,
                newVersion.CreatedAt,
                doc.FileExtension,
                doc.FileType
            );
        }

        public async Task<FileDownloadResult?> DownloadDocumentVersionAsync(int documentId, int versionNumber)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc == null) return null;

            if (string.IsNullOrWhiteSpace(doc.FileType))
            {
                doc.FileType = "application/octet-stream";
            }

            var finalPath = await ReconstructFileAsync(documentId, versionNumber);
            if (finalPath == null || !global::System.IO.File.Exists(finalPath))
                return null;

            var fileStream = new FileStream(finalPath, FileMode.Open, FileAccess.Read);

            var docNameSafe = SanitizeFileName(doc.Name);
            var finalFileName = $"{docNameSafe}_v{versionNumber}{doc.FileExtension}";

            return new FileDownloadResult
            {
                FileStream = fileStream,
                FileName = finalFileName,
                ContentType = doc.FileType
            };
        }

        public async Task<List<DocumentVersionResponse>> GetDocumentVersionsAsync(int documentId)
        {
            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderBy(v => v.VersionNumber)
                .ToListAsync();

            return versions
                .Select(v => new DocumentVersionResponse(
                    v.Id,
                    v.DocumentId,
                    v.VersionNumber,
                    v.CreatedAt,
                    v.Document?.FileExtension ?? ".bin",
                    v.Document?.FileType ?? "application/octet-stream"
                ))
                .ToList();
        }

        public async Task<bool> DeleteVersionAsync(int documentId, int versionNumber, int currentUserId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return false;
            if (doc.OwnerUserId != currentUserId) return false;

            var version = doc.DocumentVersions
                .FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version == null) return false;

            bool isBaseForOthers = doc.DocumentVersions
                .Any(v => v.BaseVersionId == version.Id);
            if (isBaseForOthers) return false;

            _context.DocumentVersions.Remove(version);
            await _context.SaveChangesAsync();

            if (global::System.IO.File.Exists(version.StoragePath))
            {
                global::System.IO.File.Delete(version.StoragePath);
            }
            return true;
        }

        public async Task<bool> DeleteAllVersionsAsync(int documentId, int currentUserId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return false;
            if (doc.OwnerUserId != currentUserId) return false;

            foreach (var ver in doc.DocumentVersions)
            {
                if (global::System.IO.File.Exists(ver.StoragePath))
                {
                    global::System.IO.File.Delete(ver.StoragePath);
                }
            }
            _context.DocumentVersions.RemoveRange(doc.DocumentVersions);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DocumentResponse>> GetDocumentsByChannelAsync(int channelId)
        {
            var docs = await _context.Documents
                .Where(d => d.ChannelId == channelId)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();

            return docs.Select(d => new DocumentResponse(
                d.Id,
                d.Name,
                d.OwnerUserId,
                d.CreatedAt,
                d.FileType,
                d.ChannelId
            )).ToList();
        }

        private async Task<string?> ReconstructFileAsync(int documentId, int versionNumber)
        {
            var version = await _context.DocumentVersions
                .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNumber == versionNumber);
            if (version == null) return null;

            if (version.IsFull) return version.StoragePath;
            if (!version.BaseVersionId.HasValue) return null;

            var baseVersion = await _context.DocumentVersions
                .FirstOrDefaultAsync(v => v.Id == version.BaseVersionId.Value);
            if (baseVersion == null) return null;

            var basePath = await ReconstructFileAsync(documentId, baseVersion.VersionNumber);
            if (basePath == null || !global::System.IO.File.Exists(basePath))
                return null;

            var tempOut = Path.Combine(_storageOptions.StoragePath,
                $"reconstruct_{documentId}_v{versionNumber}_{Guid.NewGuid()}.tmp");

            DeltaUtility.ApplyDelta(basePath, version.StoragePath, tempOut);
            return tempOut;
        }

        private static string SanitizeFileName(string input)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input;
        }
    }

    public class FileDownloadResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
    }
}
