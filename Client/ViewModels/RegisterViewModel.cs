using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Services;
using De.Hsfl.LoomChat.Client.Views;
using De.Hsfl.LoomChat.Common.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {

        private string _username;
        private string _password;
        private RegisterService _registerService;
        private string _errorMessage;

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

        public ICommand RegisterCommand { get; }
        public ICommand NavigateToLoginCommand { get; }

        public RegisterViewModel()
        {
            _registerService = new RegisterService();
            RegisterCommand = new RelayCommand(ExecuteRegister);
            NavigateToLoginCommand = new RelayCommand(_ =>
            {
                MainWindow.NavigationService.Navigate(new LoginView());
            });
        }

        private async void ExecuteRegister(object parameter)
        {
            var registerData = new RegisterRequest(Username, Password);
            var result = await _registerService .Register(registerData);
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
