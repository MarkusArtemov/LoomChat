using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace De.Hsfl.LoomChat.Client.Converter
{
    /// <summary>
    /// Zeigt Visible, wenn
    ///   - IsClosed == true  ODER
    ///   - HasUserVoted == true
    /// => Wir kriegen 2 bool-Werte in values[0..1].
    /// </summary>
    public class PollResultsVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Visibility.Collapsed;

            // 0 => IsClosed
            // 1 => HasUserVoted
            if (values[0] is bool isClosed && isClosed)
                return Visibility.Visible;
            if (values[1] is bool hasVoted && hasVoted)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
