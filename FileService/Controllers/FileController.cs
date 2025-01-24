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
            if (request == null)
                return BadRequest("No data provided");

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var doc = await _fileService.CreateDocumentAsync(request, userId);
            return Ok(doc);
        }

        [HttpPost("{documentId}/upload")]
        public async Task<ActionResult<DocumentVersionResponse>> UploadVersion(int documentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var version = await _fileService.UploadDocumentVersionAsync(documentId, file, userId);
            if (version == null)
                return NotFound("Document not found or not owner, or extension mismatch");

            return Ok(version);
        }

        [HttpGet("channel/{channelId}")]
        public async Task<ActionResult<List<DocumentResponse>>> GetDocsByChannel(int channelId)
        {
            var docs = await _fileService.GetDocumentsByChannelAsync(channelId);
            return Ok(docs);
        }

        [HttpGet("{documentId}/versions")]
        public async Task<ActionResult<List<DocumentVersionResponse>>> GetVersions(int documentId)
        {
            var vers = await _fileService.GetDocumentVersionsAsync(documentId);
            return Ok(vers);
        }

        [HttpGet("{documentId}/version/{versionNumber}")]
        public async Task<IActionResult> DownloadVersion(int documentId, int versionNumber)
        {
            var downloadResult = await _fileService.DownloadDocumentVersionAsync(documentId, versionNumber);
            if (downloadResult == null)
                return NotFound("Document or version not found");

            return File(
                downloadResult.FileStream,
                downloadResult.ContentType,
                downloadResult.FileName
            );
        }

        // ------------------------------------------------
        // DELETE Document => + versions => broadcast
        // ------------------------------------------------
        [HttpDelete("{documentId}")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var success = await _fileService.DeleteDocumentAsync(documentId, userId);
            if (!success)
                return BadRequest("Couldn't delete doc (not found or not owner)");

            return Ok("Document deleted");
        }

        // ------------------------------------------------
        // DELETE a single version => broadcast
        // ------------------------------------------------
        [HttpDelete("{documentId}/version/{versionNumber}")]
        public async Task<IActionResult> DeleteVersion(int documentId, int versionNumber)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var success = await _fileService.DeleteVersionAsync(documentId, versionNumber, userId);
            if (!success)
                return BadRequest("Couldn't delete version (not found or not owner)");

            return Ok("Version deleted");
        }
    }
}
