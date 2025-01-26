using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace De.Hsfl.LoomChat.Client.Converter
{
    /// <summary>
    /// Zeigt Visible, wenn 
    ///   - IsClosed == false UND
    ///   - HasUserVoted == false
    /// </summary>
    public class PollResultsVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Visibility.Collapsed;

            // 0 => IsClosed
            // 1 => HasUserVoted
            if (values[0] is bool isClosed && isClosed) return Visibility.Collapsed;
            if (values[1] is bool hasVoted && hasVoted) return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
