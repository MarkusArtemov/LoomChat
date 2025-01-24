using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using De.Hsfl.LoomChat.Client.ViewModels;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;

namespace De.Hsfl.LoomChat.Client.Views
{
    public partial class MainView : Page
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void MainViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.LoadAsyncData();
            }
        }

        private void ChannelListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((ListView)sender).SelectedItem is ChannelDto selectedChannel)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.ChannelClicked(selectedChannel);
                }
            }
        }

        private void UsersListViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((ListView)sender).SelectedItem is User selectedUser)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.UserClicked(selectedUser);
                }
            }
        }
    }
}
