using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Common.Contracts;
using De.Hsfl.LoomChat.Client.Security;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class PluginManager
    {
        private readonly string _baseServerUrl;

        public PluginManager(string baseServerUrl)
        {
            _baseServerUrl = baseServerUrl;
        }

        public async Task<IChatPlugin> DownloadAndLoadPluginAsync(
            string pluginName,
            string baseUrlForConstructor,
            string jwtToken)
        {
            // Download plugin from server
            var pluginUrl = $"{_baseServerUrl}/plugins/{pluginName.ToLower()}";
            byte[] downloadedBytes;

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                downloadedBytes = await httpClient.GetByteArrayAsync(pluginUrl);
            }

            // Load and return plugin instance
            return LoadPluginFromBytes(downloadedBytes, baseUrlForConstructor, jwtToken);
        }

        private IChatPlugin LoadPluginFromBytes(
            byte[] assemblyBytes,
            string baseUrlForConstructor,
            string jwtToken)
        {
            // Load assembly from bytes
            var asm = Assembly.Load(assemblyBytes);

            // Check PublicKeyToken
            var publicKeyTokenBytes = asm.GetName().GetPublicKeyToken();
            if (publicKeyTokenBytes == null || publicKeyTokenBytes.Length == 0)
            {
                throw new Exception("Plugin assembly has no PublicKeyToken!");
            }

            // Convert bytes to string
            var publicKeyTokenString = BitConverter
                .ToString(publicKeyTokenBytes)
                .Replace("-", "")
                .ToLowerInvariant();

            // Compare to known token
            if (publicKeyTokenString != PluginKeyRegistry.GlobalPublicKeyToken)
            {
                throw new Exception($"Wrong PublicKeyToken: {publicKeyTokenString}, expected: {PluginKeyRegistry.GlobalPublicKeyToken}");
            }

            // Find a class that implements IChatPlugin
            var pluginType = asm.GetTypes()
                .FirstOrDefault(t => typeof(IChatPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            if (pluginType == null)
            {
                throw new Exception("No IChatPlugin implementation found in assembly!");
            }

            // Create instance
            var instance = Activator.CreateInstance(pluginType, baseUrlForConstructor, jwtToken);
            if (instance is IChatPlugin plugin)
            {
                plugin.Initialize();
                return plugin;
            }

            throw new Exception("Could not create IChatPlugin instance.");
        }
    }
}
