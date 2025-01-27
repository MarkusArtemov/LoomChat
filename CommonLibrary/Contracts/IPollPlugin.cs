using System.Collections.Generic;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Contracts
{
    public interface IPollPlugin : IChatPlugin
    {
        // Poll-spezifische Events:
        event System.Action<string, List<string>> PollCreatedEvent;
        event System.Action<string, System.Collections.Generic.Dictionary<string, int>> PollUpdatedEvent;
        event System.Action<string> PollClosedEvent;
        event System.Action<string> PollDeletedEvent;

        Task CreatePoll(int channelId, string title, List<string> options);
        Task Vote(string title, string option);
        Task ClosePoll(string title);
        Task DeletePoll(string title);
    }
}
