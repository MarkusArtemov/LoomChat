using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Common.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace De.Hsfl.LoomChat.PollPlugin
{
    /// <summary>
    /// Clientseitiges PollPlugin, das sich via SignalR-Client zu einem
    /// Server-Hub verbindet und Poll-Ereignisse verarbeitet.
    /// 
    /// (A) BaseUrl kommt per Konstruktor
    /// (B) Ereignisse signalisieren Poll-Aktionen an den Host
    /// </summary>
    public class PollPlugin : IChatPlugin
    {
        public string Name => "PollPlugin";


        public event Action<string, List<string>> PollCreatedEvent;
        public event Action<string, Dictionary<string, int>> PollUpdatedEvent;
        public event Action<string> PollClosedEvent;
        public event Action<string> PollDeletedEvent;

        private readonly string _baseUrl;
        private HubConnection _connection;

        public PollPlugin(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        /// <summary>
        /// Wird beim Laden des Plugins aufgerufen.
        /// Hier bauen wir die SignalR-Connection zum Server auf.
        /// </summary>
        public void Initialize()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/pollHub")
                .Build();

            _connection.On<string, List<string>>("PollCreated", (title, options) =>
            {
                Console.WriteLine($"[PollPlugin] PollCreated: {title} = {string.Join(", ", options)}");
                PollCreatedEvent?.Invoke(title, options);
            });

            _connection.On<string, Dictionary<string, int>>("PollUpdated", (title, dict) =>
            {
                Console.WriteLine($"[PollPlugin] PollUpdated: '{title}'");
                PollUpdatedEvent?.Invoke(title, dict);
            });

            _connection.On<string>("PollClosed", (title) =>
            {
                Console.WriteLine($"[PollPlugin] PollClosed: '{title}'");
                PollClosedEvent?.Invoke(title);
            });

            _connection.On<string>("PollDeleted", (title) =>
            {
                Console.WriteLine($"[PollPlugin] PollDeleted: '{title}'");
                PollDeletedEvent?.Invoke(title);
            });

            _ = StartConnectionAsync();
        }

        private async Task StartConnectionAsync()
        {
            try
            {
                await _connection.StartAsync();
                Console.WriteLine("[PollPlugin] Connection established.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PollPlugin] Fehler beim Starten der Connection: {ex.Message}");
            }
        }

        public async Task CreatePoll(string title, List<string> options)
        {
            if (_connection == null) return;
            await _connection.InvokeAsync("CreatePoll", title, options);
        }

        public async Task Vote(string title, string option)
        {
            if (_connection == null) return;
            await _connection.InvokeAsync("Vote", title, option);
        }

        public async Task ClosePoll(string title)
        {
            if (_connection == null) return;
            await _connection.InvokeAsync("ClosePoll", title);
        }

        public async Task DeletePoll(string title)
        {
            if (_connection == null) return;
            await _connection.InvokeAsync("DeletePoll", title);
        }
    }
}
