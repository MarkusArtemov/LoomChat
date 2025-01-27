using System;

namespace De.Hsfl.LoomChat.Client.Security
{
    /// <summary>
    /// Holds the single known PublicKeyToken for all plugins
    /// signed with the same .snk file.
    /// </summary>
    public static class PluginKeyRegistry
    {
        public const string GlobalPublicKeyToken = "4eebb1e39db60e60";
    }
}
