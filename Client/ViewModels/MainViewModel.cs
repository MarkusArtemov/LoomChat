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

            // Collections initialisieren
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

                        // Optionales Refresh
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

            // (A) Channels
            var channels = await _chatService.LoadChannels(new GetChannelsRequest(SessionStore.User.Id));
            if (channels != null)
            {
                foreach (var c in channels)
                {
                    OpenChats.Add(c);
                }
            }

            // (B) DMs
            var dms = await _chatService.LoadDirectChannels(new GetDirectChannelsRequest(SessionStore.User.Id));
            if (dms != null)
            {
                foreach (var dm in dms)
                {
                    // Hier benennen wir "Direktnachricht" in den Usernamen des Gegenübers um
                    if (dm.IsDmChannel && dm.ChannelMembers != null)
                    {
                        var otherMember = dm.ChannelMembers
                            .FirstOrDefault(m => m.UserId != SessionStore.User.Id);

                        if (otherMember != null)
                        {
                            var otherUser = Users.FirstOrDefault(u => u.Id == otherMember.UserId);
                            if (otherUser != null)
                            {
                                dm.Name = otherUser.Username;
                            }
                            // Wenn unknown, bleibt "Direktnachricht" oder du setzt was anderes
                        }
                    }

                    DirectMessages.Add(dm);
                }
            }

            // (C) Users
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
                OpenChats.Add(dto);
                ChannelClicked(dto);
            }
        }

        private async void ExecuteSendMessage()
        {
            if (SelectedChannel == null || string.IsNullOrWhiteSpace(NewMessage))
                return;

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

            // Server liefert Channel mit "Name=Direktnachricht" + ChannelMembers
            var chan = await _chatService.OpenChatWithUser(new OpenChatWithUserRequest(
                SessionStore.User.Id,
                user.Id
            ));

            if (chan != null)
            {
                // Benenne den Channel auf den angeklickten User um, 
                // da wir genau wissen, dass "user" der andere ist.
                if (chan.IsDmChannel)
                {
                    chan.Name = user.Username;
                }

                // Oder du könntest again ChannelMembers checken:
                // var otherMember = chan.ChannelMembers.FirstOrDefault(m => m.UserId != SessionStore.User.Id);
                // if (otherMember != null) { ... }

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
    }
}
