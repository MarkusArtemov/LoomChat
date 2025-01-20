using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Models;
using De.Hsfl.LoomChat.Client.Views;
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
            RegisterCommand = new RelayCommand(ExecuteRegister);
            NavigateToLoginCommand = new RelayCommand(_ =>
            {
                MainWindow.NavigationService.Navigate(new LoginView());
            });
        }

        private async void ExecuteRegister(object parameter)
        {
            var registerData = new RegisterRequest(Username, Password);

            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/register"; 
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(registerData), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Registrierung erfolgreich! Sie können sich jetzt anmelden.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                        MainWindow.NavigationService.Navigate(new LoginView()); // Zur Login-Ansicht navigieren
                    }
                    else
                    {
                        MessageBox.Show($"Registrierung fehlgeschlagen. Statuscode: {response.StatusCode}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler bei der Registrierung: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                }
            }
        }
    }
}
