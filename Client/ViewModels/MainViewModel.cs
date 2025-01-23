using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Global;
using De.Hsfl.LoomChat.Client.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class MainViewModel: BaseViewModel
    {
        private ObservableCollection<ChannelDto> _openChats;
        public ObservableCollection<ChannelDto> OpenChats
        {
            get => _openChats;
            set
            {
                _openChats = value;
                OnPropertyChanged(nameof(OpenChats));
            }
        }

        private ObservableCollection<ChannelDto> _directMessages;
        public ObservableCollection<ChannelDto> DirectMessages
        {
            get => _directMessages;
            set
            {
                _directMessages = value;
                OnPropertyChanged(nameof(DirectMessages));
            }
        }

        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged(nameof(Users));
            }
        }

        private string _newChannelName;
        public string NewChannelName
        {
            get => _newChannelName;
            set
            {
                _newChannelName = value;
                OnPropertyChanged(nameof(NewChannelName));
            }
        }

        private string _newMessage = "";
        public string NewMessage
        {
            get => _newMessage;
            set
            {
                _newMessage = value;
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        private bool _popupOpen;
        public bool PopupOpen
        {
            get => _popupOpen;
            set
            {
                if (_popupOpen != value)
                {
                    _popupOpen = value;
                    OnPropertyChanged(nameof(PopupOpen));
                }
            }
        }

        private ChannelDto _selectedChannel;

        public ChannelDto SelectedChannel
        {
            get => _selectedChannel;
            set
            {
                _selectedChannel = value;
                ChatVisible = true;
                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        private bool _chatVisible = false;
        public bool ChatVisible
        {
            get => _chatVisible;
            set
            {
                _chatVisible = value;
                OnPropertyChanged(nameof(ChatVisible));
                OnPropertyChanged(nameof(IsChatNotVisible));
            }
        }

        public bool IsChatNotVisible => !_chatVisible;

        public ICommand OpenNewChatCommand { get; set; }
        public ICommand LogoutCommand { get; set; }

        public ICommand CreateChannelCommand { get; set; }

        public ICommand OpenPopupCommand { get; set; }

        public ICommand ClosePopupCommand { get; set; }

        public ICommand SendMessageCommand { get; set; }

        private LoginService _loginService;
        private ChatService _chatService;

        public MainViewModel() 
        {
            _loginService = new LoginService();
            _chatService = new ChatService();
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenNewChatCommand = new RelayCommand(_ => ExecuteOpenChat());
            CreateChannelCommand = new RelayCommand(_ => ExecuteCreateChannel());
            OpenPopupCommand = new RelayCommand(_ => OpenPopup());
            ClosePopupCommand = new RelayCommand(_ => ClosePopup());
            SendMessageCommand = new RelayCommand(_ => SendMessage());
        }

        public void OpenPopup()
        {
            PopupOpen = true;
        }

        public void ClosePopup()
        {
            PopupOpen = false;
        }

        public void ExecuteLogout()
        {
            _loginService.Logout();
        }

        public void ExecuteOpenChat()
        {

        }

        public async void ExecuteCreateChannel()
        {
            PopupOpen = false;
            if(NewChannelName.Length > 3)
            {
                ChannelDto dto = await _chatService.CreateNewChannel(new CreateChannelRequest(SessionStore.User.Id, NewChannelName));
                OpenChats.Add(dto);
            } else
            {
                MessageBox.Show("Der Channelname muss mind. 4 Zeichen lang sein.");
            }
        }

        public async void LoadAsyncData()
        {
            if(SessionStore.User != null)
            {
                OpenChats = new ObservableCollection<ChannelDto>(await _chatService.LoadChannels(new GetChannelsRequest(SessionStore.User.Id)));
                DirectMessages = new ObservableCollection<ChannelDto>(await _chatService.LoadDirectChannels(new GetDirectChannelsRequest(SessionStore.User.Id)));
                Users = new ObservableCollection<User>(await _chatService.LoadAllUsers(new GetUsersRequest()));
            } else
            {
                MessageBox.Show($"Kein UserObjekt zum Fetchen der Daten vorhanden.");
            }
        }

        public async void UserClicked(User user)
        {
            ChannelDto chan = await _chatService.OpenChatWithUser(new OpenChatWithUserRequest(SessionStore.User.Id, user.Id));
            if (chan != null)
            {
                var existingChannel = DirectMessages.FirstOrDefault(c => c.Id == chan.Id);

                if (existingChannel != null)
                {
                    DirectMessages.Remove(existingChannel);
                    DirectMessages.Insert(0, existingChannel);
                }
                else
                {
                    DirectMessages.Insert(0, chan);
                }
            }
        }

        public async void ChannelClicked(ChannelDto channel)
        {
            SelectedChannel = channel;
        }

        public async void SendMessage()
        {
            ChannelDto response = await _chatService.SendMessage(new SendMessageRequest(SessionStore.User.Id, NewMessage, SelectedChannel.Id));
            NewMessage = "";
            SelectedChannel = response;
        }
    }
}
