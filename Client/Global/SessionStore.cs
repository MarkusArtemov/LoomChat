using De.Hsfl.LoomChat.Common.Models;

namespace De.Hsfl.LoomChat.Client.Global
{
    /// <summary>
    /// Hält globale Sessiondaten des aktuellen Nutzers (in einer statischen Klasse).
    /// </summary>
    public static class SessionStore
    {
        public static string JwtToken { get; set; }
        public static User User { get; set; }
    }
}
