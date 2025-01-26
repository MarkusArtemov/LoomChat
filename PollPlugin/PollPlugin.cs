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
    /// (A) BaseUrl und Token kommen per Konstruktor.
    /// (B) Wir setzen den AccessTokenProvider, damit die Requests authentifiziert werden.
    /// (C) In Initialize() bauen wir die Verbindung auf und registrieren Event-Handler.
    /// </summary>
    public class PollPlugin : IPollPlugin
    {
        public string Name => "PollPlugin";

        // Events, um Veränderungen in der Umfrage an die Host-Applikation weiterzugeben.
        public event Action<string, List<string>> PollCreatedEvent;
        public event Action<string, Dictionary<string, int>> PollUpdatedEvent;
        public event Action<string> PollClosedEvent;
        public event Action<string> PollDeletedEvent;

        private readonly string _baseUrl;
        private readonly string _token;
        private HubConnection _connection;

        /// <summary>
        /// Konstruktor nimmt BaseUrl und JWT-Token an.
        /// </summary>
        public PollPlugin(string baseUrl, string token)
        {
            _baseUrl = baseUrl;
            _token = token;
        }

        /// <summary>
        /// Wird aufgerufen, wenn das Plugin geladen wird.
        /// Stellt die Verbindung mit dem PollHub her und registriert sämtliche Event-Handler.
        /// </summary>
        public void Initialize()
        {
            // HubConnection erstellen und das JWT als Bearer-Token mitschicken
            _connection = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/pollHub", options =>
                {
                    // AccessTokenProvider sorgt dafür, dass bei jedem Request das Token angehängt wird
                    options.AccessTokenProvider = () =>
                    {
                        return Task.FromResult(_token);
                    };
                })
                .Build();

            // Serverseitige Events verarbeiten
            _connection.On<string, List<string>>("PollCreated", (title, options) =>
            {
                Console.WriteLine($"[PollPlugin] PollCreated: {title} => {string.Join(", ", options)}");
                PollCreatedEvent?.Invoke(title, options);
            });

            _connection.On<string, Dictionary<string, int>>("PollUpdated", (title, results) =>
            {
                Console.WriteLine($"[PollPlugin] PollUpdated: '{title}'");
                PollUpdatedEvent?.Invoke(title, results);
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

            // Verbindung asynchron starten
            _ = StartConnectionAsync();
        }

        /// <summary>
        /// Interner Helfer, um die Verbindung mit dem Hub zu starten.
        /// </summary>
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

        /// <summary>
        /// Eine neue Umfrage anlegen.
        /// </summary>
        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            if (_connection == null)
            {
                Console.WriteLine("[PollPlugin] Connection is null, cannot CreatePoll.");
                return;
            }
            // Wichtig: channelId zuerst, dann title, dann options!
            await _connection.InvokeAsync("CreatePoll", channelId, title, options);
        }

        /// <summary>
        /// Abstimmung für eine bestimmte Option.
        /// </summary>
        public async Task Vote(string title, string option)
        {
            if (_connection == null)
            {
                Console.WriteLine("[PollPlugin] Connection is null, cannot Vote.");
                return;
            }
            await _connection.InvokeAsync("Vote", title, option);
        }

        /// <summary>
        /// Eine Umfrage schließen (z.B. keine weiteren Votes zulassen).
        /// </summary>
        public async Task ClosePoll(string title)
        {
            if (_connection == null)
            {
                Console.WriteLine("[PollPlugin] Connection is null, cannot ClosePoll.");
                return;
            }
            await _connection.InvokeAsync("ClosePoll", title);
        }

        /// <summary>
        /// Eine Umfrage löschen (z. B. wenn sie nicht mehr benötigt wird).
        /// </summary>
        public async Task DeletePoll(string title)
        {
            if (_connection == null)
            {
                Console.WriteLine("[PollPlugin] Connection is null, cannot DeletePoll.");
                return;
            }
            await _connection.InvokeAsync("DeletePoll", title);
        }
    }
}
