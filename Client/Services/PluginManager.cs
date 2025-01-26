using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Client.Security;
using De.Hsfl.LoomChat.Common.Contracts;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class PluginManager
    {
        private readonly string _baseServerUrl;

        public PluginManager(string baseServerUrl)
        {
            _baseServerUrl = baseServerUrl;
        }

        /// <summary>
        /// Lädt das Plugin (z.B. PollPlugin) entweder direkt von der lokalen Festplatte
        /// oder - wenn es noch nicht vorhanden ist - vom Server.
        /// </summary>
        public async Task<IChatPlugin> DownloadAndLoadPluginAsync(
            string pluginName,
            string baseUrlForConstructor,
            string jwtToken)
        {
            // 1) Wir definieren, wo wir das Plugin lokal cachen:
            var localFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PluginsCache");
            Directory.CreateDirectory(localFolder);

            var localDllPath = Path.Combine(localFolder, $"{pluginName}.dll");

            // 2) Zunächst prüfen wir, ob die DLL lokal existiert:
            if (File.Exists(localDllPath))
            {
                // Versuchen wir zu laden und den StrongName zu checken
                var pluginBytes = File.ReadAllBytes(localDllPath);

                // Optional: Du könntest hier die Versionsnummer im Dateinamen speichern
                // oder kurz den StrongName checken.

                // Hier rufen wir "ValidateStrongName" (kannst du anpassen oder rauslassen)
                if (ValidateStrongName(pluginBytes, PluginKeyRegistry.KnownPlugins[pluginName]))
                {
                    // Passt => Laden aus lokaler Datei
                    return LoadPluginFromBytes(pluginBytes, baseUrlForConstructor, jwtToken);
                }
                else
                {
                    // Falls nicht valide => löschen und neu vom Server laden
                    File.Delete(localDllPath);
                }
            }

            // 3) Wenn wir hier sind, haben wir lokal nichts Gültiges => vom Server downloaden
            var pluginUrl = $"{_baseServerUrl}/plugins/{pluginName.ToLower()}";
            byte[] downloadedBytes;
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                downloadedBytes = await httpClient.GetByteArrayAsync(pluginUrl);
            }

            // 4) StrongName check
            if (!ValidateStrongName(downloadedBytes, PluginKeyRegistry.KnownPlugins[pluginName]))
            {
                throw new SecurityException(
                    $"Downloaded plugin '{pluginName}' does NOT match the expected strong name token!"
                );
            }

            // 5) Lokal speichern, damit wir beim nächsten Start nicht erneut downloaden
            File.WriteAllBytes(localDllPath, downloadedBytes);

            // 6) Plugin-Assembly laden
            return LoadPluginFromBytes(downloadedBytes, baseUrlForConstructor, jwtToken);
        }

        private IChatPlugin LoadPluginFromBytes(byte[] assemblyBytes,
                                               string baseUrlForConstructor,
                                               string jwtToken)
        {
            var asm = Assembly.Load(assemblyBytes);
            var pluginType = asm.GetTypes()
                .FirstOrDefault(t => typeof(IChatPlugin).IsAssignableFrom(t) && !t.IsAbstract);

            if (pluginType == null)
            {
                throw new Exception("No IChatPlugin implementation found in plugin assembly!");
            }

            var instance = Activator.CreateInstance(pluginType, baseUrlForConstructor, jwtToken);
            if (instance is IChatPlugin plugin)
            {
                plugin.Initialize();
                return plugin;
            }
            throw new Exception("Could not create IChatPlugin instance.");
        }

        // Deine StrongName-Validierung
        private bool ValidateStrongName(byte[] assemblyBytes, string expectedTokenHex)
        {
            try
            {
                var asm = Assembly.Load(assemblyBytes);
                var tokenBytes = asm.GetName().GetPublicKeyToken();
                if (tokenBytes == null || tokenBytes.Length == 0) return false;

                var actualToken = BitConverter.ToString(tokenBytes)
                    .Replace("-", "")
                    .ToLowerInvariant();
                return (actualToken == expectedTokenHex);
            }
            catch
            {
                return false;
            }
        }
    }
}
