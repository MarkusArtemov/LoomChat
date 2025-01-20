using System;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Net.Http;
using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Views;
using Newtonsoft.Json;
using De.Hsfl.LoomChat.Client.Services;
using De.Hsfl.LoomChat.Common.Dtos;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        private string _password;
        private string _errorMessage;
        private LoginService _loginService;


        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
            _loginService = new LoginService();
            NavigateToRegisterCommand = new RelayCommand(_ =>
            {
                MainWindow.NavigationService.Navigate(new RegisterView());
            });
        }

        private async void ExecuteLogin(object parameter)
        {
            var loginData = new LoginRequest(Username, Password);
            var result = await _loginService.Login(loginData);
            if (result)
            {
                MainWindow.NavigationService.Navigate(new MainView());
            }
            else
            {
                ErrorMessage = "Fehler beim Einloggen!";
            }
        }
    }
}
