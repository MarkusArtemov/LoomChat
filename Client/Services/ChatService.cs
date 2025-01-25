using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using Microsoft.AspNetCore.Http.Connections;
using De.Hsfl.LoomChat.Common.dtos;

namespace De.Hsfl.LoomChat.Client.Services
{
    /// <summary>
    /// Provides REST for channel/user management and SignalR for live messaging
    /// </summary>
    internal class ChatService
    {
        private HubConnection _hubConnection;

        // We'll store the JWT token here once we get it in InitializeSignalRAsync
        private string _jwtToken;

        // Fires when a new message arrives
        public event Action<int, int, string, string, DateTime> OnMessageReceived;

        // Fires when the server sends the entire history for a channel
        public event Action<int, List<ChatMessageResponse>> OnChannelHistoryReceived;

        /// <summary>
        /// Sets up SignalR connection with JWT (and also stores it for REST calls).
        /// </summary>
        public async Task InitializeSignalRAsync(string jwtToken)
        {
            // Save this token for subsequent REST calls
            _jwtToken = jwtToken;
            int port = await GetChatServicePort();
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:{port}/chatHub", options =>
                {
                    // For SignalR, we put the JWT as "access_token" in the query
                    options.Transports = HttpTransportType.WebSockets;
                    options.AccessTokenProvider = () => Task.FromResult(jwtToken);
                })
                .Build();

            // (A) single new messages
            _hubConnection.On<int, int, string, string, DateTime>(
                "ReceiveChannelMessage",
                (channelId, senderUserId, senderName, content, sentAt) =>
                {
                    OnMessageReceived?.Invoke(channelId, senderUserId, senderName, content, sentAt);
                }
            );

            // (B) channel history
            //     server sends "ChannelHistory", channelId + List<ChatMessageResponse>
            _hubConnection.On<int, List<ChatMessageResponse>>(
                "ChannelHistory",
                (channelId, msgList) =>
                {
                    OnChannelHistoryReceived?.Invoke(channelId, msgList);
                }
            );

            await _hubConnection.StartAsync();
        }

        private async Task<int> GetChatServicePort()
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/chat/port";
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

        /// <summary>
        /// Helper method: creates an HttpClient that sets the Authorization header if we have a JWT.
        /// </summary>
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

        /// <summary>
        /// Joins a channel group => triggers "ChannelHistory" from server
        /// </summary>
        public async Task JoinChannel(int channelId)
        {
            if (_hubConnection == null) return;
            var req = new JoinChannelRequest(channelId);
            await _hubConnection.InvokeAsync("JoinChannel", req);
        }

        /// <summary>
        /// Leaves the channel
        /// </summary>
        public async Task LeaveChannel(int channelId, bool removeMembership)
        {
            if (_hubConnection == null) return;
            var req = new LeaveChannelRequest(channelId, removeMembership);
            await _hubConnection.InvokeAsync("LeaveChannel", req);
        }

        /// <summary>
        /// Sends a message via SignalR to all in channel
        /// </summary>
        public async Task SendMessageSignalR(int channelId, int userId, string messageText)
        {
            if (_hubConnection == null) return;
            var req = new SendMessageRequest(userId, messageText, channelId);
            await _hubConnection.InvokeAsync("SendMessageToChannel", req);
        }

        /// <summary>
        /// Disconnects from hub
        /// </summary>
        public async Task DisconnectSignalRAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        // ========== REST PART: minimal channels, users ==========

        public async Task<List<ChannelDto>> LoadChannels(GetChannelsRequest request)
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/chat/Chat/channels";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
                    var response = await client.PostAsync(url, jsonContent);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<GetChannelsResponse>(responseBody);
                    return responseObj.Channels;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen der Channels: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<List<ChannelDto>> LoadDirectChannels(GetDirectChannelsRequest request)
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/chat/Chat/dms";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
                    var response = await client.PostAsync(url, jsonContent);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<GetDirectChannelsResponse>(responseBody);
                    return responseObj.Channels;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen der DMs: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<List<User>> LoadAllUsers(GetUsersRequest request)
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/chat/Chat/users";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
                    var response = await client.PostAsync(url, jsonContent);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<GetUsersResponse>(responseBody);
                    return responseObj.Users;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Fetchen der User: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<ChannelDto> OpenChatWithUser(OpenChatWithUserRequest request)
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/chat/Chat/openDm";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
                    var response = await client.PostAsync(url, jsonContent);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<OpenChatWithUserResponse>(responseBody);
                    return responseObj.Channel;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Öffnen der Direktnachricht: {ex.Message}");
                    return null;
                }
            }
        }

        public async Task<ChannelDto> CreateNewChannel(CreateChannelRequest request)
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost/chat/Chat/createChannel";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
                    var response = await client.PostAsync(url, jsonContent);
                    if (!response.IsSuccessStatusCode) return null;

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<CreateChannelResponse>(responseBody);
                    return responseObj.Channel;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Erstellen des Channels: {ex.Message}");
                    return null;
                }
            }
        }
    }
}
