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
        private ChatService _chatService;
        private FileService _fileService;

        public ObservableCollection<ChannelDto> OpenChats { get; set; }
        public ObservableCollection<ChannelDto> DirectMessages { get; set; }
        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<DocumentResponse> Documents { get; set; }
        public ObservableCollection<DocumentVersionResponse> Versions { get; set; }

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

        private string _newMessage;
        public string NewMessage
        {
            get => _newMessage;
            set { _newMessage = value; OnPropertyChanged(); }
        }

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
        public ObservableCollection<string> PollOptions { get; set; }
        private string _newOptionText;
        public string NewOptionText
        {
            get => _newOptionText;
            set { _newOptionText = value; OnPropertyChanged(); }
        }

        private bool _isPollPluginLoaded;
        public bool IsPollPluginLoaded
        {
            get => _isPollPluginLoaded;
            set { _isPollPluginLoaded = value; OnPropertyChanged(); }
        }

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
            set { _selectedVersion = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand LogoutCommand { get; }
        public ICommand OpenPopupCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand CreateChannelCommand { get; }
        public ICommand SendMessageCommand { get; }

        public ICommand CreateDocumentCommand { get; }
        public ICommand UploadVersionCommand { get; }
        public ICommand DownloadVersionCommand { get; }
        public ICommand DeleteVersionCommand { get; }
        public ICommand DeleteDocumentCommand { get; }

        // Poll commands
        public ICommand OpenPollPopupCommand { get; }
        public ICommand ClosePollPopupCommand { get; }
        public ICommand AddPollOptionCommand { get; }
        public ICommand CreatePollCommand { get; }
        public ICommand VotePollCommand { get; }
        public ICommand ClosePollCommand { get; }
        public ICommand DeletePollCommand { get; }

        public MainViewModel()
        {
            _chatService = new ChatService();
            PollOptions = new ObservableCollection<string>();

            OpenChats = new ObservableCollection<ChannelDto>();
            DirectMessages = new ObservableCollection<ChannelDto>();
            Users = new ObservableCollection<User>();
            Documents = new ObservableCollection<DocumentResponse>();
            Versions = new ObservableCollection<DocumentVersionResponse>();

            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenPopupCommand = new RelayCommand(_ => PopupOpen = true);
            ClosePopupCommand = new RelayCommand(_ => PopupOpen = false);
            CreateChannelCommand = new RelayCommand(_ => ExecuteCreateChannel());
            SendMessageCommand = new RelayCommand(_ => ExecuteSendMessage());

            CreateDocumentCommand = new RelayCommand(_ => ExecuteCreateDocument());
            UploadVersionCommand = new RelayCommand(_ => ExecuteUploadVersion());
            DownloadVersionCommand = new RelayCommand(_ => ExecuteDownloadVersion());
            DeleteVersionCommand = new RelayCommand(_ => ExecuteDeleteVersion());
            DeleteDocumentCommand = new RelayCommand(_ => ExecuteDeleteDocument());

            OpenPollPopupCommand = new RelayCommand(_ => ExecuteOpenPollPopup());
            ClosePollPopupCommand = new RelayCommand(_ => PollPopupOpen = false);
            AddPollOptionCommand = new RelayCommand(_ => ExecuteAddPollOption());

            CreatePollCommand = new RelayCommand(_ => ExecuteCreatePoll());
            VotePollCommand = new RelayCommand(param => ExecuteVotePoll(param));
            ClosePollCommand = new RelayCommand(_ => ExecuteClosePoll());
            DeletePollCommand = new RelayCommand(_ => ExecuteDeletePoll());
        }

        public async void LoadAsyncData()
        {
            // 1) Prüfen auf User
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

            // 2) SignalR-Verbindung (ChatHub)
            await _chatService.InitializeSignalRAsync(jwt);

            // 3) Poll-Events abonnieren
            _chatService.OnPollCreated += (title, options) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (SelectedChannel != null)
                    {
                        // Erstmal unbekannte Votes => "blank"
                        var pollMsg = new ChatMessageDto
                        {
                            ChannelId = SelectedChannel.Id,
                            SenderUserId = SessionStore.User.Id,
                            SentAt = DateTime.Now,
                            Type = MessageType.Poll,
                            PollTitle = title,
                            PollOptions = options.ToList(),
                            IsClosed = false,
                            HasUserVoted = false
                        };
                        SelectedChannel.ChatMessages.Add(pollMsg);
                    }
                });
            };
            _chatService.OnPollUpdated += (title, results) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var pollMsg = FindPollMessageInAllChannels(title);
                    if (pollMsg != null)
                    {
                        // Wenn Poll schon geschlossen oder user hat gevotet => zeige Votes
                        if (pollMsg.IsClosed || pollMsg.HasUserVoted)
                        {
                            var newList = new List<string>();
                            foreach (var kvp in results)
                            {
                                newList.Add($"{kvp.Key} ({kvp.Value} Votes)");
                            }
                            pollMsg.PollOptions = newList;
                        }
                        else
                        {
                            // Noch nicht abgestimmt => nur blank text
                            pollMsg.PollOptions = results.Keys.ToList();
                        }
                    }
                });
            };
            _chatService.OnPollClosed += (title) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var pollMsg = FindPollMessageInAllChannels(title);
                    if (pollMsg != null)
                    {
                        pollMsg.IsClosed = true;
                    }
                });
            };
            _chatService.OnPollDeleted += (title) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var pollMsg = FindPollMessageInAllChannels(title);
                    if (pollMsg != null && pollMsg.ChannelId != 0)
                    {
                        var channelDto = FindChannel(pollMsg.ChannelId);
                        if (channelDto != null)
                        {
                            channelDto.ChatMessages.Remove(pollMsg);
                        }
                    }
                });
            };
            _chatService.OnPollError += (errorMsg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Poll-Fehler: " + errorMsg);
                });
            };

            // => Wir signalisieren dem UI, dass ab jetzt Polls erstellt werden können
            IsPollPluginLoaded = true;

            // 4) FileService
            _fileService = new FileService("https://localhost:7021/", jwt);
            await _fileService.InitializeFileHubAsync();

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

            // 5) ChatService-Ereignisse
            _chatService.OnMessageReceived += (channelId, senderUserId, senderName, content, sentAt) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var ch = FindChannel(channelId);
                    if (ch != null)
                    {
                        var msg = new ChatMessageDto
                        {
                            ChannelId = channelId,
                            SenderUserId = senderUserId,
                            SentAt = sentAt,
                            Type = MessageType.Text,
                            Content = content
                        };
                        ch.ChatMessages.Add(msg);

                        // Sortierung
                        var sorted = ch.ChatMessages.OrderBy(m => m.SentAt).ToList();
                        ch.ChatMessages.Clear();
                        foreach (var m in sorted)
                        {
                            ch.ChatMessages.Add(m);
                        }
                    }
                });
            };

            _chatService.OnChannelHistoryReceived += (channelId, msgList) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var ch = FindChannel(channelId);
                    if (ch != null)
                    {
                        var sorted = msgList.OrderBy(m => m.SentAt).ToList();
                        ch.ChatMessages.Clear();
                        foreach (var m in sorted)
                        {
                            ch.ChatMessages.Add(m);
                        }
                    }
                });
            };

            // 6) Channels + DMs + Users laden
            var channels = await _chatService.LoadChannels(new GetChannelsRequest(SessionStore.User.Id));
            if (channels != null)
            {
                foreach (var c in channels)
                {
                    c.ChatMessages = new ObservableCollection<ChatMessageDto>();
                    OpenChats.Add(c);
                }
            }

            var dms = await _chatService.LoadDirectChannels(new GetDirectChannelsRequest(SessionStore.User.Id));
            if (dms != null)
            {
                foreach (var d in dms)
                {
                    d.ChatMessages = new ObservableCollection<ChatMessageDto>();
                    DirectMessages.Add(d);
                }
            }

            var userList = await _chatService.LoadAllUsers(new GetUsersRequest());
            if (userList != null)
            {
                foreach (var u in userList)
                {
                    Users.Add(u);
                }
            }
        }

        // ====================  CHANNEL / CHAT  ====================
        private ChannelDto FindChannel(int channelId)
        {
            return OpenChats.FirstOrDefault(x => x.Id == channelId)
                ?? DirectMessages.FirstOrDefault(x => x.Id == channelId);
        }

        private void ExecuteLogout()
        {
            SessionStore.User = null;
            SessionStore.JwtToken = null;
            MainWindow.NavigationService.Navigate(new LoginView());
        }

        private async void ExecuteCreateChannel()
        {
            PopupOpen = false;
            if (string.IsNullOrWhiteSpace(NewChannelName) || NewChannelName.Length < 4)
            {
                MessageBox.Show("Channelname zu kurz.");
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
                if (_fileService != null)
                {
                    await _fileService.JoinFileChannel(channel.Id);
                }
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

        // ====================  FILES  ====================
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
                MessageBox.Show("Kein Channel gewählt.");
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

        // ====================  POLLS  ====================
        private ChatMessageDto FindPollMessageInAllChannels(string pollTitle)
        {
            // Durchsuche alle Channels (OpenChats, DirectMessages)
            var allChannels = OpenChats.Concat(DirectMessages);
            foreach (var chan in allChannels)
            {
                var msg = chan.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll && m.PollTitle == pollTitle);
                if (msg != null)
                    return msg;
            }
            return null;
        }

        private void ExecuteOpenPollPopup()
        {
            PollTitle = "";
            PollOptions.Clear();
            NewOptionText = "";
            PollPopupOpen = true;
        }

        private void ExecuteAddPollOption()
        {
            if (!string.IsNullOrWhiteSpace(NewOptionText))
            {
                PollOptions.Add(NewOptionText);
                NewOptionText = "";
            }
        }

        private void ExecuteClosePollPopup() => PollPopupOpen = false;

        // (A) CreatePoll
        private async void ExecuteCreatePoll()
        {
            if (!IsPollPluginLoaded)
            {
                MessageBox.Show("Umfragen nicht verfügbar.");
                return;
            }
            if (SelectedChannel == null)
            {
                MessageBox.Show("Bitte zuerst Channel wählen.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PollTitle))
            {
                MessageBox.Show("Bitte Umfragetitel eingeben.");
                return;
            }
            if (PollOptions.Count == 0)
            {
                MessageBox.Show("Bitte mindestens eine Antwortoption hinzufügen.");
                return;
            }

            try
            {
                await _chatService.CreatePoll(SelectedChannel.Id, PollTitle, PollOptions.ToList());
                MessageBox.Show($"Umfrage '{PollTitle}' wurde erstellt!");
                PollPopupOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei CreatePoll: " + ex.Message);
            }
        }

        // (B) Vote
        private async void ExecuteVotePoll(object param)
        {
            if (!IsPollPluginLoaded)
            {
                MessageBox.Show("Umfragen nicht verfügbar.");
                return;
            }

            string chosenOption = param as string;
            if (string.IsNullOrWhiteSpace(chosenOption))
            {
                MessageBox.Show("Keine Option ausgewählt.");
                return;
            }

            try
            {
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll && !m.IsClosed);

                if (pollMsg == null)
                {
                    MessageBox.Show("Keine offene Umfrage gefunden.");
                    return;
                }

                await _chatService.VotePoll(pollMsg.PollTitle, chosenOption);

                // HatUserVoted = true => UI wechselt zu Ergebnis-Anzeige
                pollMsg.HasUserVoted = true;

                MessageBox.Show($"Vote abgegeben für '{chosenOption}'!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei VotePoll: " + ex.Message);
            }
        }

        // (C) ClosePoll
        private async void ExecuteClosePoll()
        {
            if (!IsPollPluginLoaded)
            {
                MessageBox.Show("Umfragen nicht verfügbar.");
                return;
            }
            try
            {
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll && !m.IsClosed);
                if (pollMsg == null)
                {
                    MessageBox.Show("Keine offene Umfrage im Channel.");
                    return;
                }

                await _chatService.ClosePoll(pollMsg.PollTitle);
                MessageBox.Show("Umfrage geschlossen!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei ClosePoll: " + ex.Message);
            }
        }

        // (D) DeletePoll
        private async void ExecuteDeletePoll()
        {
            if (!IsPollPluginLoaded)
            {
                MessageBox.Show("Umfragen nicht verfügbar.");
                return;
            }
            try
            {
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll);
                if (pollMsg == null)
                {
                    MessageBox.Show("Keine Poll-Nachricht vorhanden.");
                    return;
                }

                await _chatService.DeletePoll(pollMsg.PollTitle);
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
