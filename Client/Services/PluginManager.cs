using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Common.Contracts;
using De.Hsfl.LoomChat.Client.Security;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class PluginManager
    {
        private readonly string _baseServerUrl;
        private readonly string _pluginFolder;

        public PluginManager(string baseServerUrl)
        {
            _baseServerUrl = baseServerUrl;
            _pluginFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(_pluginFolder))
            {
                Directory.CreateDirectory(_pluginFolder);
            }
        }

        public async Task<IChatPlugin> InstallAndLoadPluginAsync(
            string pluginName,
            string baseUrlForConstructor,
            string jwtToken)
        {
            var localPluginPath = Path.Combine(_pluginFolder, $"{pluginName}.dll");

            // If not in local folder, download first.
            if (!File.Exists(localPluginPath))
            {
                await DownloadPluginAsync(pluginName, localPluginPath);
            }
            return LoadPluginFromPath(localPluginPath, baseUrlForConstructor, jwtToken);
        }

        public void UninstallPlugin(string pluginName)
        {
            var localPluginPath = Path.Combine(_pluginFolder, $"{pluginName}.dll");
            if (File.Exists(localPluginPath))
            {
                File.Delete(localPluginPath);
            }
        }

        private async Task DownloadPluginAsync(string pluginName, string localPath)
        {
            var pluginUrl = $"{_baseServerUrl}/plugins/{pluginName.ToLower()}";
            using var httpClient = new HttpClient();
            var bytes = await httpClient.GetByteArrayAsync(pluginUrl);
            await File.WriteAllBytesAsync(localPath, bytes);
        }

        private IChatPlugin LoadPluginFromPath(string pluginPath, string baseUrlForConstructor, string jwtToken)
        {
            var assemblyBytes = File.ReadAllBytes(pluginPath);
            return LoadPluginFromBytes(assemblyBytes, baseUrlForConstructor, jwtToken);
        }

        private IChatPlugin LoadPluginFromBytes(byte[] assemblyBytes, string baseUrlForConstructor, string jwtToken)
        {
            var asm = Assembly.Load(assemblyBytes);
            var publicKeyTokenBytes = asm.GetName().GetPublicKeyToken();
            if (publicKeyTokenBytes == null || publicKeyTokenBytes.Length == 0)
            {
                throw new Exception("Plugin assembly has no PublicKeyToken!");
            }

            var publicKeyTokenString = BitConverter
                .ToString(publicKeyTokenBytes)
                .Replace("-", "")
                .ToLowerInvariant();

            if (publicKeyTokenString != PluginKeyRegistry.GlobalPublicKeyToken)
            {
                throw new Exception($"Wrong PublicKeyToken: {publicKeyTokenString}, expected: {PluginKeyRegistry.GlobalPublicKeyToken}");
            }

            var pluginType = asm.GetTypes()
                .FirstOrDefault(t => typeof(IChatPlugin).IsAssignableFrom(t) && !t.IsAbstract);
            if (pluginType == null)
            {
                throw new Exception("No IChatPlugin implementation found in assembly!");
            }

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
