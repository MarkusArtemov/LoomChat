using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using De.Hsfl.LoomChat.Client.Services;

namespace De.Hsfl.LoomChat.Client
{
    public partial class MainWindow : Window
    {
        public static NavigationService NavigationService { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            NavigationService = new NavigationService(MainFrame);
            MainFrame.Navigate(new Views.LoginView());
        }
    }
}
