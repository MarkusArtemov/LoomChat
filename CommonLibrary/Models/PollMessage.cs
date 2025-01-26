namespace De.Hsfl.LoomChat.Common.Models
{
    /// <summary>
    /// Chat-Nachricht für eine Umfrage, verknüpft mit einem Poll-Eintrag
    /// </summary>
    public class PollMessage : ChatMessage
    {
        // 1:1 oder 1:n? Hier ein Beispiel für "jede PollMessage gehört zu genau 1 Poll".
        public int PollId { get; set; }
        public Poll Poll { get; set; }
    }
}
