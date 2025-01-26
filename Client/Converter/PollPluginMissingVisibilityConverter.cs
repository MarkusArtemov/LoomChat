using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace De.Hsfl.LoomChat.Client.Converter
{
    /// <summary>
    /// Zeigt Visible, wenn das Plugin NICHT geladen ist (IsPollPluginLoaded == false).
    /// Collapsed sonst.
    /// => Bekommt 1 bool-Wert (IsPollPluginLoaded).
    /// </summary>
    public class PollPluginMissingVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = IsPollPluginLoaded (bool).
            if (values.Length == 0) return Visibility.Collapsed;

            if (values[0] is bool pluginLoaded)
            {
                // Wenn NICHT geladen => Visible
                return pluginLoaded ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
