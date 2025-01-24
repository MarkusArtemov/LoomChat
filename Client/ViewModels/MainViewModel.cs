using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Global;
using De.Hsfl.LoomChat.Client.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ChatService _chatService;
        private readonly LoginService _loginService;
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
            set
            {
                _newMessage = value;
                OnPropertyChanged();
            }
        }

        private bool _popupOpen;
        public bool PopupOpen
        {
            get => _popupOpen;
            set
            {
                _popupOpen = value;
                OnPropertyChanged();
            }
        }

        private string _newChannelName;
        public string NewChannelName
        {
            get => _newChannelName;
            set
            {
                _newChannelName = value;
                OnPropertyChanged();
            }
        }

        public ICommand LogoutCommand { get; }
        public ICommand OpenPopupCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand CreateChannelCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand CreateDocumentCommand { get; }
        public ICommand UploadVersionCommand { get; }
        public ICommand DownloadVersionCommand { get; }

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

        public MainViewModel()
        {
            _loginService = new LoginService();
            _chatService = new ChatService();
            OpenChats = new ObservableCollection<ChannelDto>();
            DirectMessages = new ObservableCollection<ChannelDto>();
            Users = new ObservableCollection<User>();
            Documents = new ObservableCollection<DocumentResponse>();
            Versions = new ObservableCollection<DocumentVersionResponse>();

            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenPopupCommand = new RelayCommand(_ => ExecuteOpenPopup());
            ClosePopupCommand = new RelayCommand(_ => ExecuteClosePopup());
            CreateChannelCommand = new RelayCommand(_ => ExecuteCreateChannel());
            SendMessageCommand = new RelayCommand(_ => ExecuteSendMessage());
            CreateDocumentCommand = new RelayCommand(_ => ExecuteCreateDocument());
            UploadVersionCommand = new RelayCommand(_ => ExecuteUploadVersion());
            DownloadVersionCommand = new RelayCommand(_ => ExecuteDownloadVersion());
        }

        public async void LoadAsyncData()
        {
            if (SessionStore.User == null)
            {
                MessageBox.Show("Keine Benutzerdaten vorhanden.");
                return;
            }
            string jwt = SessionStore.JwtToken;
            if (string.IsNullOrEmpty(jwt))
            {
                MessageBox.Show("Kein JWT-Token vorhanden.");
                return;
            }
            await _chatService.InitializeSignalRAsync(jwt);
            _fileService = new FileService("http://localhost:5277", jwt);

            _chatService.OnMessageReceived += (channelId, senderUserId, senderName, content, sentAt) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var ch = FindChannel(channelId);
                    if (ch != null)
                    {
                        ch.ChatMessages.Add(new ChatMessageDto
                        {
                            ChannelId = channelId,
                            SenderUserId = senderUserId,
                            Content = content,
                            SentAt = sentAt
                        });
                        ch.ChatMessages = new ObservableCollection<ChatMessageDto>(
                            ch.ChatMessages.OrderBy(m => m.SentAt)
                        );
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
                            ch.ChatMessages.Add(new ChatMessageDto
                            {
                                Id = m.Id,
                                ChannelId = m.ChannelId,
                                SenderUserId = m.SenderUserId,
                                Content = m.Content,
                                SentAt = m.SentAt
                            });
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

        private ChannelDto FindChannel(int channelId)
        {
            return OpenChats.FirstOrDefault(x => x.Id == channelId)
                ?? DirectMessages.FirstOrDefault(x => x.Id == channelId);
        }

        private void ExecuteLogout()
        {
            _loginService.Logout();
        }

        private void ExecuteOpenPopup()
        {
            PopupOpen = true;
        }

        private void ExecuteClosePopup()
        {
            PopupOpen = false;
        }

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
                MessageBox.Show("Kein Channel gewählt");
                return;
            }
            if (_fileService == null) return;
            var doc = await _fileService.CreateAndUploadFileForChannel(SelectedChannel.Id);
            if (doc != null)
            {
                Documents.Add(doc);
            }
        }

        private async void ExecuteUploadVersion()
        {
            if (SelectedDocument == null)
            {
                MessageBox.Show("Kein Dokument ausgewählt.");
                return;
            }
            if (_fileService == null) return;
            var ver = await _fileService.UploadVersionAsync(SelectedDocument.Id);
            if (ver != null)
            {
                Versions.Add(ver);
            }
        }

        private async void ExecuteDownloadVersion()
        {
            if (SelectedDocument == null)
            {
                MessageBox.Show("Kein Dokument ausgewählt.");
                return;
            }
            if (SelectedVersion == null)
            {
                MessageBox.Show("Keine Version ausgewählt.");
                return;
            }
            if (_fileService == null) return;
            await _fileService.DownloadVersionAsync(SelectedDocument.Id, SelectedVersion.VersionNumber);
        }
    }
}
