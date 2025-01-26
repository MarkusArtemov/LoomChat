using System.Windows;
using System.Windows.Controls;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Client.Global;

namespace De.Hsfl.LoomChat.Client.Converter
{
    /// <summary>
    /// Wählt das passende DataTemplate für eine ChatMessageDto
    /// basierend auf dem MessageType (Text/Poll) und ob man selbst der Sender ist.
    /// </summary>
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MyTextMessageTemplate { get; set; }
        public DataTemplate OtherTextMessageTemplate { get; set; }
        public DataTemplate PollMessageTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatMessageDto msg)
            {
                // 1) Wenn MessageType = Poll
                if (msg.Type == MessageType.Poll)
                {
                    return PollMessageTemplate;
                }
                // 2) Ansonsten: Typ = Text => Prüfen, ob eigener User
                else if (msg.Type == MessageType.Text)
                {
                    int myUserId = SessionStore.User?.Id ?? 0;
                    if (msg.SenderUserId == myUserId)
                    {
                        // Nachricht von mir -> rechtes Template
                        return MyTextMessageTemplate;
                    }
                    else
                    {
                        // fremde Nachricht -> linkes Template
                        return OtherTextMessageTemplate;
                    }
                }
            }
            // Fallback, falls item nicht ChatMessageDto oder unbekannter Type:
            return base.SelectTemplate(item, container);
        }
    }
}
