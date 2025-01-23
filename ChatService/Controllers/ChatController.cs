using De.Hsfl.LoomChat.Chat.Services;
using Microsoft.AspNetCore.Mvc;
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.Chat.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("channels")]
        public async Task<IActionResult> GetAllChannels([FromBody] GetChannelsRequest request)
        {
            var response = await _chatService.GetAllChannels(request);
            return Ok(response);
        }

        [HttpPost("users")]
        public async Task<IActionResult> AllUsers([FromBody] GetUsersRequest request)
        {
           var response = await _chatService.GetAllUsers(request);
            return response == null ? BadRequest("Fehler") : Ok(response);
        }

        [HttpPost("dms")]
        public async Task<IActionResult> AllDirectChannels([FromBody] GetDirectChannelsRequest request)
        {
            var response = await _chatService.GetAllDirectChannels(request);
            return Ok(response);
        }

        [HttpPost("openDm")]
        public async Task<IActionResult> OpenChatWithUser([FromBody] OpenChatWithUserRequest request)
        {
            var response = await _chatService.OpenChatWithUser(request);
            return Ok(response);
        }

        [HttpPost("createChannel")]
        public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
        {
            var response = await _chatService.CreateChannelAsync(request);
            return Ok(response);
        }

        [HttpPost("newMessage")]
        public async Task<IActionResult> NewMessage([FromBody] SendMessageRequest request)
        {
            var response = await _chatService.SendMessage(request);
            return Ok(response);
        }
    }

}