using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace De.Hsfl.LoomChat.Chat.Controllers
{
    /// <summary>
    /// Handles REST endpoints for channels, users, etc.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("channels")]
        public async Task<IActionResult> GetAllChannels([FromBody] GetChannelsRequest request)
        {
            _logger.LogInformation("HTTP POST /Chat/channels called. UserId={UserId}", request.UserId);

            var response = await _chatService.GetAllChannels(request);
            return Ok(response);
        }

        [HttpPost("users")]
        public async Task<IActionResult> AllUsers([FromBody] GetUsersRequest request)
        {
            _logger.LogInformation("HTTP POST /Chat/users called");

            var response = await _chatService.GetAllUsers(request);
            if (response == null)
            {
                _logger.LogWarning("/Chat/users returned null from ChatService.");
                return BadRequest("Fehler beim Abrufen der Nutzerliste.");
            }

            return Ok(response);
        }

        [HttpPost("dms")]
        public async Task<IActionResult> AllDirectChannels([FromBody] GetDirectChannelsRequest request)
        {
            _logger.LogInformation("HTTP POST /Chat/dms called. UserId={UserId}", request.UserId);

            var response = await _chatService.GetAllDirectChannels(request);
            return Ok(response);
        }

        [HttpPost("openDm")]
        public async Task<IActionResult> OpenChatWithUser([FromBody] OpenChatWithUserRequest request)
        {
            _logger.LogInformation("HTTP POST /Chat/openDm called. OwnId={OwnId}, OtherId={OtherId}",
                                   request.OwnId, request.OtherId);

            var response = await _chatService.OpenChatWithUser(request);
            return Ok(response);
        }

        [HttpPost("createChannel")]
        public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
        {
            _logger.LogInformation("HTTP POST /Chat/createChannel called. UserId={UserId}, ChannelName={ChannelName}",
                                   request.UserId, request.ChannelName);

            var response = await _chatService.CreateChannelAsync(request);
            return Ok(response);
        }

        [HttpPost("newMessage")]
        public async Task<IActionResult> NewMessage([FromBody] SendMessageRequest request)
        {
            _logger.LogInformation("HTTP POST /Chat/newMessage called. ChannelId={ChannelId}, UserId={UserId}, Message={Message}",
                                   request.ChannelId, request.UserId, request.Message);

            var response = await _chatService.SendMessage(request);
            return Ok(response);
        }
    }
}
