using System;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Net.Http;
using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Views;
using Newtonsoft.Json;
using De.Hsfl.LoomChat.Client.Models;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password;
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
            NavigateToRegisterCommand = new RelayCommand(_ =>
            {
                MainWindow.NavigationService.Navigate(new RegisterView());
            });
        }

        private async void ExecuteLogin(object parameter)
        {
            var loginData = new LoginRequest(Username, Password);

            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/login";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(loginData), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Login erfolgreich!");
                        MainWindow.NavigationService.Navigate(new MainView());
                    }
                    else
                    {
                        MessageBox.Show($"Login fehlgeschlagen. Statuscode: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Login: {ex.Message}");
                }
            }
        }
    }
}
