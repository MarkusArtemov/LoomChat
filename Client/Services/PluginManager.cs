using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Common.Contracts;
using De.Hsfl.LoomChat.Client.Security;

namespace De.Hsfl.LoomChat.Client.Services
{
    /// <summary>
    /// Lädt Plugins dynamisch (DLL vom Server),
    /// prüft Strong-Name, erzeugt Instanz, ruft Initialize().
    /// </summary>
    public class PluginManager
    {
        private readonly string _baseServerUrl;

        public PluginManager(string baseServerUrl)
        {
            _baseServerUrl = baseServerUrl;
        }

        /// <summary>
        /// Neue Methode, die BaseUrl und Token als Parameter annimmt.
        /// </summary>
        public async Task<IChatPlugin> DownloadAndLoadPluginAsync(
            string pluginName,
            string baseUrlForConstructor,
            string jwtToken)
        {
            // 1) KnownPlugins => PublicKeyToken prüfen
            if (!PluginKeyRegistry.KnownPlugins.TryGetValue(pluginName, out var expectedToken))
            {
                throw new Exception($"PluginKeyRegistry has no known token for plugin '{pluginName}'!");
            }

            // 2) DLL herunterladen
            var pluginUrl = $"{_baseServerUrl}/plugins/{pluginName.ToLower()}";
            byte[] pluginBytes;
            using (var httpClient = new HttpClient())
            {
                pluginBytes = await httpClient.GetByteArrayAsync(pluginUrl);
            }

            // 3) Strong Name check
            if (!ValidateStrongName(pluginBytes, expectedToken))
            {
                throw new SecurityException(
                    $"Downloaded plugin '{pluginName}' does NOT match the expected strong name token '{expectedToken}'!"
                );
            }

            // 4) Assembly laden
            var asm = Assembly.Load(pluginBytes);

            // 5) Typ suchen, der IChatPlugin implementiert
            var pluginType = asm.GetTypes().FirstOrDefault(t =>
                typeof(IChatPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            if (pluginType == null)
            {
                throw new Exception($"No IChatPlugin implementation found in plugin '{pluginName}'!");
            }

            // 6) Instanz mit Constructor-Parametern erzeugen
            object instance = Activator.CreateInstance(pluginType, baseUrlForConstructor, jwtToken);
            if (instance is IChatPlugin plugin)
            {
                // 7) Initialize aufrufen
                plugin.Initialize();
                return plugin;
            }

            throw new Exception($"Could not create IChatPlugin instance from plugin '{pluginName}'.");
        }

        private bool ValidateStrongName(byte[] assemblyBytes, string expectedTokenHex)
        {
            try
            {
                var asm = Assembly.Load(assemblyBytes);
                var tokenBytes = asm.GetName().GetPublicKeyToken();
                if (tokenBytes == null || tokenBytes.Length == 0) return false;

                var actualToken = BitConverter.ToString(tokenBytes).Replace("-", "").ToLowerInvariant();
                return (actualToken == expectedTokenHex);
            }
            catch
            {
                return false;
            }
        }
    }
}
