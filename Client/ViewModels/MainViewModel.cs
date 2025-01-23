using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using De.Hsfl.LoomChat.Client.Global; 
namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ChatService _chatService;
        private LoginService _loginService;

        public ObservableCollection<ChannelDto> OpenChats { get; set; }
        public ObservableCollection<ChannelDto> DirectMessages { get; set; }
        public ObservableCollection<User> Users { get; set; }

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

        public ICommand LogoutCommand { get; }
        public ICommand OpenPopupCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand CreateChannelCommand { get; }
        public ICommand SendMessageCommand { get; }

        public MainViewModel()
        {
            _loginService = new LoginService(); // Falls du ihn brauchst
            _chatService = new ChatService();

            // Initialisiere Collections
            OpenChats = new ObservableCollection<ChannelDto>();
            DirectMessages = new ObservableCollection<ChannelDto>();
            Users = new ObservableCollection<User>();

            // Commands
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenPopupCommand = new RelayCommand(_ => ExecuteOpenPopup());
            ClosePopupCommand = new RelayCommand(_ => ExecuteClosePopup());
            CreateChannelCommand = new RelayCommand(_ => ExecuteCreateChannel());
            SendMessageCommand = new RelayCommand(_ => ExecuteSendMessage());
        }

        public async void LoadAsyncData()
        {
            // Prüfe, ob User und JWT verfügbar sind
            if (SessionStore.User == null)
            {
                MessageBox.Show("Keine Benutzerdaten vorhanden.");
                return;
            }
            string jwtToken = SessionStore.JwtToken;
            if (string.IsNullOrEmpty(jwtToken))
            {
                MessageBox.Show("Kein JWT-Token vorhanden.");
                return;
            }

            // 1) SignalR verbinden
            await _chatService.InitializeSignalRAsync(jwtToken);

            // 2) Events abonnieren
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

                        // Falls du ein Refresh brauchst:
                        if (SelectedChannel != null && SelectedChannel.Id == channelId)
                        {
                            var tmp = SelectedChannel;
                            SelectedChannel = null;
                            SelectedChannel = tmp;
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
                        ch.ChatMessages.Clear();
                        foreach (var m in msgList)
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

                        if (SelectedChannel != null && SelectedChannel.Id == channelId)
                        {
                            var tmp = SelectedChannel;
                            SelectedChannel = null;
                            SelectedChannel = tmp;
                        }
                    }
                });
            };

            // 3) Channels, DMs, Users per REST laden
            var channels = await _chatService.LoadChannels(new GetChannelsRequest(SessionStore.User.Id));
            if (channels != null)
            {
                foreach (var c in channels)
                {
                    // Falls du ChatMessages-Collection brauchst:
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

            var allUsers = await _chatService.LoadAllUsers(new GetUsersRequest());
            if (allUsers != null)
            {
                foreach (var u in allUsers)
                {
                    Users.Add(u);
                }
            }
        }

        private ChannelDto FindChannel(int channelId)
        {
            var c = OpenChats.FirstOrDefault(x => x.Id == channelId)
                ?? DirectMessages.FirstOrDefault(x => x.Id == channelId);
            return c;
        }

        private void ExecuteLogout()
        {
            // Beispiel: LogoutService
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
                ChannelClicked(dto);
            }
        }

        private async void ExecuteSendMessage()
        {
            if (SelectedChannel == null || string.IsNullOrWhiteSpace(NewMessage))
                return;

            // Hier holen wir die User-ID aus dem SessionStore
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
            }
        }

        public async void UserClicked(User user)
        {
            if (user == null) return;

            var chan = await _chatService.OpenChatWithUser(new OpenChatWithUserRequest(SessionStore.User.Id, user.Id));
            if (chan != null)
            {
                // Prüfen, ob dieser DM-Channel bereits existiert
                var existing = DirectMessages.FirstOrDefault(c => c.Id == chan.Id);
                if (existing == null)
                {
                    chan.ChatMessages = new ObservableCollection<ChatMessageDto>();
                    DirectMessages.Insert(0, chan);
                }
                else
                {
                    DirectMessages.Remove(existing);
                    DirectMessages.Insert(0, existing);
                }
                ChannelClicked(chan);
            }
        }
    }
}
