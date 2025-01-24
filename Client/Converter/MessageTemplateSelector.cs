using System.Windows;
using System.Windows.Controls;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Client.Global; // Damit wir SessionStore.User.Id kennen

namespace De.Hsfl.LoomChat.Client.Converter
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MyMessageTemplate { get; set; }
        public DataTemplate OtherMessageTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatMessageDto msg)
            {
                // Wenn die SenderUserId == meine UserId, benutze das "rechte" Template
                if (msg.SenderUserId == SessionStore.User.Id)
                {
                    return MyMessageTemplate;
                }
            }
            // Ansonsten fremde Nachricht -> "linkes" Template
            return OtherMessageTemplate;
        }
    }
}
