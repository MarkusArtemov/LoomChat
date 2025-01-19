using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Views;
using De.Hsfl.LoomChat.Client.Services;
using System.Net.Http;

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
            var loginData = new Dictionary<string, string>
            {
                { "Username", Username },
                { "Password", Password } 
            };

            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/login";
                    var content = new FormUrlEncodedContent(loginData);
                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Login erfolgreich!");
                    }
                    else
                    {
                        MessageBox.Show("Login fehlgeschlagen. Überprüfe die Anmeldedaten.");
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
