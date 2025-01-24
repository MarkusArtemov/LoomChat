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
        public async Task JoinChannel(int channelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "file_channel_" + channelId);
        }

        // Optional: Falls du manuell vom Server broadcasten willst,
        // wir machen es aber normal in FileService via IHubContext<FileHub>
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

        public async Task NotifyDocumentDeleted(int documentId, int channelId)
        {
            await Clients.Group("file_channel_" + channelId)
                         .SendAsync("DocumentDeleted", documentId);
        }

        public async Task NotifyVersionDeleted(int documentId, int versionNumber, int channelId)
        {
            // Man könnte auch ein spezielles DTO senden
            await Clients.Group("file_channel_" + channelId)
                         .SendAsync("VersionDeleted", new { DocumentId = documentId, VersionNumber = versionNumber });
        }
    }
}
