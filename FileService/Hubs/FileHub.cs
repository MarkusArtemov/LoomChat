using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using De.Hsfl.LoomChat.Common.Dtos;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.File.Hubs
{
    [Authorize]
    public class FileHub : Hub
    {
        // Client ruft das auf, um einer Channel-Gruppe beizutreten
        // So bekommt er in Echtzeit DocumentCreated/VersionCreated für diesen Channel
        public async Task JoinChannel(int channelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "file_channel_" + channelId);
        }

        // (Optional) Falls du testweise manuell vom Server broadcasten möchtest
        public async Task NotifyDocumentCreated(DocumentResponse doc, int channelId)
        {
            await Clients.Group("file_channel_" + channelId)
                         .SendAsync("DocumentCreated", doc);
        }

        public async Task NotifyVersionCreated(DocumentVersionResponse version, int channelId)
        {
            await Clients.Group("file_channel_" + channelId)
                         .SendAsync("VersionCreated", version);
        }
    }
}
