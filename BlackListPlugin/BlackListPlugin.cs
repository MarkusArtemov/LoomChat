using System.Text.RegularExpressions;
using De.Hsfl.LoomChat.Common.Contracts;

namespace De.Hsfl.LoomChat.BlackListPlugin
{
    public class BlackListPlugin : ITextFilterPlugin
    {
        public string Name => "BlackListPlugin";

        // Words to be censored
        private readonly List<string> _blacklistWords = new()
        {
            "schimpfwort",
            "mist",
            "idiot",
            "trottel",
            "dummkopf",
            "blödmann",
            "heuchler",
            "doof",
            "banane",
        };

        // Constructor with optional parameters
        public BlackListPlugin(string baseUrl, string jwtToken)
        {
        }

        public async Task Initialize()
        {
            // Could load external lists here
            await Task.CompletedTask;
        }

        public string OnBeforeSend(string message)
        {
            return CensorMessage(message);
        }

        public string OnBeforeReceive(string message)
        {
            return CensorMessage(message);
        }

        // Replaces blacklisted words with matching number of '*'
        private string CensorMessage(string message)
        {
            foreach (var word in _blacklistWords)
            {
                var pattern = $@"\b{Regex.Escape(word)}\b";
                message = Regex.Replace(
                    message,
                    pattern,
                    match => new string('*', match.Value.Length), 
                    RegexOptions.IgnoreCase
                );
            }
            return message;
        }
    }
}
