namespace De.Hsfl.LoomChat.Client.Plugins
{
    /// <summary>
    /// Basisschnittstelle für jedes Plugin,
    /// das zur Laufzeit geladen werden kann.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Eindeutiger Name des Plugins, z.B. "Survey"
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Wird nach dem Laden aufgerufen.
        /// Hier kann das Plugin interne Init-Logik durchführen.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Beispiel-Methode zum Starten der eigentlichen Funktion
        /// (z.B. Umfrage-Fenster öffnen).
        /// </summary>
        void Execute();
    }
}
