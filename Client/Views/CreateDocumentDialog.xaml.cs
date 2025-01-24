using System.Windows;

namespace De.Hsfl.LoomChat.Client.Views
{
    public partial class CreateDocumentDialog : Window
    {
        public string DocumentName { get; private set; }

        public CreateDocumentDialog()
        {
            InitializeComponent();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            DocumentName = DocumentNameTextBox.Text;
            DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
