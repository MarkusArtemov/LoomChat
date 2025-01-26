using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Contracts
{
    public interface IPollPlugin : IChatPlugin
    {
        event Action<string, List<string>> PollCreatedEvent;
        event Action<string, Dictionary<string, int>> PollUpdatedEvent;
        event Action<string> PollClosedEvent;
        event Action<string> PollDeletedEvent;

        Task CreatePoll(int channelId, string title, List<string> options);
        Task Vote(string title, string option);
        Task ClosePoll(string title);
        Task DeletePoll(string title);
    }
}
