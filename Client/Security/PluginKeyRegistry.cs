using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Client.Security
{
    /// <summary>
    /// Hält bekannte Plugin-Namen und zugehörige PublicKeyTokens 
    /// (oder PublicKeys) für Strong-Name-Checks.
    /// </summary>
    public static class PluginKeyRegistry
    {
        public static readonly Dictionary<string, string> KnownPlugins = new()
        {
            // per sn -Tp ermittelte PublicKeyToken
            { "PollPlugin", "4eebb1e39db60e60" },
        };
    }
}
