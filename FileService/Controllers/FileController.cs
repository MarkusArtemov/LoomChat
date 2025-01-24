using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using De.Hsfl.LoomChat.File.Services;
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.File.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }

        private int GetCurrentUserId()
        {
            var userClaim = User.FindFirst("sub")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null) return 0;
            return int.Parse(userClaim.Value);
        }

        [HttpPost("create-document")]
        public async Task<ActionResult<DocumentResponse>> CreateDocument([FromBody] CreateDocumentRequest request)
        {
            if (request == null) return BadRequest("No data provided");

            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("No valid user token");

            var docResponse = await _fileService.CreateDocumentAsync(request, currentUserId);
            return Ok(docResponse);
        }

        [HttpPost("{documentId}/upload")]
        public async Task<ActionResult<DocumentVersionResponse>> UploadVersion(int documentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized("No valid user token");

            var versionResponse = await _fileService.UploadDocumentVersionAsync(documentId, file, currentUserId);
            if (versionResponse == null)
                return NotFound("Document not found, not owner, or upload failed");

            return Ok(versionResponse);
        }

        [HttpGet("{documentId}/version/{versionNumber}")]
        public async Task<IActionResult> DownloadVersion(int documentId, int versionNumber)
        {
            var downloadResult = await _fileService.DownloadDocumentVersionAsync(documentId, versionNumber);
            if (downloadResult == null)
                return NotFound("File not found or version invalid");

            return File(
                downloadResult.FileStream,
                downloadResult.ContentType,
                downloadResult.FileName
            );
        }

        [HttpGet("{documentId}/versions")]
        public async Task<ActionResult<List<DocumentVersionResponse>>> GetVersions(int documentId)
        {
            var versions = await _fileService.GetDocumentVersionsAsync(documentId);
            return Ok(versions);
        }

        [HttpDelete("{documentId}/version/{versionNumber}")]
        public async Task<IActionResult> DeleteVersion(int documentId, int versionNumber)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized("No valid user token");

            var success = await _fileService.DeleteVersionAsync(documentId, versionNumber, currentUserId);
            if (!success)
                return BadRequest("Could not delete version (maybe used as base, or not owner)");

            return Ok("Version deleted");
        }

        [HttpDelete("{documentId}/all-versions")]
        public async Task<IActionResult> DeleteAllVersions(int documentId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Unauthorized("No valid user token");

            var success = await _fileService.DeleteAllVersionsAsync(documentId, currentUserId);
            if (!success)
                return BadRequest("Could not delete all versions (doc not found or not owner)");

            return Ok("All versions deleted");
        }

        [HttpGet("channel/{channelId}")]
        public async Task<ActionResult<List<DocumentResponse>>> GetDocumentsByChannel(int channelId)
        {
            var docs = await _fileService.GetDocumentsByChannelAsync(channelId);
            return Ok(docs);
        }
    }
}
