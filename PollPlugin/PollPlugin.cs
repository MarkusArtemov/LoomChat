using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Common.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hsfl.LoomChat.PollPlugin
{
    public class PollPlugin : IPollPlugin
    {
        public string Name => "PollPlugin";

        // Events
        public event Action<string, List<string>> PollCreatedEvent;
        public event Action<string, Dictionary<string, int>> PollUpdatedEvent;
        public event Action<string> PollClosedEvent;
        public event Action<string> PollDeletedEvent;

        private readonly string _baseUrl;
        private readonly string _token;
        private HubConnection _connection;

        public PollPlugin(string baseUrl, string token)
        {
            _baseUrl = baseUrl;
            _token = token;
        }

        // NEU: async => wir warten tatsächlich auf StartAsync()
        public async Task Initialize()
        {
            // HubConnection erstellen
            _connection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/chatHub", options =>
                {
                    // JWT
                    options.AccessTokenProvider = () => Task.FromResult(_token);
                })
                .Build();

            // Event-Handler für Server-RPC:
            _connection.On<string, List<string>>("PollCreated", (title, options) =>
            {
                Console.WriteLine($"[PollPlugin] PollCreated: {title}");
                PollCreatedEvent?.Invoke(title, options);
            });
            _connection.On<string, Dictionary<string, int>>("PollUpdated", (title, results) =>
            {
                Console.WriteLine($"[PollPlugin] PollUpdated: {title}");
                PollUpdatedEvent?.Invoke(title, results);
            });
            _connection.On<string>("PollClosed", (title) =>
            {
                Console.WriteLine($"[PollPlugin] PollClosed: {title}");
                PollClosedEvent?.Invoke(title);
            });
            _connection.On<string>("PollDeleted", (title) =>
            {
                Console.WriteLine($"[PollPlugin] PollDeleted: {title}");
                PollDeletedEvent?.Invoke(title);
            });

            // VERBINDUNG wirklich herstellen:
            await _connection.StartAsync();
            Console.WriteLine("[PollPlugin] Connected to chatHub.");
        }

        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                Console.WriteLine("[PollPlugin] Connection not active, cannot CreatePoll!");
                return;
            }
            await _connection.InvokeAsync("CreatePoll", channelId, title, options);
        }

        public async Task Vote(string title, string option)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                Console.WriteLine("[PollPlugin] Connection not active, cannot Vote!");
                return;
            }
            await _connection.InvokeAsync("Vote", title, option);
        }

        public async Task ClosePoll(string title)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                Console.WriteLine("[PollPlugin] Connection not active, cannot ClosePoll!");
                return;
            }
            await _connection.InvokeAsync("ClosePoll", title);
        }

        public async Task DeletePoll(string title)
        {
            if (_connection == null || _connection.State != HubConnectionState.Connected)
            {
                Console.WriteLine("[PollPlugin] Connection not active, cannot DeletePoll!");
                return;
            }
            await _connection.InvokeAsync("DeletePoll", title);
        }
    }
}
