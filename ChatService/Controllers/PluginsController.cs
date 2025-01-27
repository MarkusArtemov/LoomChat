using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace De.Hsfl.LoomChat.Chat.Controllers
{
    [ApiController]
    [Route("plugins")]
    public class PluginsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PluginsController> _logger;

        public PluginsController(IWebHostEnvironment env, ILogger<PluginsController> logger)
        {
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Returns the PollPlugin DLL as a byte stream.
        /// </summary>
        [HttpGet("pollplugin")]
        public IActionResult GetPollPlugin()
        {
            var pluginPath = Path.Combine(_env.ContentRootPath, "Plugins", "De.Hsfl.LoomChat.PollPlugin.dll");

            if (!System.IO.File.Exists(pluginPath))
            {
                _logger.LogWarning("PollPlugin.dll not found at {Path}", pluginPath);
                return NotFound("Plugin DLL not found.");
            }

            _logger.LogInformation("Sending PollPlugin.dll from {Path}", pluginPath);
            var pluginBytes = System.IO.File.ReadAllBytes(pluginPath);
            var fileResult = File(pluginBytes, "application/octet-stream", "De.Hsfl.LoomChat.PollPlugin.dll");
            fileResult.EnableRangeProcessing = true;
            return fileResult;
        }

        /// <summary>
        /// Returns the BlackListPlugin DLL as a byte stream.
        /// </summary>
        [HttpGet("blacklistplugin")]
        public IActionResult GetBlackListPlugin()
        {
            var pluginPath = Path.Combine(_env.ContentRootPath, "Plugins", "De.Hsfl.LoomChat.BlackListPlugin.dll");

            if (!System.IO.File.Exists(pluginPath))
            {
                _logger.LogWarning("BlackListPlugin.dll not found at {Path}", pluginPath);
                return NotFound("Plugin DLL not found.");
            }

            _logger.LogInformation("Sending BlackListPlugin.dll from {Path}", pluginPath);
            var pluginBytes = System.IO.File.ReadAllBytes(pluginPath);
            var fileResult = File(pluginBytes, "application/octet-stream", "De.Hsfl.LoomChat.BlackListPlugin.dll");
            fileResult.EnableRangeProcessing = true;
            return fileResult;
        }
    }
}
