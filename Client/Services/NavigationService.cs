using System;
using System.Windows.Controls;

namespace De.Hsfl.LoomChat.Client.Services
{
    public class NavigationService
    {
        private readonly Frame _mainFrame;

        public NavigationService(Frame mainFrame)
        {
            _mainFrame = mainFrame ?? throw new ArgumentNullException(nameof(mainFrame));
        }

        public void Navigate(Page page)
        {
            _mainFrame.Navigate(page);
        }
    }
}
