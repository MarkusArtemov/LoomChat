using De.Hsfl.LoomChat.Client.Commands;
using De.Hsfl.LoomChat.Client.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace De.Hsfl.LoomChat.Client.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
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

        private void ExecuteRegister(object parameter)
        {
            if (Username == "admin" && (parameter as string) == "password123")
            {
                // Login erfolgreich: Navigation zur Hauptansicht
                var mainView = new MainView();
                mainView.Show();
                App.Current.MainWindow.Close(); // Aktuelle View schließen
                App.Current.MainWindow = mainView;
            }
            else
            {
                // Login fehlgeschlagen
                MessageBox.Show("Ungültige Anmeldedaten", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
