using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using De.Hsfl.LoomChat.File.Persistence;
using De.Hsfl.LoomChat.File.Options;
using De.Hsfl.LoomChat.File.Models;
using De.Hsfl.LoomChat.File.Hubs;
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.File.Services
{
    public class FileService
    {
        private readonly FileDbContext _context;
        private readonly FileStorageOptions _storageOptions;
        private readonly IHubContext<FileHub> _fileHubContext;

        public FileService(
            FileDbContext context,
            FileStorageOptions storageOptions,
            IHubContext<FileHub> fileHubContext)
        {
            _context = context;
            _storageOptions = storageOptions;
            _fileHubContext = fileHubContext;
        }

        /// <summary>
        /// Erzeugt ein neues Dokument (noch keine Datei-Version).
        /// </summary>
        public async Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request, int userId)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                request.Name = "Document";
            }

            var doc = new Document
            {
                Name = request.Name,
                ChannelId = request.ChannelId,
                OwnerUserId = userId,
                CreatedAt = System.DateTime.UtcNow,
                // Standard-Werte:
                FileType = "application/octet-stream",
                FileExtension = ".bin"
            };
            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            var ownerName = "User_" + doc.OwnerUserId; // Demo-Username
            var docResponse = new DocumentResponse(
                doc.Id,
                doc.Name,
                doc.OwnerUserId,
                doc.CreatedAt,
                doc.FileType,
                doc.ChannelId,
                ownerName,
                doc.FileExtension
            );

            // Echtzeit-Broadcast
            await _fileHubContext.Clients
                .Group($"file_channel_{doc.ChannelId}")
                .SendAsync("DocumentCreated", docResponse);

            return docResponse;
        }

        /// <summary>
        /// Lädt eine neue Version hoch (Multiform-data).
        /// </summary>
        public async Task<DocumentVersionResponse?> UploadDocumentVersionAsync(int documentId, IFormFile file, int currentUserId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return null;

            // Nur der Besitzer darf hochladen
            if (doc.OwnerUserId != currentUserId) return null;

            // Prüfen, welche Extension die hochgeladene Datei hat
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            // Falls das Document noch keine Versions hat, setzen wir die Extension
            // (Man könnte stattdessen auch immer überschreiben.)
            if (!doc.DocumentVersions.Any())
            {
                doc.FileExtension = extension;
            }
            // doc.FileExtension != extension => man kann hier meckern oder ignorieren

            // MIME‐Type absichern
            var contentType = file.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }
            doc.FileType = contentType; // fix: nie leer lassen

            // Neue Versionsnummer
            int newVersionNumber = doc.DocumentVersions.Any()
                ? doc.DocumentVersions.Max(v => v.VersionNumber) + 1
                : 1;

            var docNameSafe = SanitizeFileName(doc.Name);
            var serverFileName = $"{docNameSafe}_v{newVersionNumber}{doc.FileExtension}";
            var fullPath = Path.Combine(_storageOptions.StoragePath, serverFileName);

            // Datei speichern
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var newVersion = new DocumentVersion
            {
                DocumentId = doc.Id,
                VersionNumber = newVersionNumber,
                StoragePath = fullPath,
                CreatedAt = System.DateTime.UtcNow
            };

            _context.DocumentVersions.Add(newVersion);
            await _context.SaveChangesAsync();

            var versionResponse = new DocumentVersionResponse(
                newVersion.Id,
                newVersion.DocumentId,
                newVersion.VersionNumber,
                newVersion.CreatedAt,
                doc.FileExtension,
                doc.FileType
            );

            // Broadcast VersionCreated
            await _fileHubContext.Clients
                .Group($"file_channel_{doc.ChannelId}")
                .SendAsync("VersionCreated", versionResponse);

            return versionResponse;
        }

        public async Task<List<DocumentResponse>> GetDocumentsByChannelAsync(int channelId)
        {
            var docs = await _context.Documents
                .Where(d => d.ChannelId == channelId)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync();

            var list = new List<DocumentResponse>();
            foreach (var d in docs)
            {
                var ownerName = "User_" + d.OwnerUserId; // Bsp.: Nutzername
                list.Add(new DocumentResponse(
                    d.Id,
                    d.Name,
                    d.OwnerUserId,
                    d.CreatedAt,
                    d.FileType,
                    d.ChannelId,
                    ownerName,
                    d.FileExtension
                ));
            }
            return list;
        }

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
                v.CreatedAt,
                v.Document?.FileExtension ?? ".bin",
                v.Document?.FileType ?? "application/octet-stream"
            )).ToList();
        }

        /// <summary>
        /// Liefert einen Stream zum Herunterladen einer bestimmten Version.
        /// </summary>
        public async Task<FileDownloadResult?> DownloadDocumentVersionAsync(int documentId, int versionNumber)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc == null) return null;

            var version = await _context.DocumentVersions
                .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.VersionNumber == versionNumber);
            if (version == null) return null;

            if (!System.IO.File.Exists(version.StoragePath)) return null;

            // Stream öffnen
            var stream = new FileStream(version.StoragePath, FileMode.Open, FileAccess.Read);

            // z.B. "MeinDokument_v1.pdf"
            var docNameSafe = SanitizeFileName(doc.Name);
            var fileName = $"{docNameSafe}_v{version.VersionNumber}{doc.FileExtension}";
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "download.bin"; // Fallback

            var contentType = doc.FileType;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            return new FileDownloadResult
            {
                FileStream = stream,
                FileName = fileName,
                ContentType = contentType
            };
        }

        // -------------------------------------------------------
        // DOKUMENT + Versionen löschen
        // -------------------------------------------------------
        public async Task<bool> DeleteDocumentAsync(int documentId, int currentUserId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return false;

            // Nur der Besitzer
            if (doc.OwnerUserId != currentUserId) return false;

            // Physische Dateien entfernen
            foreach (var ver in doc.DocumentVersions)
            {
                if (System.IO.File.Exists(ver.StoragePath))
                {
                    System.IO.File.Delete(ver.StoragePath);
                }
            }

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            // Broadcast => "DocumentDeleted"
            await _fileHubContext.Clients
                .Group($"file_channel_{doc.ChannelId}")
                .SendAsync("DocumentDeleted", doc.Id);

            return true;
        }

        // -------------------------------------------------------
        // Eine einzelne Version löschen
        // -------------------------------------------------------
        public async Task<bool> DeleteVersionAsync(int documentId, int versionNumber, int currentUserId)
        {
            var doc = await _context.Documents
                .Include(d => d.DocumentVersions)
                .FirstOrDefaultAsync(d => d.Id == documentId);
            if (doc == null) return false;

            // Nur der Besitzer
            if (doc.OwnerUserId != currentUserId) return false;

            var version = doc.DocumentVersions
                .FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version == null) return false;

            if (System.IO.File.Exists(version.StoragePath))
            {
                System.IO.File.Delete(version.StoragePath);
            }

            _context.DocumentVersions.Remove(version);
            await _context.SaveChangesAsync();

            // Broadcast => "VersionDeleted"
            await _fileHubContext.Clients
                .Group($"file_channel_{doc.ChannelId}")
                .SendAsync("VersionDeleted", new { DocumentId = doc.Id, VersionNumber = versionNumber });

            return true;
        }

        private string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Document";

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
