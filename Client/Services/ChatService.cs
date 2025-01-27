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

namespace De.Hsfl.LoomChat.Client.Services
{
    internal class ChatService
    {
        private HubConnection _hubConnection;
        private string _jwtToken;

        // ========== CHAT-EVENTS ==========
        // Fired when a new text message arrives
        public event Action<int, int, string, string, DateTime> OnMessageReceived;

        // Fired when the server sends the entire channel history as List<ChatMessageDto>
        public event Action<int, List<ChatMessageDto>> OnChannelHistoryReceived;

        // ========== POLL-EVENTS ==========
        public event Action<string, List<string>> OnPollCreated;
        public event Action<string, Dictionary<string, int>> OnPollUpdated;
        public event Action<string> OnPollClosed;
        public event Action<string> OnPollDeleted;
        public event Action<string> OnPollError;

        // ========== INITIALISIERUNG ==========
        public async Task InitializeSignalRAsync(string jwtToken)
        {
            _jwtToken = jwtToken;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5115/chatHub", options =>
                {
                    // JWT
                    options.AccessTokenProvider = () => Task.FromResult(jwtToken);
                })
                .Build();

            // (A) Single new text message
            _hubConnection.On<int, int, string, string, DateTime>(
                "ReceiveChannelMessage",
                (channelId, senderUserId, senderName, content, sentAt) =>
                {
                    OnMessageReceived?.Invoke(channelId, senderUserId, senderName, content, sentAt);
                }
            );

            // (B) ChannelHistory => List<ChatMessageDto>
            _hubConnection.On<int, List<ChatMessageDto>>(
                "ChannelHistory",
                (channelId, msgList) =>
                {
                    OnChannelHistoryReceived?.Invoke(channelId, msgList);
                }
            );

            // ========== POLL-EVENTS ==========
            // Broadcast vom Server => an diese Methoden gebunden
            _hubConnection.On<string, List<string>>(
                "PollCreated",
                (title, options) =>
                {
                    OnPollCreated?.Invoke(title, options);
                }
            );

            _hubConnection.On<string, Dictionary<string, int>>(
                "PollUpdated",
                (title, results) =>
                {
                    OnPollUpdated?.Invoke(title, results);
                }
            );

            _hubConnection.On<string>(
                "PollClosed",
                (title) =>
                {
                    OnPollClosed?.Invoke(title);
                }
            );

            _hubConnection.On<string>(
                "PollDeleted",
                (title) =>
                {
                    OnPollDeleted?.Invoke(title);
                }
            );

            _hubConnection.On<string>(
                "PollError",
                (errorMsg) =>
                {
                    OnPollError?.Invoke(errorMsg);
                }
            );

            await _hubConnection.StartAsync();
        }

        public async Task DisconnectSignalRAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        private HttpClient CreateHttpClientWithAuth()
        {
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_jwtToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _jwtToken);
            }
            return client;
        }

        // ========== SIGNALR: CHANNEL JOIN / MESSAGES ==========

        // SignalR: join channel
        public async Task JoinChannel(int channelId)
        {
            if (_hubConnection == null) return;
            var req = new JoinChannelRequest(channelId);
            await _hubConnection.InvokeAsync("JoinChannel", req);
        }

        // SignalR: send text message
        public async Task SendMessageSignalR(int channelId, int userId, string messageText)
        {
            if (_hubConnection == null) return;
            var req = new SendMessageRequest(userId, messageText, channelId);
            await _hubConnection.InvokeAsync("SendMessageToChannel", req);
        }

        // ========== SIGNALR: POLL-METHODEN ==========

        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("CreatePoll", channelId, title, options);
        }

        public async Task VotePoll(string title, string option)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("Vote", title, option);
        }

        public async Task ClosePoll(string title)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("ClosePoll", title);
        }

        public async Task DeletePoll(string title)
        {
            if (_hubConnection == null) return;
            await _hubConnection.InvokeAsync("DeletePoll", title);
        }

        // ========== REST-PART (zum Laden von Channels/Users/DMs) ==========

        public async Task<List<ChannelDto>> LoadChannels(GetChannelsRequest request)
        {
            using (var client = CreateHttpClientWithAuth())
            {
                try
                {
                    var url = "http://localhost:5115/Chat/channels";
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
                    MessageBox.Show($"Fehler beim Laden der Channels: {ex.Message}");
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
                    var url = "http://localhost:5115/Chat/dms";
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
                    MessageBox.Show($"Fehler beim Laden der DMs: {ex.Message}");
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
                    var url = "http://localhost:5232/Auth/users";
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
                    MessageBox.Show($"Fehler beim Laden der User: {ex.Message}");
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
                    var url = "http://localhost:5115/Chat/openDm";
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
                    var url = "http://localhost:5115/Chat/createChannel";
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
