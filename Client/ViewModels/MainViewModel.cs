using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Global;
using De.Hsfl.LoomChat.Client.Services;
using De.Hsfl.LoomChat.Client.Views;
using De.Hsfl.LoomChat.Common.Contracts;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ChatService _chatService;
        private readonly LoginService _loginService;
        private FileService _fileService;
        private PluginManager _pluginManager;
        private IPollPlugin _pollPlugin;

        // ------------------ Collections für Channels etc. ------------------
        public ObservableCollection<ChannelDto> OpenChats { get; set; }
        public ObservableCollection<ChannelDto> DirectMessages { get; set; }
        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<DocumentResponse> Documents { get; set; }
        public ObservableCollection<DocumentVersionResponse> Versions { get; set; }

        // ------------------ Ausgewählter Channel + Anzeige  ------------------
        private ChannelDto _selectedChannel;
        public ChannelDto SelectedChannel
        {
            get => _selectedChannel;
            set
            {
                _selectedChannel = value;
                ChatVisible = (_selectedChannel != null);
                OnPropertyChanged();
            }
        }

        private bool _chatVisible;
        public bool ChatVisible
        {
            get => _chatVisible;
            set
            {
                _chatVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChatNotVisible));
            }
        }
        public bool IsChatNotVisible => !ChatVisible;

        // ------------------ Neue Textnachricht  ------------------
        private string _newMessage;
        public string NewMessage
        {
            get => _newMessage;
            set { _newMessage = value; OnPropertyChanged(); }
        }

        // ------------------ Popup zum Channel-Erstellen  ------------------
        private bool _popupOpen;
        public bool PopupOpen
        {
            get => _popupOpen;
            set { _popupOpen = value; OnPropertyChanged(); }
        }

        private string _newChannelName;
        public string NewChannelName
        {
            get => _newChannelName;
            set { _newChannelName = value; OnPropertyChanged(); }
        }

        // ------------------ Poll-Popup  ------------------
        private bool _pollPopupOpen;
        public bool PollPopupOpen
        {
            get => _pollPopupOpen;
            set { _pollPopupOpen = value; OnPropertyChanged(); }
        }

        private string _pollTitle;
        public string PollTitle
        {
            get => _pollTitle;
            set { _pollTitle = value; OnPropertyChanged(); }
        }

        // Liste aller Antwortoptionen
        public ObservableCollection<string> PollOptions { get; set; }
        private string _newOptionText;
        public string NewOptionText
        {
            get => _newOptionText;
            set { _newOptionText = value; OnPropertyChanged(); }
        }

        // ------------------ Dokumentauswahl  ------------------
        private DocumentResponse _selectedDocument;
        public DocumentResponse SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                _selectedDocument = value;
                OnPropertyChanged();
                if (_selectedDocument != null)
                {
                    LoadVersionsAsync(_selectedDocument.Id);
                }
            }
        }

        private DocumentVersionResponse _selectedVersion;
        public DocumentVersionResponse SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                _selectedVersion = value;
                OnPropertyChanged();
            }
        }

        // ------------------ Commands  ------------------
        public ICommand LogoutCommand { get; }
        public ICommand OpenPopupCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand CreateChannelCommand { get; }
        public ICommand SendMessageCommand { get; }

        // FileService
        public ICommand CreateDocumentCommand { get; }
        public ICommand UploadVersionCommand { get; }
        public ICommand DownloadVersionCommand { get; }
        public ICommand DeleteVersionCommand { get; }
        public ICommand DeleteDocumentCommand { get; }

        // PollPlugin laden
        public ICommand LoadPollPluginCommand { get; }

        // Poll-Popup
        public ICommand OpenPollPopupCommand { get; }
        public ICommand ClosePollPopupCommand { get; }
        public ICommand AddPollOptionCommand { get; }

        // Poll-spezifische
        public ICommand CreatePollCommand { get; }
        public ICommand VotePollCommand { get; }
        public ICommand ClosePollCommand { get; }
        public ICommand DeletePollCommand { get; }

        // ------------------ Konstruktor  ------------------
        public MainViewModel()
        {
            _loginService = new LoginService();
            _chatService = new ChatService();

            // Collections anlegen
            OpenChats = new ObservableCollection<ChannelDto>();
            DirectMessages = new ObservableCollection<ChannelDto>();
            Users = new ObservableCollection<User>();
            Documents = new ObservableCollection<DocumentResponse>();
            Versions = new ObservableCollection<DocumentVersionResponse>();

            PollOptions = new ObservableCollection<string>();

            // Basic Commands
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenPopupCommand = new RelayCommand(_ => ExecuteOpenPopup());
            ClosePopupCommand = new RelayCommand(_ => ExecuteClosePopup());
            CreateChannelCommand = new RelayCommand(_ => ExecuteCreateChannel());
            SendMessageCommand = new RelayCommand(_ => ExecuteSendMessage());

            // FileService commands
            CreateDocumentCommand = new RelayCommand(_ => ExecuteCreateDocument());
            UploadVersionCommand = new RelayCommand(_ => ExecuteUploadVersion());
            DownloadVersionCommand = new RelayCommand(_ => ExecuteDownloadVersion());
            DeleteVersionCommand = new RelayCommand(_ => ExecuteDeleteVersion());
            DeleteDocumentCommand = new RelayCommand(_ => ExecuteDeleteDocument());

            // PollPlugin commands
            LoadPollPluginCommand = new RelayCommand(_ => ExecuteLoadPollPlugin());
            OpenPollPopupCommand = new RelayCommand(_ => ExecuteOpenPollPopup());
            ClosePollPopupCommand = new RelayCommand(_ => ExecuteClosePollPopup());
            AddPollOptionCommand = new RelayCommand(_ => ExecuteAddPollOption());

            CreatePollCommand = new RelayCommand(_ => ExecuteCreatePoll());
            VotePollCommand = new RelayCommand(param => ExecuteVotePoll(param));
            ClosePollCommand = new RelayCommand(_ => ExecuteClosePoll());
            DeletePollCommand = new RelayCommand(_ => ExecuteDeletePoll());
        }

        // ------------------ Daten laden  ------------------
        public async void LoadAsyncData()
        {
            if (SessionStore.User == null)
            {
                MessageBox.Show("Keine Benutzerdaten vorhanden.");
                return;
            }
            string jwt = SessionStore.JwtToken;
            if (string.IsNullOrWhiteSpace(jwt))
            {
                MessageBox.Show("Kein JWT-Token vorhanden.");
                return;
            }

            // SignalR initialisieren
            await _chatService.InitializeSignalRAsync(jwt);

            // FileService
            _fileService = new FileService("https://localhost:7021/", jwt);
            await _fileService.InitializeFileHubAsync();

            // FileService-Ereignisse
            _fileService.OnDocumentCreated += doc =>
            {
                if (doc.ChannelId == SelectedChannel?.Id)
                {
                    Application.Current.Dispatcher.Invoke(() => Documents.Add(doc));
                }
            };
            _fileService.OnVersionCreated += version =>
            {
                if (SelectedDocument != null && SelectedDocument.Id == version.DocumentId)
                {
                    Application.Current.Dispatcher.Invoke(() => Versions.Add(version));
                }
            };

            // Chat-Ereignisse
            _chatService.OnMessageReceived += (channelId, senderUserId, senderName, content, sentAt) =>
            {
                // Wir tun so, als wäre das ein reiner Text
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var ch = FindChannel(channelId);
                    if (ch != null)
                    {
                        // Erzeugt ein ChatMessageDto mit Type=Text
                        var msg = new ChatMessageDto
                        {
                            ChannelId = channelId,
                            SenderUserId = senderUserId,
                            SentAt = sentAt,
                            Type = MessageType.Text,
                            Content = content
                        };
                        ch.ChatMessages.Add(msg);

                        // Sort
                        ch.ChatMessages = new ObservableCollection<ChatMessageDto>(
                            ch.ChatMessages.OrderBy(m => m.SentAt)
                        );
                    }
                });
            };
            _chatService.OnChannelHistoryReceived += (channelId, msgList) =>
            {
                // msgList => List<ChatMessageResponse> / oder ChatMessageDto
                // Hier wandeln wir sie 1:1 in ChatMessageDto (ggf. we already have the same type)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var ch = FindChannel(channelId);
                    if (ch != null)
                    {
                        var sorted = msgList.OrderBy(m => m.SentAt).ToList();
                        ch.ChatMessages.Clear();

                        foreach (var m in sorted)
                        {
                            // Falls serverseitig bereits ChatMessageDto => 
                            // wir könnten m direkt adden.
                            // Hier als Bsp 1:1:
                            var msg = new ChatMessageDto
                            {
                                Id = m.Id,
                                ChannelId = m.ChannelId,
                                SenderUserId = m.SenderUserId,
                                SentAt = m.SentAt,
                                Type = m.Type,         // Ob Poll/Text
                                Content = m.Content,
                                PollId = m.PollId,
                                IsClosed = m.IsClosed,
                                PollTitle = m.PollTitle,
                                PollOptions = m.PollOptions ?? new List<string>()
                            };
                            ch.ChatMessages.Add(msg);
                        }
                    }
                });
            };

            // Channels laden
            var channels = await _chatService.LoadChannels(new GetChannelsRequest(SessionStore.User.Id));
            if (channels != null)
            {
                foreach (var c in channels)
                {
                    c.ChatMessages = new ObservableCollection<ChatMessageDto>();
                    OpenChats.Add(c);
                }
            }

            // DMs laden
            var dms = await _chatService.LoadDirectChannels(new GetDirectChannelsRequest(SessionStore.User.Id));
            if (dms != null)
            {
                foreach (var d in dms)
                {
                    d.ChatMessages = new ObservableCollection<ChatMessageDto>();
                    DirectMessages.Add(d);
                }
            }

            // Users laden
            var userList = await _chatService.LoadAllUsers(new GetUsersRequest());
            if (userList != null)
            {
                foreach (var u in userList)
                {
                    Users.Add(u);
                }
            }
        }

        // ------------------ Hilfsmethode  ------------------
        private ChannelDto FindChannel(int channelId)
        {
            return OpenChats.FirstOrDefault(x => x.Id == channelId)
                ?? DirectMessages.FirstOrDefault(x => x.Id == channelId);
        }

        // ------------------ Basic (Logout, Channel)  ------------------
        private void ExecuteLogout()
        {
            SessionStore.User = null;
            SessionStore.JwtToken = null;
            MainWindow.NavigationService.Navigate(new LoginView());
        }

        private void ExecuteOpenPopup() => PopupOpen = true;
        private void ExecuteClosePopup() => PopupOpen = false;

        private async void ExecuteCreateChannel()
        {
            PopupOpen = false;
            if (string.IsNullOrWhiteSpace(NewChannelName) || NewChannelName.Length < 4)
            {
                MessageBox.Show("Der Channelname muss mind. 4 Zeichen lang sein.");
                return;
            }
            var req = new CreateChannelRequest(SessionStore.User.Id, NewChannelName);
            var dto = await _chatService.CreateNewChannel(req);
            if (dto != null)
            {
                dto.ChatMessages = new ObservableCollection<ChatMessageDto>();
                OpenChats.Add(dto);
            }
        }

        private async void ExecuteSendMessage()
        {
            if (SelectedChannel == null || string.IsNullOrWhiteSpace(NewMessage)) return;
            int userId = SessionStore.User.Id;
            await _chatService.SendMessageSignalR(SelectedChannel.Id, userId, NewMessage);
            NewMessage = "";
        }

        public async void ChannelClicked(ChannelDto channel)
        {
            SelectedChannel = channel;
            if (channel != null)
            {
                await _chatService.JoinChannel(channel.Id);
                await LoadDocumentsForChannel(channel.Id);
                await _fileService.JoinFileChannel(channel.Id);
            }
        }

        public async void UserClicked(User user)
        {
            if (user == null) return;
            var chan = await _chatService.OpenChatWithUser(new OpenChatWithUserRequest(SessionStore.User.Id, user.Id));
            if (chan != null)
            {
                chan.ChatMessages = new ObservableCollection<ChatMessageDto>();
                var existing = DirectMessages.FirstOrDefault(c => c.Id == chan.Id);
                if (existing == null)
                {
                    DirectMessages.Add(chan);
                }
                else
                {
                    DirectMessages.Remove(existing);
                    DirectMessages.Insert(0, chan);
                }
                ChannelClicked(chan);
            }
        }

        // ------------------ Dokumente  ------------------
        private async Task LoadDocumentsForChannel(int channelId)
        {
            Documents.Clear();
            Versions.Clear();
            if (_fileService == null) return;

            var docs = await _fileService.GetDocumentsByChannelAsync(channelId);
            if (docs != null)
            {
                foreach (var d in docs)
                {
                    Documents.Add(d);
                }
            }
        }

        private async void LoadVersionsAsync(int documentId)
        {
            if (_fileService == null) return;
            Versions.Clear();

            var vers = await _fileService.GetVersionsAsync(documentId);
            if (vers != null)
            {
                foreach (var v in vers)
                {
                    Versions.Add(v);
                }
            }
        }

        private async void ExecuteCreateDocument()
        {
            if (SelectedChannel == null)
            {
                MessageBox.Show("Kein Channel gewählt");
                return;
            }
            if (_fileService == null) return;

            await _fileService.CreateAndUploadFileForChannel(SelectedChannel.Id);
        }

        private async void ExecuteUploadVersion()
        {
            if (SelectedDocument == null || _fileService == null) return;
            await _fileService.UploadDocumentVersionAsync(SelectedDocument.Id);
        }

        private async void ExecuteDownloadVersion()
        {
            if (_fileService == null || SelectedDocument == null || SelectedVersion == null) return;
            await _fileService.DownloadVersionAsync(SelectedDocument.Id, SelectedVersion.VersionNumber);
        }

        private async void ExecuteDeleteVersion()
        {
            if (_fileService == null || SelectedDocument == null || SelectedVersion == null) return;
            var ok = await _fileService.DeleteVersionAsync(SelectedDocument.Id, SelectedVersion.VersionNumber);
            if (ok)
            {
                Versions.Remove(SelectedVersion);
            }
        }

        private async void ExecuteDeleteDocument()
        {
            if (_fileService == null || SelectedDocument == null) return;
            var ok = await _fileService.DeleteDocumentAsync(SelectedDocument.Id);
            if (ok)
            {
                Documents.Remove(SelectedDocument);
                Versions.Clear();
            }
        }

        // ------------------ PollPlugin / Umfrage  ------------------
        private async void ExecuteLoadPollPlugin()
        {
            try
            {
                if (_pluginManager == null)
                    _pluginManager = new PluginManager("http://localhost:5115");

                var baseUrl = "http://localhost:5115";
                var token = SessionStore.JwtToken;

                var plugin = await _pluginManager.DownloadAndLoadPluginAsync("PollPlugin", baseUrl, token);
                _pollPlugin = plugin as IPollPlugin;
                if (_pollPlugin == null)
                {
                    MessageBox.Show("Das geladene Plugin ist kein IPollPlugin.");
                    return;
                }

                // Beispiel-Ereignisse
                _pollPlugin.PollCreatedEvent += (title, options) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (SelectedChannel == null) return;

                        // Fügen wir eine Poll-Nachricht in die UI ein
                        var pollMsg = new ChatMessageDto
                        {
                            ChannelId = SelectedChannel.Id,
                            SenderUserId = SessionStore.User.Id,
                            SentAt = DateTime.Now,
                            Type = MessageType.Poll,
                            PollTitle = title,
                            PollOptions = options.ToList()
                        };
                        SelectedChannel.ChatMessages.Add(pollMsg);
                    });
                };

                _pollPlugin.PollUpdatedEvent += (title, results) =>
                {
                    // Dict<string,int>
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (SelectedChannel == null) return;
                        var pollMsg = SelectedChannel.ChatMessages
                            .FirstOrDefault(m => m.Type == MessageType.Poll
                                              && m.PollTitle == title);
                        if (pollMsg != null)
                        {
                            // Bsp: Umbau in "Option (X Votes)"
                            var newList = new List<string>();
                            foreach (var kvp in results)
                            {
                                newList.Add($"{kvp.Key} ({kvp.Value} Votes)");
                            }
                            pollMsg.PollOptions = newList;
                        }
                    });
                };

                MessageBox.Show("PollPlugin erfolgreich geladen!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Plugins: {ex.Message}");
            }
        }

        private void ExecuteOpenPollPopup()
        {
            PollTitle = "";
            PollOptions.Clear();
            NewOptionText = "";
            PollPopupOpen = true;
        }

        private void ExecuteClosePollPopup()
        {
            PollPopupOpen = false;
        }

        private void ExecuteAddPollOption()
        {
            if (!string.IsNullOrWhiteSpace(NewOptionText))
            {
                PollOptions.Add(NewOptionText);
                NewOptionText = "";
            }
        }

        private async void ExecuteCreatePoll()
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin ist nicht geladen!");
                return;
            }
            if (SelectedChannel == null)
            {
                MessageBox.Show("Bitte zuerst einen Channel auswählen.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PollTitle))
            {
                MessageBox.Show("Bitte einen Umfragetitel eingeben.");
                return;
            }
            if (PollOptions.Count == 0)
            {
                MessageBox.Show("Bitte mindestens eine Antwortoption hinzufügen.");
                return;
            }

            try
            {
                int channelId = SelectedChannel.Id;
                await _pollPlugin.CreatePoll(channelId, PollTitle, PollOptions.ToList());
                MessageBox.Show($"Umfrage '{PollTitle}' wurde erstellt!");
                PollPopupOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei CreatePoll: " + ex.Message);
            }
        }

        private async void ExecuteVotePoll(object param)
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin ist nicht geladen!");
                return;
            }
            string chosenOption = param as string;
            if (string.IsNullOrEmpty(chosenOption))
            {
                MessageBox.Show("Keine Option ausgewählt.");
                return;
            }

            try
            {
                // Passenden Poll-Nachricht suchen, z.B. 
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll);
                if (pollMsg == null)
                {
                    MessageBox.Show("Keine Poll-Nachricht gefunden.");
                    return;
                }

                // PollId => string
                await _pollPlugin.Vote(pollMsg.PollId.ToString(), chosenOption);

                MessageBox.Show($"Vote abgegeben für '{chosenOption}'!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei VotePoll: " + ex.Message);
            }
        }


        private async void ExecuteClosePoll()
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin ist nicht geladen!");
                return;
            }
            try
            {
                if (SelectedChannel == null) return;
                var pollMsg = SelectedChannel.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll);
                if (pollMsg == null) return;

                await _pollPlugin.ClosePoll(pollMsg.PollId.ToString());
                MessageBox.Show("Umfrage geschlossen!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei ClosePoll: " + ex.Message);
            }
        }

        private async void ExecuteDeletePoll()
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin ist nicht geladen!");
                return;
            }
            try
            {
                if (SelectedChannel == null) return;
                var pollMsg = SelectedChannel.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll);
                if (pollMsg == null) return;

                await _pollPlugin.DeletePoll(pollMsg.PollId.ToString());
                MessageBox.Show("Umfrage gelöscht!");

                SelectedChannel.ChatMessages.Remove(pollMsg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei DeletePoll: " + ex.Message);
            }
        }
    }
}
