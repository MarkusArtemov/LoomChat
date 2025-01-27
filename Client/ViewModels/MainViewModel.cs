﻿using System;
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
        private PluginManager _pluginManager;

        // Poll
        private IPollPlugin _pollPlugin;
        private bool _isPollPluginLoaded;
        public bool IsPollPluginLoaded
        {
            get => _isPollPluginLoaded;
            set
            {
                _isPollPluginLoaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PollPluginButtonContent));
            }
        }

        public string PollPluginButtonContent => IsPollPluginLoaded ? "Uninstall Poll Plugin" : "Install Poll Plugin";

        // BlackList
        private ITextFilterPlugin _textFilterPlugin;
        private bool _isBlackListPluginLoaded;
        public bool IsBlackListPluginLoaded
        {
            get => _isBlackListPluginLoaded;
            set
            {
                _isBlackListPluginLoaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BlackListPluginButtonContent));
            }
        }

        public string BlackListPluginButtonContent => IsBlackListPluginLoaded ? "Uninstall BlackList Plugin" : "Install BlackList Plugin";

        // Collections
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

        // Toggle commands for Poll & BlackList
        public ICommand TogglePollPluginCommand { get; }
        public ICommand ToggleBlackListPluginCommand { get; }

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

            // Poll plugin toggle
            TogglePollPluginCommand = new RelayCommand(_ => ExecuteTogglePollPlugin());
            // BlackList plugin toggle
            ToggleBlackListPluginCommand = new RelayCommand(_ => ExecuteToggleBlackListPlugin());

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

            await _chatService.InitializeSignalRAsync(jwt);

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

            _chatService.OnMessageReceived += (channelId, senderUserId, senderName, content, sentAt) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_textFilterPlugin != null)
                    {
                        content = _textFilterPlugin.OnBeforeReceive(content);
                    }

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
                        if (_textFilterPlugin != null)
                        {
                            foreach (var message in msgList)
                            {
                                message.Content = _textFilterPlugin.OnBeforeReceive(message.Content);
                            }
                        }
                        var sorted = msgList.OrderBy(m => m.SentAt).ToList();
                        ch.ChatMessages.Clear();
                        foreach (var m in sorted)
                        {
                            ch.ChatMessages.Add(m);
                        }
                    }
                });
            };

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

            if (_textFilterPlugin != null)
            {
                NewMessage = _textFilterPlugin.OnBeforeSend(NewMessage);
            }

            int userId = SessionStore.User.Id;
            await _chatService.SendMessageSignalR(SelectedChannel.Id, userId, NewMessage);
            NewMessage = "";
        }

        // Toggle PollPlugin
        private async void ExecuteTogglePollPlugin()
        {
            try
            {
                if (_pluginManager == null)
                {
                    _pluginManager = new PluginManager("http://localhost:5115");
                }

                if (!IsPollPluginLoaded)
                {
                    // Install & load
                    var baseUrl = "http://localhost:5115";
                    var token = SessionStore.JwtToken;
                    var plugin = await _pluginManager.InstallAndLoadPluginAsync("PollPlugin", baseUrl, token);
                    _pollPlugin = plugin as IPollPlugin;
                    if (_pollPlugin == null)
                    {
                        MessageBox.Show("Loaded plugin is not IPollPlugin!");
                        return;
                    }

                    // Wire up poll events
                    _pollPlugin.PollCreatedEvent += (title, options) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (SelectedChannel != null)
                            {
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
                            }
                        });
                    };

                    _pollPlugin.PollUpdatedEvent += (title, results) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var pollMsg = FindPollMessageInAllChannels(title);
                            if (pollMsg != null)
                            {
                                var newList = new List<string>();
                                foreach (var kvp in results)
                                {
                                    newList.Add($"{kvp.Key} ({kvp.Value} Votes)");
                                }
                                pollMsg.PollOptions = newList;
                            }
                        });
                    };

                    _pollPlugin.PollClosedEvent += (title) =>
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

                    _pollPlugin.PollDeletedEvent += (title) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var pollMsg = FindPollMessageInAllChannels(title);
                            if (pollMsg != null && pollMsg.ChannelId != 0)
                            {
                                var channelDto = FindChannel(pollMsg.ChannelId);
                                channelDto?.ChatMessages.Remove(pollMsg);
                            }
                        });
                    };

                    IsPollPluginLoaded = true;
                    MessageBox.Show("PollPlugin installed and loaded.");
                }
                else
                {
                    // Uninstall
                    _pluginManager.UninstallPlugin("PollPlugin");
                    _pollPlugin = null;
                    IsPollPluginLoaded = false;
                    MessageBox.Show("PollPlugin uninstalled.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling PollPlugin: {ex.Message}");
            }
        }

        // Toggle BlackListPlugin
        private async void ExecuteToggleBlackListPlugin()
        {
            try
            {
                if (_pluginManager == null)
                {
                    _pluginManager = new PluginManager("http://localhost:5115");
                }

                if (!IsBlackListPluginLoaded)
                {
                    var baseUrl = "http://localhost:5115";
                    var token = SessionStore.JwtToken;

                    var plugin = await _pluginManager.InstallAndLoadPluginAsync("BlackListPlugin", baseUrl, token);
                    if (plugin is ITextFilterPlugin textFilter)
                    {
                        _textFilterPlugin = textFilter;
                        IsBlackListPluginLoaded = true;
                        MessageBox.Show("BlackListPlugin installed and loaded.");
                    }
                    else
                    {
                        MessageBox.Show("Plugin is not ITextFilterPlugin!");
                    }
                }
                else
                {
                    _pluginManager.UninstallPlugin("BlackListPlugin");
                    _textFilterPlugin = null;
                    IsBlackListPluginLoaded = false;
                    MessageBox.Show("BlackListPlugin uninstalled.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling BlackListPlugin: {ex.Message}");
            }
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

        private void ExecuteClosePollPopup()
        {
            PollPopupOpen = false;
        }

        private async void ExecuteCreatePoll()
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin not loaded!");
                return;
            }
            if (SelectedChannel == null)
            {
                MessageBox.Show("Select a channel first.");
                return;
            }
            if (string.IsNullOrWhiteSpace(PollTitle))
            {
                MessageBox.Show("Enter poll title.");
                return;
            }
            if (PollOptions.Count == 0)
            {
                MessageBox.Show("Add at least one option.");
                return;
            }

            try
            {
                var opts = PollOptions.ToList();
                await _pollPlugin.CreatePoll(SelectedChannel.Id, PollTitle, opts);
                MessageBox.Show($"Poll '{PollTitle}' created!");
                PollPopupOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CreatePoll: " + ex.Message);
            }
        }

        private async void ExecuteVotePoll(object param)
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin not loaded!");
                return;
            }

            string chosenOption = param as string;
            if (string.IsNullOrWhiteSpace(chosenOption))
            {
                MessageBox.Show("No option selected.");
                return;
            }

            try
            {
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll && !m.IsClosed);
                if (pollMsg == null)
                {
                    MessageBox.Show("No open poll found.");
                    return;
                }

                await _pollPlugin.Vote(pollMsg.PollTitle, chosenOption);
                pollMsg.HasUserVoted = true;
                MessageBox.Show($"Voted '{chosenOption}'!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in VotePoll: " + ex.Message);
            }
        }

        private async void ExecuteClosePoll()
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin not loaded!");
                return;
            }
            try
            {
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll && !m.IsClosed);
                if (pollMsg == null)
                {
                    MessageBox.Show("No open poll in this channel.");
                    return;
                }

                await _pollPlugin.ClosePoll(pollMsg.PollTitle);
                MessageBox.Show("Poll closed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ClosePoll: " + ex.Message);
            }
        }

        private async void ExecuteDeletePoll()
        {
            if (_pollPlugin == null)
            {
                MessageBox.Show("PollPlugin not loaded!");
                return;
            }
            try
            {
                var pollMsg = SelectedChannel?.ChatMessages
                    .FirstOrDefault(m => m.Type == MessageType.Poll);
                if (pollMsg == null)
                {
                    MessageBox.Show("No poll message found.");
                    return;
                }

                await _pollPlugin.DeletePoll(pollMsg.PollTitle);
                MessageBox.Show("Poll deleted.");

                SelectedChannel.ChatMessages.Remove(pollMsg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeletePoll: " + ex.Message);
            }
        }

        private ChannelDto FindChannel(int channelId)
        {
            return OpenChats.FirstOrDefault(x => x.Id == channelId)
                ?? DirectMessages.FirstOrDefault(x => x.Id == channelId);
        }

        private ChatMessageDto FindPollMessageInAllChannels(string pollTitle)
        {
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
    }
}
