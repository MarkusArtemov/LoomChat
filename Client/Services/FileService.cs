using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Windows;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.dtos;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class FileService
    {
        private readonly string _baseUrl;
        private readonly string _jwtToken;
        private HubConnection _hubConnection;

        // Events für Echtzeit
        public event Action<DocumentResponse> OnDocumentCreated;
        public event Action<DocumentVersionResponse> OnVersionCreated;

        public FileService(string baseUrl, string jwtToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _jwtToken = jwtToken;
        }

        // -------------------------------------
        // 1) SIGNALR: FileHub
        // -------------------------------------
        public async Task InitializeFileHubAsync()
        {

            
            int port = await GetFileServicePort();
            var hubUrl = $"{_baseUrl}:{port}/fileHub";
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_jwtToken);
                })
                .Build();

            _hubConnection.On<DocumentResponse>("DocumentCreated", doc =>
            {
                OnDocumentCreated?.Invoke(doc);
            });

            _hubConnection.On<DocumentVersionResponse>("VersionCreated", version =>
            {
                OnVersionCreated?.Invoke(version);
            });

            await _hubConnection.StartAsync();
        }

        private async Task<int> GetFileServicePort()
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/file/port";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Fehler beim Port-Fetch. HTTP-" + response.StatusCode);
                        return 0;
                    }
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var portResponse = JsonConvert.DeserializeObject<PortResponse>(responseBody);
                    if (portResponse == null)
                    {
                        MessageBox.Show("Port-Antwort war leer oder ungültig!");
                        return 0;
                    }
                    return portResponse.Port;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen des Chat-Ports: {ex.Message}");
                    return 0;
                }
            }
        }

        private HttpClient CreateHttpClientWithAuth()
        {
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_jwtToken))
            {
                // Standard Bearer header
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
            return client;
        }

        public async Task JoinFileChannel(int channelId)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("JoinChannel", channelId);
        }

        // -------------------------------------
        // 2) REST-API Calls
        // -------------------------------------
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
                    var url = $"{_baseUrl}/file/File/channel/{channelId}";
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
                    var url = $"{_baseUrl}/file/File/{documentId}/versions";
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
                    var url = $"{_baseUrl}/file/File/create-document";
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

        public async Task<DocumentVersionResponse> UploadDocumentVersionAsync(int documentId)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return null;

            var filePath = ofd.FileName;

            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/file/File/{documentId}/upload";
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

        public async Task<DocumentResponse> CreateAndUploadFileForChannel(int channelId)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return null;

            var filePath = ofd.FileName;
            var fileNameNoExt = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // 1) Document anlegen
            var createReq = new CreateDocumentRequest(fileNameNoExt, channelId);
            var doc = await CreateDocumentAsync(createReq);
            if (doc == null) return null;

            // 2) Erste Version hochladen
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/file/File/{doc.Id}/upload";
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
                    var url = $"{_baseUrl}/file/File/{documentId}/version/{versionNumber}";
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

        // Einzelversion löschen
        public async Task<bool> DeleteVersionAsync(int documentId, int versionNumber)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/file/File/{documentId}/version/{versionNumber}";
                    var response = await client.DeleteAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Fehler beim Löschen der Version");
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen der Version: {ex.Message}");
                    return false;
                }
            }
        }

        // NEU: Komplettes Dokument löschen
        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/file/File/{documentId}";
                    var response = await client.DeleteAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Fehler beim Löschen des Dokuments");
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Löschen des Dokuments: {ex.Message}");
                    return false;
                }
            }
        }
    }
}
