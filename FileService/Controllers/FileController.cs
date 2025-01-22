using Microsoft.AspNetCore.Mvc;
using De.Hsfl.LoomChat.File.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace De.Hsfl.LoomChat.File.Controllers
{
    /// <summary>
    /// Handles file-related endpoints
    /// </summary>
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

        [HttpPost("create-document")]
        public async Task<ActionResult<DocumentResponse>> CreateDocument([FromBody] CreateDocumentRequest request)
        {
            if (request == null) return BadRequest("No data provided");
            var docResponse = await _fileService.CreateDocumentAsync(request);
            return Ok(docResponse);
        }

        [HttpPost("{documentId}/upload")]
        public async Task<ActionResult<DocumentVersionResponse>> UploadVersion(int documentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var versionResponse = await _fileService.UploadDocumentVersionAsync(documentId, file);
            if (versionResponse == null)
                return NotFound("Document not found or upload failed");

            return Ok(versionResponse);
        }

        [HttpGet("{documentId}/version/{versionNumber}")]
        public async Task<IActionResult> DownloadVersion(int documentId, int versionNumber)
        {
            var downloadResult = await _fileService.DownloadDocumentVersionAsync(documentId, versionNumber);
            if (downloadResult == null)
                return NotFound("File not found or version invalid");

            return File(downloadResult.FileStream, downloadResult.ContentType, downloadResult.FileName);
        }

        [HttpGet("{documentId}/versions")]
        public async Task<ActionResult<List<DocumentVersionResponse>>> GetVersions(int documentId)
        {
            var versions = await _fileService.GetDocumentVersionsAsync(documentId);
            return Ok(versions);
        }
    }
}
