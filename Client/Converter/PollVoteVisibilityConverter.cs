using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace De.Hsfl.LoomChat.Client.Converter
{
    /// <summary>
    /// Zeigt Visible, wenn:
    ///   - Plugin geladen: IsPollPluginLoaded == true
    ///   - Poll NICHT geschlossen: IsClosed == false
    ///   - User hat NICHT gevotet: HasUserVoted == false
    /// Sonst Collapsed.
    /// => Wir kriegen 3 bool-Werte in values[0..2].
    /// </summary>
    public class PollVoteVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return Visibility.Collapsed;

            // 0 => IsPollPluginLoaded
            // 1 => IsClosed
            // 2 => HasUserVoted
            if (values[0] is bool pluginLoaded &&
                values[1] is bool isClosed &&
                values[2] is bool hasVoted)
            {
                if (pluginLoaded && !isClosed && !hasVoted)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
