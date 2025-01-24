using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32; // Für OpenFileDialog/SaveFileDialog
using Newtonsoft.Json;
using System.Windows;  // Falls du WPF-MessageBox nutzt
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class FileService
    {
        private readonly string _baseUrl;
        private readonly string _jwtToken;

        // Hier die SignalR-Connection zum FileHub
        private HubConnection _hubConnection;

        // Events für Echtzeit-Anbindung
        public event Action<DocumentResponse> OnDocumentCreated;
        public event Action<DocumentVersionResponse> OnVersionCreated;

        public FileService(string baseUrl, string jwtToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _jwtToken = jwtToken;
        }

        // ------------------------------------------------
        // 1) SIGNALR-Funktionen (FileHub)
        // ------------------------------------------------
        public async Task InitializeFileHubAsync()
        {
            // Beispiel: Hub unter /fileHub
            var hubUrl = $"{_baseUrl}/fileHub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // JWT, falls benötigt
                    options.AccessTokenProvider = () => Task.FromResult(_jwtToken);
                })
                .Build();

            // Events vom Server empfangen
            _hubConnection.On<DocumentResponse>("DocumentCreated", doc =>
            {
                // Client teilt es per Event weiter ans ViewModel
                OnDocumentCreated?.Invoke(doc);
            });

            _hubConnection.On<DocumentVersionResponse>("VersionCreated", version =>
            {
                // Genauso
                OnVersionCreated?.Invoke(version);
            });

            await _hubConnection.StartAsync();
        }

        /// <summary>
        /// Tritt einer file_channel_{channelId}-Gruppe bei,
        /// um Dokument-Events in Echtzeit zu empfangen.
        /// </summary>
        public async Task JoinFileChannel(int channelId)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("JoinChannel", channelId);
        }

        // ------------------------------------------------
        // 2) REST-APIs an den FileController
        // ------------------------------------------------
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_jwtToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
            return client;
        }

        public async Task<List<DocumentResponse>> GetDocumentsByChannelAsync(int channelId)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/channel/{channelId}";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var docs = JsonConvert.DeserializeObject<List<DocumentResponse>>(responseBody);
                    return docs;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Laden der Dokumente: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<List<DocumentVersionResponse>> GetVersionsAsync(int documentId)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/{documentId}/versions";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var vers = JsonConvert.DeserializeObject<List<DocumentVersionResponse>>(responseBody);
                    return vers;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Laden der Versionen: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/create-document";
                    var json = JsonConvert.SerializeObject(request);
                    var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var doc = JsonConvert.DeserializeObject<DocumentResponse>(responseBody);
                    return doc;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Erstellen des Dokuments: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Öffnet ein FileDialog und lädt die Datei als neue Version hoch.
        /// </summary>
        public async Task<DocumentVersionResponse> UploadDocumentVersionAsync(int documentId)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return null;

            var filePath = ofd.FileName;

            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/{documentId}/upload";
                    using (var content = new MultipartFormDataContent())
                    {
                        using (var fileStream = System.IO.File.OpenRead(filePath))
                        {
                            var streamContent = new StreamContent(fileStream);
                            var fileName = System.IO.Path.GetFileName(filePath);
                            streamContent.Headers.ContentDisposition =
                                new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "\"file\"",
                                    FileName = fileName
                                };
                            content.Add(streamContent, "file", fileName);

                            var response = await client.PostAsync(url, content);
                            if (!response.IsSuccessStatusCode)
                            {
                                MessageBox.Show("Upload fehlgeschlagen");
                                return null;
                            }

                            var responseBody = await response.Content.ReadAsStringAsync();
                            var version = JsonConvert.DeserializeObject<DocumentVersionResponse>(responseBody);
                            return version;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Hochladen: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Öffnet FileDialog, erstellt ein neues Document und lädt direkt eine 1. Version hoch.
        /// </summary>
        public async Task<DocumentResponse> CreateAndUploadFileForChannel(int channelId)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return null;

            var filePath = ofd.FileName;
            var fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // Erst Document anlegen
            var createReq = new CreateDocumentRequest(fileNameNoExt, channelId);
            var doc = await CreateDocumentAsync(createReq);
            if (doc == null) return null;

            // Dann 1. Version hochladen
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/{doc.Id}/upload";
                    using (var content = new MultipartFormDataContent())
                    {
                        using (var fileStream = System.IO.File.OpenRead(filePath))
                        {
                            var streamContent = new StreamContent(fileStream);
                            var fileName = System.IO.Path.GetFileName(filePath);
                            streamContent.Headers.ContentDisposition =
                                new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "\"file\"",
                                    FileName = fileName
                                };
                            content.Add(streamContent, "file", fileName);

                            var response = await client.PostAsync(url, content);
                            if (!response.IsSuccessStatusCode)
                            {
                                MessageBox.Show("Upload fehlgeschlagen");
                                return null;
                            }

                            // Wir parsen das Result, brauchen es aber nicht mehr manuell in die Liste einfügen
                            var responseBody = await response.Content.ReadAsStringAsync();
                            var version = JsonConvert.DeserializeObject<DocumentVersionResponse>(responseBody);
                            if (version == null) return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Hochladen: {ex.Message}");
                    return null;
                }
            }

            return doc;
        }

        public async Task<bool> DownloadVersionAsync(int documentId, int versionNumber)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/{documentId}/version/{versionNumber}";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Fehler beim Download");
                        return false;
                    }

                    var contentDisposition = response.Content.Headers.ContentDisposition;
                    var fileName = contentDisposition?.FileNameStar ?? "download.bin";

                    var sfd = new SaveFileDialog { FileName = fileName };
                    if (sfd.ShowDialog() == true)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        System.IO.File.WriteAllBytes(sfd.FileName, bytes);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Download: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
