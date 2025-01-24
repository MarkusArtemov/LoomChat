using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.Windows; // Falls du WPF-MessageBox etc. nutzen willst
using De.Hsfl.LoomChat.Client.Plugins; // IPlugin-Interface

namespace De.Hsfl.LoomChat.Client.Plugins
{
    public static class PluginManager
    {
        // Alle aktuell geladenen Plugins
        private static readonly Dictionary<string, IPlugin> LoadedPlugins = new Dictionary<string, IPlugin>();

        /// <summary>
        /// Lädt ein Plugin (z.B. SurveyPlugin) zur Laufzeit.
        /// 1) Download
        /// 2) SHA256-Check
        /// 3) Reflection Load => IPlugin
        /// 4) Initialize
        /// </summary>
        /// <param name="pluginName">Eindeutiger Name, z.B. "SurveyPlugin"</param>
        /// <param name="downloadUrl">wo die DLL liegt, z.B. http://localhost:5277/plugins/SurveyPlugin.dll</param>
        /// <param name="expectedSha256">Sicherheits-check: SHA256-Hash der DLL</param>
        /// <returns>true, wenn erfolgreich; sonst false</returns>
        public static bool LoadPlugin(string pluginName, string downloadUrl, string expectedSha256)
        {
            try
            {
                // 1) DLL von Server laden in ein Temp-Verzeichnis
                string tempDir = Path.Combine(Path.GetTempPath(), "LoomChatPlugins");
                Directory.CreateDirectory(tempDir);

                string localPath = Path.Combine(tempDir, pluginName + ".dll");
                using (var client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, localPath);
                }

                // 2) SHA256 überprüfen
                string fileHash = ComputeSha256OfFile(localPath);
                if (!fileHash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"SHA256-Check fehlgeschlagen. Erwartet {expectedSha256}, aber war {fileHash}");
                    File.Delete(localPath);
                    return false;
                }

                // 3) Reflection load => IPlugin
                var assembly = Assembly.LoadFrom(localPath);

                // Finde Typ, der IPlugin implementiert
                var pluginType = assembly.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);
                if (pluginType == null)
                {
                    MessageBox.Show("In der DLL wurde kein Typ gefunden, der IPlugin implementiert.");
                    return false;
                }

                // Instanz erzeugen
                var pluginInstance = (IPlugin)Activator.CreateInstance(pluginType);
                pluginInstance.Initialize();

                // 4) In Dictionary speichern
                LoadedPlugins[pluginInstance.Name] = pluginInstance;

                MessageBox.Show($"Plugin '{pluginInstance.Name}' erfolgreich geladen und initialisiert.");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Plugins: {ex.Message}");
                return false;
            }
        }

        public static IPlugin GetPlugin(string pluginName)
        {
            if (LoadedPlugins.ContainsKey(pluginName))
            {
                return LoadedPlugins[pluginName];
            }
            return null;
        }

        private static string ComputeSha256OfFile(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
