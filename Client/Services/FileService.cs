using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class FileService
    {
        private readonly string _baseUrl;
        private readonly string _jwtToken;

        public FileService(string baseUrl, string jwtToken)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _jwtToken = jwtToken;
        }

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

        public async Task<DocumentResponse> CreateDocumentAsync(CreateDocumentRequest request)
        {
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/create-document";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
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

        public async Task<DocumentVersionResponse> UploadVersionAsync(int documentId)
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
                        var fileStream = System.IO.File.OpenRead(filePath);
                        var streamContent = new StreamContent(fileStream);
                        var fileName = System.IO.Path.GetFileName(filePath);
                        streamContent.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("form-data")
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
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Hochladen: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<DocumentResponse> CreateAndUploadDocumentAsync(int channelId)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != true) return null;
            var filePath = ofd.FileName;
            var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var createReq = new CreateDocumentRequest(name, channelId);
            var doc = await CreateDocumentAsync(createReq);
            if (doc == null) return null;
            using (var client = CreateHttpClient())
            {
                try
                {
                    var url = $"{_baseUrl}/File/{doc.Id}/upload";
                    using (var content = new MultipartFormDataContent())
                    {
                        var fileStream = System.IO.File.OpenRead(filePath);
                        var streamContent = new StreamContent(fileStream);
                        var fileName = System.IO.Path.GetFileName(filePath);
                        streamContent.Headers.ContentDisposition =
                            new ContentDispositionHeaderValue("form-data")
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
            var client = CreateHttpClient();
            try
            {
                var url = $"{_baseUrl}/File/{documentId}/version/{versionNumber}";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Fehler beim Download");
                    return false;
                }
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileNameStar ?? "download.bin";
                var sfd = new SaveFileDialog
                {
                    FileName = fileName
                };
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
